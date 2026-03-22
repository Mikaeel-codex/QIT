using PointofSale.Data;
using PointofSale.Helpers;
using PointofSale.Models;
using System;
using System.Collections.Generic;
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

        // Tax code dropdown options — loaded from products in DB
        public List<string> TaxCodeOptions { get; } = new();

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

        // ── Payment splits ───────────────────────────────────────────────
        public ObservableCollection<PaymentSplit> Splits { get; } = new();

        // Total amount tendered across all splits
        public decimal TotalTendered => Splits.Sum(s => s.Amount);

        // How much is still unpaid
        public decimal AmountDue => Math.Max(0, Math.Round(Total - TotalTendered, 2));

        // Change only applies when cash is involved and overpaid
        public decimal CashChange
        {
            get
            {
                if (TotalTendered <= Total) return 0;
                // Only give change on cash overpayment
                var cashTotal = Splits.Where(s => s.Method == "Cash").Sum(s => s.Amount);
                var nonCash = Splits.Where(s => s.Method != "Cash").Sum(s => s.Amount);
                var overpaid = TotalTendered - Total;
                return cashTotal > 0 ? Math.Round(overpaid, 2) : 0;
            }
        }

        public bool AmountDueIsZero => AmountDue == 0;
        public bool HasSplits => Splits.Count > 0;
        public string PaymentMethod => Splits.Count == 1 ? Splits[0].Method
                                        : Splits.Count > 1 ? "Split" : "";

        // Legacy compat — used by totals panel visibility
        public decimal CashTendered => TotalTendered;
        public Visibility CashTenderedVisibility =>
            TotalTendered > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CashChangeVisibility =>
            CashChange > 0 ? Visibility.Visible : Visibility.Collapsed;

        // Pending gift card — card ID waiting to be deducted on finalize
        public int PendingGiftCardId { get; set; } = 0;
        public decimal PendingGiftCardAmount { get; set; } = 0m;

        public int ItemsSold => Cart.Count;
        public int TotalQtySold => Cart.Sum(x => x.Qty);
        public decimal Subtotal => Cart.Sum(x => x.LineTotal);

        // Tax is calculated per line using each line's individual TaxRate
        // so items marked "No Tax" contribute 0 regardless of other lines
        public decimal Tax => Math.Round(Cart.Sum(x => x.LineTotal * x.TaxRate / 100m), 2);
        public decimal Total => Subtotal + Tax;

        public DateTime SaleDate { get; } = DateTime.Today;

        public string CashChangeDisplay => CashChange > 0 ? $"Cash Change  {CashChange:N2}" : "";

        /// <summary>
        /// Set this from the UI layer to receive low stock warnings
        /// from IncreaseQtyCommand (Qty+ button on cart row).
        /// </summary>
        public Action<LowStockWarning>? OnLowStock { get; set; }

        public RelayCommand<CartLine> IncreaseQtyCommand { get; }
        public RelayCommand<CartLine> DecreaseQtyCommand { get; }
        public RelayCommand<CartLine> RemoveFromCartCommand { get; }

        public PosViewModel()
        {
            Cart.CollectionChanged += Cart_CollectionChanged;

            // Load tax code options from existing products
            try
            {
                using var db = new AppDbContext();
                var codes = db.Products
                    .Select(p => p.Tax)
                    .Where(t => t != null && t != "")
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                // Always include the standard options
                foreach (var standard in new[] { "No Tax", "Tax", "5%", "7.5%", "10%", "15%", "20%" })
                    if (!codes.Contains(standard))
                        codes.Add(standard);

                codes.Sort();
                TaxCodeOptions.AddRange(codes);
            }
            catch
            {
                // Fallback if DB not ready
                TaxCodeOptions.AddRange(new[] { "No Tax", "Tax", "5%", "10%", "15%", "20%" });
            }

            IncreaseQtyCommand = new RelayCommand<CartLine>(line =>
            {
                if (line == null) return;
                using var db = new AppDbContext();
                var p = db.Products.FirstOrDefault(x => x.Id == line.ProductId);
                if (p == null) return;
                if (line.Qty + 1 > p.StockQty) { MessageBox.Show("Not enough stock."); return; }
                line.Qty += 1;
                RefreshTotals();

                // Check reorder point after qty increase
                if (p.ReorderPoint > 0)
                {
                    var remaining = p.StockQty - line.Qty;
                    if (remaining <= p.ReorderPoint)
                        OnLowStock?.Invoke(new LowStockWarning
                        {
                            ProductName = p.Name,
                            CurrentStock = p.StockQty,
                            CartQty = line.Qty,
                            ReorderPoint = p.ReorderPoint,
                            Remaining = remaining
                        });
                }
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
                e.PropertyName == nameof(CartLine.LineTotal) ||
                e.PropertyName == nameof(CartLine.TaxCode) ||
                e.PropertyName == nameof(CartLine.TaxRate))
                RefreshTotals();
        }

        public void RefreshTotals()
        {
            OnPropertyChanged(nameof(ItemsSold));
            OnPropertyChanged(nameof(TotalQtySold));
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Tax));
            OnPropertyChanged(nameof(Total));
            RefreshPayment();
        }

        public void RefreshPayment()
        {
            OnPropertyChanged(nameof(TotalTendered));
            OnPropertyChanged(nameof(AmountDue));
            OnPropertyChanged(nameof(CashChange));
            OnPropertyChanged(nameof(CashChangeDisplay));
            OnPropertyChanged(nameof(AmountDueIsZero));
            OnPropertyChanged(nameof(CashTendered));
            OnPropertyChanged(nameof(CashTenderedVisibility));
            OnPropertyChanged(nameof(CashChangeVisibility));
            OnPropertyChanged(nameof(PaymentMethod));
            OnPropertyChanged(nameof(HasSplits));
        }

        public void AddSplit(string method, decimal amount, int giftCardId = 0, string label = "")
        {
            // If same method already exists (non-gift), update amount instead of adding
            var existing = Splits.FirstOrDefault(s => s.Method == method && s.GiftCardId == 0 && giftCardId == 0);
            if (existing != null)
            {
                existing.Amount = amount;
                existing.Label = string.IsNullOrEmpty(label) ? method : label;
            }
            else
            {
                Splits.Add(new PaymentSplit
                {
                    Method = method,
                    Amount = amount,
                    GiftCardId = giftCardId,
                    Label = string.IsNullOrEmpty(label) ? method : label
                });
            }
            RefreshPayment();
        }

        public void ClearSplits()
        {
            Splits.Clear();
            PendingGiftCardId = 0;
            PendingGiftCardAmount = 0m;
            RefreshPayment();
        }

        /// <summary>
        /// Adds a product to the cart by SKU or ALU.
        /// Returns a LowStockWarning if the product is at or below its reorder point
        /// after being added, otherwise returns null.
        /// </summary>
        public LowStockWarning? ScanSkuAndAddToCart(string skuOrAlu)
        {
            skuOrAlu = (skuOrAlu ?? "").Trim();
            if (string.IsNullOrWhiteSpace(skuOrAlu)) return null;

            using var db = new AppDbContext();
            var p = db.Products.FirstOrDefault(x =>
                (x.SKU != null && x.SKU == skuOrAlu) ||
                (x.ALU != null && x.ALU == skuOrAlu));

            if (p == null) { MessageBox.Show("Item not found.", "Not Found"); return null; }
            if (p.StockQty <= 0) { MessageBox.Show("Item is out of stock.", "Out of Stock"); return null; }

            var existing = Cart.FirstOrDefault(x => x.ProductId == p.Id);
            if (existing != null)
            {
                if (existing.Qty + 1 > p.StockQty) { MessageBox.Show("Not enough stock to add more.", "Stock Limit"); return null; }
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
                    CostPrice = p.AvgUnitCost,
                    StockQty = p.StockQty,
                    Qty = 1,
                    Size = p.Size ?? "",
                    Attribute = p.Department ?? "",
                    TaxRate = taxRate,
                    TaxCode = taxRate > 0 ? $"{taxRate}%" : "No Tax"
                });
            }

            RefreshTotals();

            // Check reorder point — cart qty is what will be deducted on finalize
            if (p.ReorderPoint > 0)
            {
                var cartQty = Cart.First(x => x.ProductId == p.Id).Qty;
                var remaining = p.StockQty - cartQty;
                if (remaining <= p.ReorderPoint)
                    return new LowStockWarning
                    {
                        ProductName = p.Name,
                        CurrentStock = p.StockQty,
                        CartQty = cartQty,
                        ReorderPoint = p.ReorderPoint,
                        Remaining = remaining
                    };
            }

            return null;
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
            ClearSplits();
        }

        /// <summary>Restores a held receipt back into the cart.</summary>
        public void RestoreFromHeld(PointofSale.Models.HeldReceipt held)
        {
            Cart.Clear();
            ClearSplits();

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

        public ReceiptData? FinalizeSale(string paymentType, string cashierName = "")
        {
            if (Cart.Count == 0) { MessageBox.Show("Nothing to save."); return null; }

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

                // Deduct all gift card splits now that sale is confirmed
                foreach (var split in Splits.Where(s => s.GiftCardId > 0))
                {
                    var giftCard = db.GiftCards.Find(split.GiftCardId);
                    if (giftCard != null)
                    {
                        giftCard.Balance -= split.Amount;
                        giftCard.LastUsedAt = DateTime.Now;
                        giftCard.Status = giftCard.Balance <= 0 ? "Depleted" : "Active";
                    }
                }
                db.SaveChanges();

                var splitSummary = Splits.Count == 1
                    ? Splits[0].Method
                    : string.Join(" + ", Splits.Select(s => $"{s.Method} {s.Amount:N2}"));

                // Build receipt data before clearing
                var receiptData = new ReceiptData
                {
                    ReceiptNumber = DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                    SaleDate = DateTime.Now,
                    Cashier = cashierName,
                    CustomerName = CustomerSearchText,
                    Subtotal = Subtotal,
                    Tax = Tax,
                    Total = Total,
                    AmountDue = AmountDue,
                    CashChange = CashChange,
                    Lines = Cart.Select(l => new ReceiptLineItem
                    {
                        SKU = l.SKU ?? "",
                        Name = l.Name ?? "",
                        Attribute = l.Attribute ?? "",
                        Size = l.Size ?? "",
                        Qty = l.Qty,
                        UnitPrice = l.UnitPrice,
                        LineTotal = l.LineTotal,
                        TaxCode = l.TaxCode ?? "",
                        DiscountPct = l.DiscountPct,
                    }).ToList(),
                    Payments = Splits.Select(s => new ReceiptPaymentLine
                    {
                        Label = s.Label,
                        Amount = s.Amount,
                    }).ToList(),
                };

                Cart.Clear();
                ClearSplits();
                MessageBox.Show($"Sale completed.\nPayment: {splitSummary}");
                return receiptData;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
                return null;
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
                    PaymentMethod = "",   // manual hold has no confirmed payment
                    Subtotal = Subtotal,
                    Tax = Tax,
                    Total = Total,
                    ItemCount = ItemsSold,
                    TotalQty = TotalQtySold,
                    ItemsSummary = summary,
                    Status = "Held",
                    Note = ""
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
                ClearSplits();
                MessageBox.Show("Receipt put on hold.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to hold receipt: {ex.Message}", "Error");
            }
        }

        /// <summary>Put cart on hold with a custom note and status (used for pending EFT).</summary>
        public bool PutOnHoldWithNote(string cashierName, string note, string status)
        {
            if (Cart.Count == 0) return false;
            try
            {
                using var db = new AppDbContext();
                var summary = string.Join(", ", Cart.Select(l => $"{l.Name} x{l.Qty}"));
                var held = new PointofSale.Models.HeldReceipt
                {
                    HeldAt = DateTime.Now,
                    Cashier = cashierName,
                    PaymentMethod = "EFT",
                    Subtotal = Subtotal,
                    Tax = Tax,
                    Total = Total,
                    ItemCount = ItemsSold,
                    TotalQty = TotalQtySold,
                    ItemsSummary = summary,
                    Note = note,
                    Status = status
                };
                foreach (var line in Cart)
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
                db.HeldReceipts.Add(held);
                db.SaveChanges();
                Cart.Clear();
                ClearSplits();
                return true;
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Failed to hold receipt: {inner}", "Error");
                return false;
            }
        }

        public void CancelReceipt()
        {
            if (Cart.Count == 0) { MessageBox.Show("Nothing to cancel."); return; }
            Cart.Clear();
            ClearSplits();
            MessageBox.Show("Receipt cancelled.");
        }
    }
}