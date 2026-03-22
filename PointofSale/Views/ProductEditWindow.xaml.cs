using PointofSale.Data;
using PointofSale.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace PointofSale.Views
{
    public partial class ProductEditWindow : Window
    {
        private readonly int? _editId;   // null = add, not null = edit
        public bool Saved { get; private set; } = false;

        public ProductEditWindow(int? editProductId = null)
        {
            InitializeComponent();

            _editId = editProductId;

            LoadLookups();
            LoadForEditIfNeeded();
            ApplySkuAluRule();
        }

        private void LoadLookups()
        {
            using var db = new AppDbContext();
            var supplierNames = db.Suppliers
                                  .Where(s => s.IsActive)
                                  .OrderBy(s => s.Name)
                                  .Select(s => s.Name)
                                  .ToList();

            VendorBox.ItemsSource = supplierNames;

            var departments = db.Departments
                                .Where(d => d.IsActive)
                                .OrderBy(d => d.Name)
                                .Select(d => d.Name)
                                .ToList();
            DepartmentBox.ItemsSource = departments;

            UomBox.SelectedIndex = 0;
        }

        private void DepartmentBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var text = (DepartmentBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            var res = MessageBox.Show($"Add department '{text}' to database?", "Confirm Add", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            using var db = new AppDbContext();
            var exists = db.Departments.Any(d => d.Name.ToLower() == text.ToLower());
            if (exists)
            {
                MessageBox.Show("That department already exists.");
            }
            else
            {
                var d = new Department { Name = text, IsActive = true };
                db.Departments.Add(d);
                db.SaveChanges();

                LoadLookups();
                DepartmentBox.Text = d.Name;
            }
        }

        private void LoadForEditIfNeeded()
        {
            if (_editId == null)
            {
                Title = "Add Inventory Item";
                ItemNoBox.Text = "(auto)";
                return;
            }

            Title = "Edit Inventory Item";

            using var db = new AppDbContext();
            var p = db.Products.FirstOrDefault(x => x.Id == _editId.Value);
            if (p == null)
            {
                MessageBox.Show("Product not found.");
                Close();
                return;
            }

            NameBox.Text = p.Name;
            PriceBox.Text = p.Price.ToString("0.00");
            StockBox.Text = p.StockQty.ToString();
            SkuBox.Text = p.SKU ?? "";

            DepartmentBox.Text = p.Department ?? "";
            DescriptionBox.Text = p.Description ?? "";
            SizeBox.Text = p.Size ?? "";
            CostPriceBox.Text = p.CostPrice.ToString("0.00");
            LoadTaxField(p.Tax);

            VendorBox.Text = p.Supplier ?? "";
            ReorderPointBox.Text = p.ReorderPoint.ToString();
            ItemNoBox.Text = p.Id.ToString();
            AluBox.Text = p.ALU ?? "";
            UomBox.Text = string.IsNullOrWhiteSpace(p.UnitOfMeasure) ? "Each" : p.UnitOfMeasure;
            ManufacturerBox.Text = p.Manufacturer ?? "";
            CommentsBox.Text = p.Comments ?? "";

            ApplySkuAluRule();
        }

        // RULE: user can enter SKU OR ALU, not both
        private void SkuBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySkuAluRule();
        private void AluBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySkuAluRule();

        private void ApplySkuAluRule()
        {
            var skuHas = !string.IsNullOrWhiteSpace(SkuBox.Text);
            var aluHas = !string.IsNullOrWhiteSpace(AluBox.Text);

            AluBox.IsEnabled = !skuHas;
            SkuBox.IsEnabled = !aluHas;
        }

        private void SkuBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void SkuBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string))!;
                if (!Regex.IsMatch(text, @"^\d+$"))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private bool ValidateInputs(out string error)
        {
            error = "";

            var name = NameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Item Name is required.";
                return false;
            }

            var dept = DepartmentBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(dept))
            {
                error = "Department is required.";
                return false;
            }

            if (!decimal.TryParse(PriceBox.Text.Trim(), out var price) || price < 0)
            {
                error = "Enter a valid Reg Price.";
                return false;
            }

            if (!int.TryParse(StockBox.Text.Trim(), out var stock) || stock < 0)
            {
                error = "Enter a valid On-Hand Qty.";
                return false;
            }

            var sku = SkuBox.Text.Trim();
            var alu = AluBox.Text.Trim();
            if (!string.IsNullOrEmpty(sku) && !string.IsNullOrEmpty(alu))
            {
                error = "You can enter either SKU or ALU, not both.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(sku) && !Regex.IsMatch(sku, @"^\d+$"))
            {
                error = "SKU must contain numbers only.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(CostPriceBox.Text) &&
                (!decimal.TryParse(CostPriceBox.Text.Trim(), out var _cp) || _cp < 0))
            {
                error = "Enter a valid Cost Price (or leave it blank).";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(ReorderPointBox.Text) &&
                (!int.TryParse(ReorderPointBox.Text.Trim(), out var _rp) || _rp < 0))
            {
                error = "Enter a valid Reorder Point (or leave it blank).";
                return false;
            }

            return true;
        }

        private void AddSupplier_Click(object sender, RoutedEventArgs e)
        {
            var win = new SupplierEditWindow();
            var result = win.ShowDialog();
            if (result == true)
            {
                LoadLookups();
                using var db = new AppDbContext();
                var latest = db.Suppliers.OrderByDescending(s => s.Id).FirstOrDefault();
                if (latest != null)
                    VendorBox.Text = latest.Name;
            }
            else
            {
                VendorBox.IsDropDownOpen = true;
                VendorBox.Focus();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out var error))
            {
                MessageBox.Show(error);
                return;
            }

            SaveInternal();
            if (Saved) Close();
        }

        private void SaveAndNew_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out var error))
            {
                MessageBox.Show(error);
                return;
            }

            SaveInternal();

            if (Saved)
                ClearFormForNew();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Saved = false;
            Close();
        }

        private void SaveInternal()
        {
            using var db = new AppDbContext();

            Product p;
            if (_editId == null)
            {
                p = new Product();
                db.Products.Add(p);
            }
            else
            {
                p = db.Products.First(x => x.Id == _editId.Value);
            }

            p.Name = NameBox.Text.Trim();
            p.Price = decimal.Parse(PriceBox.Text.Trim());
            p.StockQty = int.Parse(StockBox.Text.Trim());
            p.SKU = SkuBox.Text.Trim();

            p.Department = DepartmentBox.Text.Trim();
            p.Description = DescriptionBox.Text.Trim();
            p.Size = SizeBox.Text.Trim();
            p.Tax = GetTaxValue();
            p.ALU = AluBox.Text.Trim();
            p.Supplier = VendorBox.Text.Trim();
            p.UnitOfMeasure = (UomBox.Text ?? "Each").Trim();
            p.Manufacturer = ManufacturerBox.Text.Trim();
            p.Comments = CommentsBox.Text.Trim();

            p.CostPrice = string.IsNullOrWhiteSpace(CostPriceBox.Text) ? 0 : decimal.Parse(CostPriceBox.Text.Trim());
            p.ReorderPoint = string.IsNullOrWhiteSpace(ReorderPointBox.Text) ? 0 : int.Parse(ReorderPointBox.Text.Trim());

            db.SaveChanges();
            Saved = true;
        }

        private void ClearFormForNew()
        {
            NameBox.Text = "";
            DepartmentBox.Text = "";
            DescriptionBox.Text = "";
            SizeBox.Text = "";
            PriceBox.Text = "";
            CostPriceBox.Text = "";
            StockBox.Text = "";
            SkuBox.Text = "";

            VendorBox.Text = "";
            ReorderPointBox.Text = "";
            ItemNoBox.Text = "(auto)";
            AluBox.Text = "";
            ManufacturerBox.Text = "";
            CommentsBox.Text = "";

            LoadTaxField(null);
            UomBox.SelectedIndex = 0;

            ApplySkuAluRule();
            NameBox.Focus();
        }

        // ═══════════════════════════════════════
        // TAX HELPERS
        // ═══════════════════════════════════════

        private void TaxBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TaxCustomBox == null) return;
            var tag = (TaxBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            TaxCustomBox.Visibility = tag == "custom" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadTaxField(string? taxValue)
        {
            if (TaxBox == null) return;
            var t = (taxValue ?? "").Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(t) || t == "no tax" || t == "notax"
                || t == "none" || t == "0" || t == "exempt" || t == "no" || t == "false")
            {
                TaxBox.SelectedIndex = 0;
                TaxCustomBox.Visibility = System.Windows.Visibility.Collapsed;
            }
            else if (t == "15" || t == "vat" || t == "tax" || t == "15% vat" || t == "yes" || t == "true" || t == "1")
            {
                TaxBox.SelectedIndex = 1;
                TaxCustomBox.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                TaxBox.SelectedIndex = 2;
                TaxCustomBox.Text = t.Replace("%", "").Trim();
                TaxCustomBox.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private string GetTaxValue()
        {
            var tag = (TaxBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "No Tax";

            if (tag == "custom")
            {
                var raw = TaxCustomBox.Text.Replace("%", "").Trim();
                if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var rate) && rate > 0)
                    return rate.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return "No Tax";
            }

            return tag;
        }
    }
}