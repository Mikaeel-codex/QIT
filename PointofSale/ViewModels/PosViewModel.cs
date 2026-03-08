using PointofSale.Data;
using PointofSale.Helpers;
using PointofSale.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace PointofSale.ViewModels
{
    public class PosViewModel : ObservableObject
    {
        public ObservableCollection<CartLine> Cart { get; } = new();

        private string _scanText = "";
        public string ScanText
        {
            get => _scanText;
            set => Set(ref _scanText, value);
        }

        private string _customerSearchText = "";
        public string CustomerSearchText
        {
            get => _customerSearchText;
            set => Set(ref _customerSearchText, value);
        }

        private decimal _cashTendered;
        public decimal CashTendered
        {
            get => _cashTendered;
            set
            {
                if (Set(ref _cashTendered, value))
                {
                    OnPropertyChanged(nameof(CashChangeDisplay));
                    OnPropertyChanged(nameof(CashChange));
                    OnPropertyChanged(nameof(AmountDue));
                    OnPropertyChanged(nameof(AmountDueIsZero));
                    OnPropertyChanged(nameof(CashTenderedVisibility));
                    OnPropertyChanged(nameof(CashChangeVisibility));
                }
            }
        }

        private string _paymentMethod = "";
        public string PaymentMethod
        {
            get => _paymentMethod;
            set => Set(ref _paymentMethod, value);
        }

        public int ItemsSold => Cart.Count;
        public int TotalQtySold => Cart.Sum(x => x.Qty);
        public decimal Subtotal => Cart.Sum(x => x.LineTotal);

        // Tax is calculated per line using each line's individual TaxRate
        // so items marked "No Tax" contribute 0 regardless of other lines
        public decimal Tax => Math.Round(Cart.Sum(x => x.LineTotal * x.TaxRate / 100m), 2);
        public decimal Total => Subtotal + Tax;

        public decimal AmountDue => CashTendered > 0
            ? Math.Max(0, Math.Round(Total - CashTendered, 2))
            : Total;

        public decimal CashChange => CashTendered > 0
            ? Math.Max(0, Math.Round(CashTendered - Total, 2))
            : 0;

        public DateTime SaleDate { get; } = DateTime.Today;

        public bool AmountDueIsZero => AmountDue == 0;

        public Visibility CashTenderedVisibility =>
            CashTendered > 0 ? Visibility.Visible : Visibility.Collapsed;

        public Visibility CashChangeVisibility =>
            CashChange > 0 ? Visibility.Visible : Visibility.Collapsed;

        public string CashChangeDisplay
        {
            get
            {
                if (CashTendered <= 0) return "";
                var change = Math.Round(CashTendered - Total, 2);
                return change >= 0 ? $"Cash Change  {change:N2}" : "";
            }
        }

        public RelayCommand<CartLine> IncreaseQtyCommand { get; }
        public RelayCommand<CartLine> DecreaseQtyCommand { get; }
        public RelayCommand<CartLine> RemoveFromCartCommand { get; }

        public PosViewModel()
        {
            Cart.CollectionChanged += Cart_CollectionChanged;

            IncreaseQtyCommand = new RelayCommand<CartLine>(line =>
            {
                if (line == null) return;
                using var db = new AppDbContext();
                var p = db.Products.FirstOrDefault(x => x.Id == line.ProductId);
                if (p == null) return;
                if (line.Qty + 1 > p.StockQty) { MessageBox.Show("Not enough stock."); return; }
                line.Qty += 1;
                RefreshTotals();
            });

            DecreaseQtyCommand = new RelayCommand<CartLine>(line =>
            {
                if (line == null) return;
                line.Qty -= 1;
                if (line.Qty == 0) Cart.Remove(line);
                RefreshTotals();
            });

            RemoveFromCartCommand = new RelayCommand<CartLine>(line =>
            {
                if (line == null) return;
                Cart.Remove(line);
                RefreshTotals();
            });
        }

        private void Cart_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (var item in e.NewItems)
                    if (item is CartLine line)
                        line.PropertyChanged += CartLine_PropertyChanged;

            if (e.OldItems != null)
                foreach (var item in e.OldItems)
                    if (item is CartLine line)
                        line.PropertyChanged -= CartLine_PropertyChanged;

            RefreshTotals();
        }

        private void CartLine_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartLine.Qty) ||
                e.PropertyName == nameof(CartLine.UnitPrice) ||
                e.PropertyName == nameof(CartLine.LineTotal))
                RefreshTotals();
        }

        public void RefreshTotals()
        {
            OnPropertyChanged(nameof(ItemsSold));
            OnPropertyChanged(nameof(TotalQtySold));
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Tax));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(AmountDue));
            OnPropertyChanged(nameof(CashChange));
            OnPropertyChanged(nameof(CashChangeDisplay));
            OnPropertyChanged(nameof(AmountDueIsZero));
            OnPropertyChanged(nameof(CashTenderedVisibility));
            OnPropertyChanged(nameof(CashChangeVisibility));
        }

        public void ScanSkuAndAddToCart(string skuOrAlu)
        {
            skuOrAlu = (skuOrAlu ?? "").Trim();
            if (string.IsNullOrWhiteSpace(skuOrAlu)) return;

            using var db = new AppDbContext();
            var p = db.Products.FirstOrDefault(x =>
                (x.SKU != null && x.SKU == skuOrAlu) ||
                (x.ALU != null && x.ALU == skuOrAlu));

            if (p == null) { MessageBox.Show("Item not found.", "Not Found"); return; }
            if (p.StockQty <= 0) { MessageBox.Show("Item is out of stock.", "Out of Stock"); return; }

            var existing = Cart.FirstOrDefault(x => x.ProductId == p.Id);
            if (existing != null)
            {
                if (existing.Qty + 1 > p.StockQty) { MessageBox.Show("Not enough stock to add more.", "Stock Limit"); return; }
                existing.Qty += 1;
            }
            else
            {
                var taxRate = ParseTaxRate(p.Tax);
                Cart.Add(new CartLine
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    SKU = p.SKU ?? "",
                    UnitPrice = p.Price,
                    Qty = 1,
                    Size = p.Size ?? "",
                    Attribute = p.Department ?? "",
                    TaxRate = taxRate,
                    TaxCode = taxRate > 0 ? $"{taxRate}%" : "No Tax"
                });
            }

            RefreshTotals();
        }

        /// <summary>
        /// Parses the product Tax field into a decimal rate (percentage).
        /// "No Tax", "None", "0", empty  -> 0
        /// "15" or "15%"                 -> 15 (stored directly)
        /// "VAT", "Tax", "yes"           -> 15 (SA standard VAT)
        /// </summary>
        private static decimal ParseTaxRate(string? taxField)
        {
            var t = (taxField ?? "").Trim().ToLowerInvariant();

            // Explicit zero cases
            if (string.IsNullOrWhiteSpace(t)) return 0m;
            if (t == "no tax") return 0m;
            if (t == "notax") return 0m;
            if (t == "none") return 0m;
            if (t == "exempt") return 0m;
            if (t == "no" || t == "n") return 0m;
            if (t == "false" || t == "0") return 0m;

            // Numeric rate stored directly e.g. "15" or "15%"
            var clean = t.Replace("%", "").Trim();
            if (decimal.TryParse(clean, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var rate))
                return rate;

            // Named taxable keywords -> read configured rate from DB
            if (t == "vat" || t == "tax" || t == "yes" || t == "y" || t == "true" || t == "1")
                return 15m; // SA standard VAT

            return 0m;
        }

        public void RemovePaymentMethod()
        {
            CashTendered = 0;
            PaymentMethod = "";
            RefreshTotals();
        }

        /// <summary>Restores a held receipt back into the cart.</summary>
        public void RestoreFromHeld(PointofSale.Models.HeldReceipt held)
        {
            Cart.Clear();
            CashTendered = 0;
            PaymentMethod = held.PaymentMethod ?? "";

            foreach (var item in held.Items)
            {
                Cart.Add(new CartLine
                {
                    ProductId = item.ProductId,
                    SKU = item.SKU,
                    Name = item.Name,
                    Attribute = item.Attribute,
                    Size = item.Size,
                    Qty = item.Qty,
                    UnitPrice = item.UnitPrice,
                    TaxCode = item.TaxCode,
                    TaxRate = item.TaxRate
                });
            }

            RefreshTotals();
        }

        public void FinalizeSale(string paymentType)
        {
            if (Cart.Count == 0) { MessageBox.Show("Nothing to save."); return; }

            try
            {
                using var db = new AppDbContext();

                foreach (var line in Cart)
                {
                    var p = db.Products.FirstOrDefault(x => x.Id == line.ProductId);
                    if (p == null) throw new Exception("A product no longer exists.");
                    if (line.Qty > p.StockQty) throw new Exception($"Not enough stock for {p.Name}.");
                }

                foreach (var line in Cart)
                {
                    var p = db.Products.First(x => x.Id == line.ProductId);
                    p.StockQty -= line.Qty;
                }

                db.SaveChanges();

                Cart.Clear();
                CashTendered = 0;
                PaymentMethod = "";
                MessageBox.Show($"Sale completed ({paymentType}).");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        public void PutOnHold(string cashierName = "")
        {
            if (Cart.Count == 0) { MessageBox.Show("Nothing to hold."); return; }

            try
            {
                using var db = new AppDbContext();

                // Build a short summary e.g. "Pepsi x1, Lays x2" for the list view
                var summary = string.Join(", ", Cart.Select(l => $"{l.Name} x{l.Qty}"));

                var held = new PointofSale.Models.HeldReceipt
                {
                    HeldAt = DateTime.Now,
                    Cashier = cashierName,
                    PaymentMethod = PaymentMethod,
                    Subtotal = Subtotal,
                    Tax = Tax,
                    Total = Total,
                    ItemCount = ItemsSold,
                    TotalQty = TotalQtySold,
                    ItemsSummary = summary
                };

                foreach (var line in Cart)
                {
                    held.Items.Add(new PointofSale.Models.HeldReceiptItem
                    {
                        ProductId = line.ProductId,
                        SKU = line.SKU,
                        Name = line.Name,
                        Attribute = line.Attribute,
                        Size = line.Size,
                        Qty = line.Qty,
                        UnitPrice = line.UnitPrice,
                        LineTotal = line.LineTotal,
                        TaxCode = line.TaxCode,
                        TaxRate = line.TaxRate
                    });
                }

                db.HeldReceipts.Add(held);
                db.SaveChanges();

                Cart.Clear();
                CashTendered = 0;
                PaymentMethod = "";
                MessageBox.Show("Receipt put on hold.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to hold receipt: {ex.Message}", "Error");
            }
        }

        public void CancelReceipt()
        {
            if (Cart.Count == 0) { MessageBox.Show("Nothing to cancel."); return; }
            Cart.Clear();
            CashTendered = 0;
            PaymentMethod = "";
            MessageBox.Show("Receipt cancelled.");
        }
    }
}