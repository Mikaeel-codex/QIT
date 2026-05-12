using PointofSale.Data;
using PointofSale.Models;
using PointofSale.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PointofSale.Views
{
    public partial class SalesOrderWindow : Window
    {
        private readonly AppUser _currentUser;
        private int? _quoteId;

        private readonly ObservableCollection<QuoteLineItem> _lines = new();
        private string _pendingQuoteNumber = "";
        private int    _pendingQuoteSeq;
        private string _customerPhone = "";
        private string _customerEmail = "";
        private int?   _selectedCustomerId;

        public SalesOrderWindow(AppUser user, int? existingQuoteId = null)
        {
            InitializeComponent();
            _currentUser = user;
            _quoteId = existingQuoteId;

            UserLabelTxt.Text = $"{_currentUser.Role}: {_currentUser.Username}";
            CashierNameTxt.Text = _currentUser.Username;
            OrderDateTxt.Text = DateTime.Now.ToString("MM/dd/yyyy");

            QuoteGrid.ItemsSource = _lines;
            _lines.CollectionChanged += (_, _) => RefreshTotals();

            ExpiryDatePicker.SelectedDate = DateTime.Today.AddDays(
                int.TryParse(StoreSettingsService.Get("QuoteExpiryDays", "30"), out var d) ? d : 30);

            if (_quoteId != null)
                LoadExistingQuote();
            else
                InitNewQuote();

            RefreshActionButtons();
        }

        // ── Init ──────────────────────────────────────────────────────────
        private void InitNewQuote()
        {
            var prefix = StoreSettingsService.Get("QuotePrefix", "SO");
            _pendingQuoteSeq = int.TryParse(StoreSettingsService.Get("NextQuoteNumber", "1"), out var n) ? n : 1;
            _pendingQuoteNumber = $"{prefix}-{_pendingQuoteSeq:D4}";
            QuoteNumberTxt.Text = _pendingQuoteNumber;
            SetStatus("Open");
        }

        private void LoadExistingQuote()
        {
            using var db = new AppDbContext();
            var q = db.Quotes.FirstOrDefault(x => x.Id == _quoteId!.Value);
            if (q == null) { MessageBox.Show("Quote not found."); Close(); return; }

            QuoteNumberTxt.Text  = q.QuoteNumber;
            CustomerNameBox.Text = q.CustomerName;
            _customerPhone       = q.CustomerPhone;
            _customerEmail       = q.CustomerEmail;
            NotesBox.Text        = q.Notes;
            ExpiryDatePicker.SelectedDate = q.ExpiresAt;

            var items = db.QuoteItems.Where(i => i.QuoteId == q.Id).ToList();
            int row = 1;
            foreach (var item in items)
            {
                _lines.Add(new QuoteLineItem
                {
                    RowNumber = row++,
                    ProductId = item.ProductId,
                    SKU = item.SKU,
                    Name = item.Name,
                    Size = item.Size,
                    Attribute = item.Attribute,
                    Qty = item.Qty,
                    UnitPrice = item.UnitPrice,
                    LineTotal = item.LineTotal,
                    TaxCode = item.TaxCode,
                    DiscountPct = item.DiscountPct,
                });
            }
            SetStatus(q.Status);

            if (!string.IsNullOrWhiteSpace(q.CustomerName))
            {
                var cust = db.Customers.FirstOrDefault(c =>
                    (c.FirstName + " " + c.LastName).Trim() == q.CustomerName);
                if (cust != null)
                    ShowCustomerDetails(cust);
            }
        }

        // ── Customer search ───────────────────────────────────────────────
        private void CustomerDropdown_Click(object sender, RoutedEventArgs e)
            => OpenCustomerPopup();

        private void FindCustomer_Click(object sender, RoutedEventArgs e)
            => OpenCustomerPopup();

        private void CustomerNameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                OpenCustomerPopup();
            else if (e.Key == Key.Down && CustomerPopup.IsOpen)
            {
                CustomerList.Focus();
                if (CustomerList.Items.Count > 0) CustomerList.SelectedIndex = 0;
                e.Handled = true;
            }
        }

        private void OpenCustomerPopup()
        {
            using var db = new AppDbContext();
            var all   = db.Customers.Where(c => c.IsActive).OrderBy(c => c.FirstName).ToList();
            var query = CustomerNameBox.Text.Trim().ToLower();

            var filtered = string.IsNullOrWhiteSpace(query)
                ? all
                : all.Where(c =>
                    c.FullName.ToLower().Contains(query) ||
                    c.Phone.Contains(query)).ToList();

            if (filtered.Count == 0)
            {
                CustomerPopup.IsOpen = false;
                MessageBox.Show("No matching customers found.", "Customer Search");
                return;
            }

            CustomerList.ItemsSource = filtered;
            CustomerPopup.IsOpen     = true;
        }

        private void CustomerList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CustomerList.SelectedItem is Models.Customer c)
                SelectCustomer(c);
        }

        private void CustomerList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && CustomerList.SelectedItem is Models.Customer c)
            {
                SelectCustomer(c);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CustomerPopup.IsOpen = false;
                CustomerNameBox.Focus();
            }
        }

        private void SelectCustomer(Models.Customer c)
        {
            _selectedCustomerId  = c.Id;
            _customerPhone       = c.Phone;
            _customerEmail       = c.Email;
            CustomerNameBox.Text = c.FullName;
            CustomerPopup.IsOpen = false;
            ShowCustomerDetails(c);
        }

        private void ShowCustomerDetails(Models.Customer c)
        {
            var parts = new[] { c.Address, c.City, c.Province, c.PostalCode }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            CustAddressTxt.Text = string.Join(", ", parts);
            if (string.IsNullOrWhiteSpace(CustAddressTxt.Text))
                CustAddressTxt.Text = "—";
            CustPhoneTxt.Text   = c.Phone;
            CustBalanceTxt.Text = $"R {c.AccountBalance:N2}";
            CustCreditTxt.Text  = $"R {c.AvailableCredit:N2}";
            CustomerDetailPanel.Visibility = Visibility.Visible;
        }

        // ── Product scan ──────────────────────────────────────────────────
        private void ScanBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = ScanBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query)) { ProductPopup.IsOpen = false; return; }

            using var db = new AppDbContext();
            var results = db.Products
                .Where(p => p.Name.Contains(query) || p.SKU.Contains(query) || p.ALU.Contains(query))
                .OrderBy(p => p.Name).Take(20).ToList();

            ProductList.ItemsSource = results;
            ProductPopup.IsOpen = results.Count > 0;
        }

        private void ScanBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var sku = ScanBox.Text.Trim();
                using var db = new AppDbContext();
                var p = db.Products.FirstOrDefault(x => x.SKU == sku || x.ALU == sku);
                if (p != null) { AddProduct(p); ScanBox.Clear(); }
                else if (ProductList.Items.Count > 0) ProductList.Focus();
            }
            else if (e.Key == Key.Down && ProductPopup.IsOpen)
            {
                ProductList.Focus();
                if (ProductList.Items.Count > 0) ProductList.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
                ProductPopup.IsOpen = false;
        }

        private void ScanDropdown_Click(object sender, RoutedEventArgs e)
        {
            using var db = new AppDbContext();
            ProductList.ItemsSource = db.Products.OrderBy(p => p.Name).Take(50).ToList();
            ProductPopup.IsOpen = true;
            ScanBox.Focus();
        }

        private void ProductList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProductList.SelectedItem is Product p)
            {
                AddProduct(p);
                ScanBox.Clear();
                ProductPopup.IsOpen = false;
            }
        }

        private void ProductList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ProductList.SelectedItem is Product p)
            {
                AddProduct(p);
                ScanBox.Clear();
                ProductPopup.IsOpen = false;
                ScanBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ProductPopup.IsOpen = false;
                ScanBox.Focus();
            }
        }

        private void AddProduct(Product p)
        {
            var existing = _lines.FirstOrDefault(l => l.ProductId == p.Id);
            if (existing != null)
            {
                existing.Qty++;
                existing.LineTotal = existing.Qty * existing.UnitPrice;
            }
            else
            {
                _lines.Add(new QuoteLineItem
                {
                    RowNumber = _lines.Count + 1,
                    ProductId = p.Id,
                    SKU = p.SKU ?? "",
                    Name = p.Name,
                    Size = p.Size ?? "",
                    Attribute = p.Attribute ?? "",
                    StockQty = p.StockQty,
                    Qty = 1,
                    UnitPrice = p.Price,
                    LineTotal = p.Price,
                    TaxCode = p.Tax ?? "No Tax",
                    DiscountPct = 0,
                });
            }
            RefreshTotals();
        }

        // ── Row selection ─────────────────────────────────────────────────
        private void QuoteGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ItemToolbar.Visibility = QuoteGrid.SelectedItem != null
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ── Row actions ───────────────────────────────────────────────────
        private void RemoveLine_Click(object sender, RoutedEventArgs e)
        {
            if (QuoteGrid.SelectedItem is QuoteLineItem line)
            {
                _lines.Remove(line);
                // Renumber
                for (int i = 0; i < _lines.Count; i++)
                    _lines[i].RowNumber = i + 1;
            }
        }

        private void EditLine_Click(object sender, RoutedEventArgs e)
        {
            if (QuoteGrid.SelectedItem is not QuoteLineItem line) return;

            // QtyPriceDiscountWindow modifies the CartLine directly in-place
            var tempLine = new CartLine
            {
                Name = line.Name,
                Qty = line.Qty,
                UnitPrice = line.UnitPrice,
                DiscountPct = line.DiscountPct,
                OriginalPrice = line.UnitPrice,
            };

            var win = new QtyPriceDiscountWindow(tempLine) { Owner = this };
            if (win.ShowDialog() == true)
            {
                // Read back the values the window wrote into tempLine
                line.Qty = tempLine.Qty;
                line.UnitPrice = tempLine.UnitPrice;
                line.DiscountPct = tempLine.DiscountPct;
                line.LineTotal = Math.Round(tempLine.UnitPrice * tempLine.Qty, 2);
                RefreshTotals();
            }
        }

        private void QtyPlus_Click(object sender, RoutedEventArgs e)
        {
            if (QuoteGrid.SelectedItem is not QuoteLineItem line) return;
            line.Qty++;
            line.LineTotal = Math.Round(line.UnitPrice * (1 - line.DiscountPct / 100m) * line.Qty, 2);
            RefreshTotals();
        }

        private void QtyMinus_Click(object sender, RoutedEventArgs e)
        {
            if (QuoteGrid.SelectedItem is not QuoteLineItem line) return;
            if (line.Qty > 1) line.Qty--;
            line.LineTotal = Math.Round(line.UnitPrice * (1 - line.DiscountPct / 100m) * line.Qty, 2);
            RefreshTotals();
        }

        // ── Totals ────────────────────────────────────────────────────────
        private void RefreshTotals()
        {
            foreach (var l in _lines)
            {
                var discounted = l.UnitPrice * (1 - l.DiscountPct / 100m);
                l.LineTotal = Math.Round(discounted * l.Qty, 2);
            }

            var sub = _lines.Sum(l => l.LineTotal);
            var tax = Math.Round(_lines.Sum(l =>
                l.TaxCode.Contains("15") ? l.LineTotal * 0.15m : 0m), 2);
            var total = sub + tax;

            SubtotalTxt.Text = $"R {sub:N2}";
            TaxTxt.Text = $"R {tax:N2}";
            TotalTxt.Text = $"R {total:N2}";
            BalanceDueTxt.Text = $"R {total:N2}";
            DiscountTxt.Text = "R 0.00";
        }

        // ── Status helpers ────────────────────────────────────────────────
        private void SetStatus(string status)
        {
            var display = status switch
            {
                "Draft"                                              => "Open",
                "Approved" or "Accepted" or "Converted"
                           or "Rejected" or "Expired"               => "Closed",
                _                                                    => status
            };
            foreach (ComboBoxItem item in StatusCombo.Items)
            {
                if ((string)item.Content == display)
                {
                    StatusCombo.SelectedItem = item;
                    return;
                }
            }
            StatusCombo.SelectedIndex = 0;
        }

        private void RefreshActionButtons()
        {
            var isAdmin = _currentUser.Role == "Admin" || _currentUser.Role == "Co-Admin";
            var status = StatusCombo.SelectedItem is ComboBoxItem ci ? (string)ci.Content : "Open";

            SubmitBtn.Visibility  = status == "Open"    ? Visibility.Visible : Visibility.Collapsed;
            ApproveBtn.Visibility = isAdmin && status == "Pending" ? Visibility.Visible : Visibility.Collapsed;
            RejectBtn.Visibility  = isAdmin && status == "Pending" ? Visibility.Visible : Visibility.Collapsed;
            ConvertBtn.Visibility = Visibility.Collapsed;
            PrintBtn.Visibility   = status == "Pending" || status == "Closed" ? Visibility.Visible : Visibility.Collapsed;
            SendBtn.Visibility    = status == "Pending" || status == "Closed" ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Action buttons ────────────────────────────────────────────────
        private void SaveDraft_Click(object sender, RoutedEventArgs e)
        {
            var id = SaveQuoteToDb("Open");
            if (id > 0)
            {
                _quoteId = id;
                MessageBox.Show("Quote saved as draft.", "Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SubmitApproval_Click(object sender, RoutedEventArgs e)
        {
            if (_lines.Count == 0)
            { MessageBox.Show("Add at least one item before submitting."); return; }

            var id = SaveQuoteToDb("Pending");
            if (id > 0)
            {
                _quoteId = id;
                SetStatus("Pending");
                RefreshActionButtons();
                MessageBox.Show("Quote submitted for admin approval.",
                    "Submitted", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ApproveBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateQuoteStatus("Closed");
            SetStatus("Closed");
            RefreshActionButtons();
            MessageBox.Show("Quote approved. Print and send are now available.",
                "Approved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RejectBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateQuoteStatus("Closed");
            SetStatus("Closed");
            RefreshActionButtons();
        }

        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_quoteId == null) { MessageBox.Show("Save the quote first."); return; }
            LogPrintAction("Print");
            QuotePrinter.PrintQuote(BuildQuoteData(), this);
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_quoteId == null) { MessageBox.Show("Save the quote first."); return; }
            LogPrintAction("Send");
            new SendQuoteWindow(BuildQuoteData()) { Owner = this }.ShowDialog();
        }

        private void ConvertBtn_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Mark this quote as Accepted?\n\nThe cashier can process payment at POS.",
                "Convert Quote", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            UpdateQuoteStatus("Accepted");
            SetStatus("Accepted");
            RefreshActionButtons();
        }

        private void CancelQuote_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Discard this quote?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                Close();
        }

        private void HomeBtn_Click(object sender, RoutedEventArgs e) => Close();

        // ── DB helpers ────────────────────────────────────────────────────
        private int SaveQuoteToDb(string status)
        {
            try
            {
                using var db = new AppDbContext();

                Quote q;
                if (_quoteId == null)
                {
                    q = new Quote { QuoteNumber = _pendingQuoteNumber, CreatedBy = _currentUser.Username };
                    StoreSettingsService.Set("NextQuoteNumber", (_pendingQuoteSeq + 1).ToString());
                    db.Quotes.Add(q);
                }
                else
                {
                    q = db.Quotes.First(x => x.Id == _quoteId.Value);
                    db.QuoteItems.RemoveRange(db.QuoteItems.Where(i => i.QuoteId == q.Id));
                }

                q.Status = status;
                q.CustomerName  = CustomerNameBox.Text.Trim();
                q.CustomerPhone = _customerPhone;
                q.CustomerEmail = _customerEmail;
                q.Notes = NotesBox.Text.Trim();
                q.ExpiresAt = ExpiryDatePicker.SelectedDate ?? DateTime.Today.AddDays(30);

                var sub = _lines.Sum(l => l.LineTotal);
                var tax = Math.Round(_lines.Sum(l => l.TaxCode.Contains("15") ? l.LineTotal * 0.15m : 0m), 2);
                q.Subtotal = sub;
                q.Tax = tax;
                q.Total = sub + tax;

                foreach (var line in _lines)
                {
                    db.QuoteItems.Add(new QuoteItem
                    {
                        QuoteId = q.Id,
                        ProductId = line.ProductId,
                        SKU = line.SKU,
                        Name = line.Name,
                        Size = line.Size,
                        Attribute = line.Attribute,
                        Qty = line.Qty,
                        UnitPrice = line.UnitPrice,
                        LineTotal = line.LineTotal,
                        TaxCode = line.TaxCode,
                        DiscountPct = line.DiscountPct,
                    });
                }

                db.SaveChanges();
                QuoteNumberTxt.Text = q.QuoteNumber;
                return q.Id;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving quote: {ex.Message}");
                return -1;
            }
        }

        private void UpdateQuoteStatus(string status)
        {
            if (_quoteId == null) return;
            using var db = new AppDbContext();
            var q = db.Quotes.First(x => x.Id == _quoteId.Value);
            q.Status = status;
            if (status == "Closed") { q.ApprovedBy = _currentUser.Username; q.ApprovedAt = DateTime.Now; }
            db.SaveChanges();
        }

        private void LogPrintAction(string action)
        {
            if (_quoteId == null) return;
            using var db = new AppDbContext();
            var q = db.Quotes.First(x => x.Id == _quoteId.Value);
            var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm}|{_currentUser.Username}|{action}";
            q.PrintLog = string.IsNullOrWhiteSpace(q.PrintLog) ? entry : q.PrintLog + "|" + entry;
            db.SaveChanges();
        }

        private QuoteData BuildQuoteData() => new()
        {
            QuoteNumber = QuoteNumberTxt.Text,
            CreatedAt = DateTime.Now,
            ExpiresAt = ExpiryDatePicker.SelectedDate ?? DateTime.Today.AddDays(30),
            CustomerName  = CustomerNameBox.Text.Trim(),
            CustomerPhone = _customerPhone,
            CustomerEmail = _customerEmail,
            Notes = NotesBox.Text.Trim(),
            CreatedBy = _currentUser.Username,
            StoreName = StoreSettingsService.Get("StoreName", "My Store"),
            StoreAddress = StoreSettingsService.Get("StoreAddress", ""),
            StorePhone = StoreSettingsService.Get("StorePhone", ""),
            Subtotal = _lines.Sum(l => l.LineTotal),
            Tax = Math.Round(_lines.Sum(l => l.TaxCode.Contains("15") ? l.LineTotal * 0.15m : 0m), 2),
            Total = _lines.Sum(l => l.LineTotal) + Math.Round(_lines.Sum(l => l.TaxCode.Contains("15") ? l.LineTotal * 0.15m : 0m), 2),
            Lines = _lines.Select(l => new QuoteLineData
            {
                Name = l.Name,
                SKU = l.SKU,
                Size = l.Size,
                Attribute = l.Attribute,
                Qty = l.Qty,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                DiscountPct = l.DiscountPct,
                TaxCode = l.TaxCode,
            }).ToList(),
        };
    }

    // ── Line item view model ──────────────────────────────────────────────
    public class QuoteLineItem : System.ComponentModel.INotifyPropertyChanged
    {
        public int RowNumber { get; set; }
        public int ProductId { get; set; }
        public string SKU { get; set; } = "";
        public string Name { get; set; } = "";
        public string Size { get; set; } = "";
        public string Attribute { get; set; } = "";
        public string TaxCode { get; set; } = "";
        public int StockQty { get; set; }

        private int _qty;
        public int Qty
        {
            get => _qty;
            set { _qty = value; OnPC(nameof(Qty)); }
        }

        private decimal _unitPrice;
        public decimal UnitPrice
        {
            get => _unitPrice;
            set { _unitPrice = value; OnPC(nameof(UnitPrice)); }
        }

        private decimal _discountPct;
        public decimal DiscountPct
        {
            get => _discountPct;
            set { _discountPct = value; OnPC(nameof(DiscountPct)); }
        }

        private decimal _lineTotal;
        public decimal LineTotal
        {
            get => _lineTotal;
            set { _lineTotal = value; OnPC(nameof(LineTotal)); }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void OnPC(string n)
            => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(n));
    }
}