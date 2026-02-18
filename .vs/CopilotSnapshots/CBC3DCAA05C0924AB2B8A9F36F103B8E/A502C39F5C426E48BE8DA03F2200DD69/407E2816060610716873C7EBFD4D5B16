using PointofSale.Data;
using PointofSale.Models;
using System.Windows.Input;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PointofSale
{
    public partial class ProductsWindow : Window
    {
        private Product? _selected;

        public ProductsWindow()
        {
            InitializeComponent();

            ProductsGrid.SelectionChanged += ProductsGrid_SelectionChanged;

            LoadProducts();
            SetMessage("Ready.");
        }

        private void LoadProducts()
        {
            using var db = new AppDbContext();
            var list = db.Products.OrderBy(p => p.Name).ToList();
            ProductsGrid.ItemsSource = list;
        }

        private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selected = ProductsGrid.SelectedItem as Product;

            if (_selected == null)
                return;

            NameBox.Text = _selected.Name;
            SkuBox.Text = _selected.SKU;
            PriceBox.Text = _selected.Price.ToString("0.00");
            StockBox.Text = _selected.StockQty.ToString();

            SetMessage($"Editing: {_selected.Name}");
        }

        private void SkuScan_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            // Move to next field after scan
            PriceBox.Focus(); // rename to your price textbox name
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts();
            SetMessage("Refreshed.");
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            _selected = null;
            SetMessage("Adding a new product.");
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            _selected = null;
            SetMessage("Cleared.");
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text.Trim();
            var sku = SkuBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(sku))
            {
                MessageBox.Show("SKU is required.");
                return;
            }

            if (!decimal.TryParse(PriceBox.Text.Trim(), out var price) || price < 0)
            {
                MessageBox.Show("Enter a valid price.");
                return;
            }

            if (!int.TryParse(StockBox.Text.Trim(), out var stock) || stock < 0)
            {
                MessageBox.Show("Enter a valid stock quantity.");
                return;
            }

            using var db = new AppDbContext();

            // SKU uniqueness check
            var skuExists = db.Products.Any(p => p.SKU == sku && (_selected == null || p.Id != _selected.Id));
            if (skuExists)
            {
                MessageBox.Show("That SKU already exists. SKU must be unique.");
                return;
            }

            if (_selected == null)
            {
                // Create
                var newProduct = new Product
                {
                    Name = name,
                    SKU = sku,
                    Price = price,
                    StockQty = stock
                };

                db.Products.Add(newProduct);
                db.SaveChanges();

                SetMessage($"Added: {newProduct.Name}");
            }
            else
            {
                // Update
                var prod = db.Products.FirstOrDefault(p => p.Id == _selected.Id);
                if (prod == null)
                {
                    MessageBox.Show("Product not found. Refresh and try again.");
                    return;
                }

                prod.Name = name;
                prod.SKU = sku;
                prod.Price = price;
                prod.StockQty = stock;

                db.SaveChanges();

                SetMessage($"Saved: {prod.Name}");
            }

            LoadProducts();
            ClearForm();
            _selected = null;
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null)
            {
                MessageBox.Show("Select a product first.");
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete '{_selected.Name}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            using var db = new AppDbContext();
            var prod = db.Products.FirstOrDefault(p => p.Id == _selected.Id);

            if (prod == null)
            {
                MessageBox.Show("Product not found. Refresh and try again.");
                return;
            }

            db.Products.Remove(prod);
            db.SaveChanges();

            SetMessage($"Deleted: {prod.Name}");

            LoadProducts();
            ClearForm();
            _selected = null;
        }

        private void ClearForm()
        {
            NameBox.Text = "";
            SkuBox.Text = "";
            PriceBox.Text = "";
            StockBox.Text = "";
            ProductsGrid.SelectedItem = null;
        }

        private void SetMessage(string msg)
        {
            MsgText.Text = msg;
        }
    }
}
