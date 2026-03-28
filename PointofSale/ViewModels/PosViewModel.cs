using PointofSale.Data;
using PointofSale.Helpers;
using PointofSale.Models;
using PointofSale.Services;
using QuickInventoryTill.Models;
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

        // For a normal sale: how much is still unpaid.
        // For a return (Total < 0): how much still needs to be refunded to the customer.
        public decimal AmountDue
        {
            get
            {
                if (Total < 0)
                    return Math.Max(0, Math.Round(Math.Abs(Total) - Math.Abs(TotalTendered), 2));
                return Math.Max(0, Math.Round(Total - TotalTendered, 2));
            }
        }

        // Change only applies when cash is involved and overpaid
        public decimal CashChange
        {
            get
            {
                if (Total < 0)
                {
                    // Return: change only if cashier over-refunded
                    var absRefunded = Math.Abs(TotalTendered);
                    var absOwed     = Math.Abs(Total);
                    if (absRefunded <= absOwed) return 0;
                    var cashSplits = Splits.Where(s => s.Method == "Cash").Sum(s => Math.Abs(s.Amount));
                    return cashSplits > 0 ? Math.Round(absRefunded - absOwed, 2) : 0;
                }
                if (TotalTendered <= Total) return 0;
                var cashTotal = Splits.Where(s => s.Method == "Cash").Sum(s => s.Amount);
                var overpaid  = TotalTendered - Total;
                return cashTotal > 0 ? Math.Round(overpaid, 2) : 0;
            }
        }

        public bool AmountDueIsZero => AmountDue == 0;
        public bool HasSplits => Splits.Count > 0;
        public string PaymentMethod => Splits.Count == 1 ? Splits[0].Method
                                        : Splits.Count > 1 ? "Split" : "";

        // Legacy compat
        public decimal CashTendered => TotalTendered;
        public Visibility CashTenderedVisibility =>
            TotalTendered > 0 ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CashChangeVisibility =>
            CashChange > 0 ? Visibility.Visible : Visibility.Collapsed;

        // Pending gift card
        public int PendingGiftCardId { get; set; } = 0;
        public decimal PendingGiftCardAmount { get; set; } = 0m;

        public int ItemsSold => Cart.Count;
        public int TotalQtySold => Cart.Sum(x => x.Qty);
        public decimal Subtotal => Cart.Sum(x => x.LineTotal);
        public decimal Tax => Math.Round(Cart.Sum(x => x.LineTotal * x.TaxRate / 100m), 2);
        public decimal Total => Subtotal + Tax;

        public DateTime SaleDate { get; } = DateTime.Today;

        public string CashChangeDisplay => CashChange > 0 ? $"Cash Change  {CashChange:N2}" : "";

        public Action<LowStockWarning>? OnLowStock { get; set; }

        public RelayCommand<CartLine> IncreaseQtyCommand { get; }
        public RelayCommand<CartLine> DecreaseQtyCommand { get; }
        public RelayCommand<CartLine> RemoveFromCartCommand { get; }

        public PosViewModel()
        {
            Cart.CollectionChanged += Cart_CollectionChanged;

            try
            {
                using var db = new AppDbContext();
                var codes = db.Products
                    .Select(p => p.Tax)
                    .Where(t => t != null && t != "")
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                foreach (var standard in new[] { "No Tax", "Tax", "5%", "7.5%", "10%", "15%", "20%" })
                    if (!codes.Contains(standard))
                        codes.Add(standard);

                codes.Sort();
                TaxCodeOptions.AddRange(codes);
            }
            catch
            {
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
                    CostPrice = p.CostPrice,
                    StockQty = p.StockQty,
                    Qty = 1,
                    Size = p.Size ?? "",
                    Attribute = p.Department ?? "",
                    TaxRate = taxRate,
                    TaxCode = taxRate > 0 ? $"{taxRate}%" : "No Tax"
                });
            }

            RefreshTotals();

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

        private static decimal ParseTaxRate(string? taxField)
        {
            var t = (taxField ?? "").Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(t)) return 0m;
            if (t == "no tax") return 0m;
            if (t == "notax") return 0m;
            if (t == "none") return 0m;
            if (t == "exempt") return 0m;
            if (t == "no" || t == "n") return 0m;
            if (t == "false" || t == "0") return 0m;

            var clean = t.Replace("%", "").Trim();
            if (decimal.TryParse(clean, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out var rate))
                return rate;

            if (t == "vat" || t == "tax" || t == "yes" || t == "y" || t == "true" || t == "1")
                return 15m;

            return 0m;
        }

        public void RemovePaymentMethod()
        {
            ClearSplits();
        }

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

                // ── Validate stock (only for sale lines, not returns) ─────
                foreach (var line in Cart.Where(l => !l.IsReturn))
                {
                    var p = db.Products.FirstOrDefault(x => x.Id == line.ProductId);
                    if (p == null) throw new Exception("A product no longer exists.");
                    if (line.Qty > p.StockQty) throw new Exception($"Not enough stock for {p.Name}.");
                }

                // ── Update stock ──────────────────────────────────────────
                foreach (var line in Cart)
                {
                    var p = db.Products.First(x => x.Id == line.ProductId);
                    if (line.IsReturn)
                        p.StockQty += Math.Abs(line.Qty);   // restore stock for returns
                    else
                        p.StockQty -= line.Qty;             // deduct stock for sales
                }
                db.SaveChanges();

                // ── Deduct gift card balances ─────────────────────────────
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

                // ── Update customer account balance for Account payments ──
                var accountTotal = Splits
                    .Where(s => s.Method == "Account")
                    .Sum(s => s.Amount);

                if (accountTotal > 0)
                {
                    if (string.IsNullOrWhiteSpace(CustomerSearchText))
                    {
                        MessageBox.Show(
                            $"Account payment of R{accountTotal:N2} could not be applied.\n\n" +
                            "No customer is attached to this sale.\n" +
                            "Please search and select a customer before using Account payment.",
                            "Account Payment — No Customer",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                    else
                    {
                        var customerName = CustomerSearchText.Trim();
                        var customer = db.Customers.FirstOrDefault(c =>
                            c.IsActive &&
                            (c.FirstName + " " + c.LastName).Trim() == customerName);

                        if (customer == null)
                        {
                            MessageBox.Show(
                                $"Account payment of R{accountTotal:N2} could not be applied.\n\n" +
                                $"No active customer named '{customerName}' was found.\n" +
                                "Please attach a valid customer before using Account payment.",
                                "Account Payment — Customer Not Found",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                        else
                        {
                            // Hard block — do not allow exceeding credit limit
                            if (customer.CreditLimit > 0 &&
                                customer.AccountBalance + accountTotal > customer.CreditLimit)
                            {
                                var available = customer.CreditLimit - customer.AccountBalance;
                                MessageBox.Show(
                                    $"Account payment blocked for {customerName}.\n\n" +
                                    $"Current balance:   R{customer.AccountBalance:N2}\n" +
                                    $"Credit limit:      R{customer.CreditLimit:N2}\n" +
                                    $"Available credit:  R{available:N2}\n\n" +
                                    $"This charge of R{accountTotal:N2} would exceed the credit limit.\n\n" +
                                    "Please use a different payment method or ask the customer to settle their account first.\n" +
                                    "An admin can increase the credit limit in the Customers screen.",
                                    "Credit Limit Reached",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Stop);

                                // Abort the entire sale — do not finalize
                                return null;
                            }

                            customer.AccountBalance += accountTotal;
                        }
                    }
                }

                db.SaveChanges();

                // ── Build split summary ───────────────────────────────────
                var splitSummary = Splits.Count == 1
                    ? Splits[0].Method
                    : string.Join(" + ", Splits.Select(s => $"{s.Method} {s.Amount:N2}"));

                // ── Persist sale ──────────────────────────────────────────
                var prefix = StoreSettingsService.Get("ReceiptPrefix", "REC");
                var nextNo = int.TryParse(StoreSettingsService.Get("NextReceiptNumber", "1"), out var n) ? n : 1;
                var receiptNo = $"{prefix}-{nextNo:D4}";
                StoreSettingsService.Set("NextReceiptNumber", (nextNo + 1).ToString());

                bool hasReturns = Cart.Any(l => l.IsReturn);
                bool hasSales   = Cart.Any(l => !l.IsReturn);
                string saleStatus = hasReturns && !hasSales ? "Return"
                                  : hasReturns              ? "Sale/Return"
                                                            : "Completed";

                var sale = new Sale
                {
                    SaleDate = DateTime.Now,
                    ReceiptNumber = receiptNo,
                    Cashier = cashierName,
                    CustomerName = CustomerSearchText ?? "",
                    Subtotal = Subtotal,
                    Tax = Tax,
                    Total = Total,
                    PaymentMethod = splitSummary,
                    Status = saleStatus,
                    Items = Cart.Select(l => new SaleItem
                    {
                        ProductId    = l.ProductId,
                        SKU          = l.SKU ?? "",
                        ProductName  = l.Name ?? "",
                        Quantity     = l.Qty,
                        UnitPrice    = l.UnitPrice,
                        LineTotal    = l.LineTotal,
                        DiscountPct  = l.DiscountPct,
                        ReturnReason = l.ReturnReason ?? "",
                    }).ToList(),
                };
                db.Sales.Add(sale);
                db.SaveChanges();

                // ── Build receipt data ────────────────────────────────────
                var receiptData = new ReceiptData
                {
                    ReceiptNumber = sale.Id.ToString(),
                    SaleDate = sale.SaleDate,
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
                var summary = string.Join(", ", Cart.Select(l => $"{l.Name} x{l.Qty}"));

                var held = new PointofSale.Models.HeldReceipt
                {
                    HeldAt = DateTime.Now,
                    Cashier = cashierName,
                    PaymentMethod = "",
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