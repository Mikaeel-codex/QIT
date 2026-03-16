using PointofSale.Data;
using PointofSale.Models;
using PointofSale.Services;
using PointofSale.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PointofSale.Views
{
    public partial class PointOfSaleWindow : Window
    {
        private readonly AppUser _currentUser;
        private bool _suppressScanTextChanged;
        private bool _suppressCustomerTextChanged;

        public PointOfSaleWindow(AppUser user)
        {
            InitializeComponent();
            _currentUser = user;
            UserLabelTop.Text = $"{_currentUser.Role}: {_currentUser.Username}";
            var vm = new PosViewModel();
            DataContext = vm;

            // Update Hold button label whenever cart changes
            vm.Cart.CollectionChanged += (_, __) => RefreshHoldButton();
            RefreshHoldButton();
        }

        // ═══════════════════════════════════════
        // STARTUP
        // ═══════════════════════════════════════

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ScanBoxTop.Focus();
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Don't steal focus if the user is editing a DataGrid cell,
            // typing in any TextBox/ComboBox, or the product popup is open
            if (ProductPopup.IsOpen) return;
            if (ScanBoxTop.IsKeyboardFocusWithin) return;

            // Check if focus is inside the ReceiptGrid (cell editing)
            var focused = System.Windows.Input.Keyboard.FocusedElement as System.Windows.DependencyObject;
            if (focused != null && IsDescendantOf(focused, ReceiptGrid)) return;

            // Also skip if a TextBox or ComboBox anywhere else has focus
            if (focused is System.Windows.Controls.TextBox) return;
            if (focused is System.Windows.Controls.ComboBox) return;

            ScanBoxTop.Focus();
        }

        private static bool IsDescendantOf(System.Windows.DependencyObject child, System.Windows.DependencyObject parent)
        {
            var current = child;
            while (current != null)
            {
                if (current == parent) return true;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current)
                       ?? System.Windows.LogicalTreeHelper.GetParent(current);
            }
            return false;
        }

        // ═══════════════════════════════════════
        // PRODUCT SEARCH
        // ═══════════════════════════════════════

        private void ScanBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressScanTextChanged) return;
            var query = ScanBoxTop.Text.Trim();
            if (query.Length < 1) { ProductPopup.IsOpen = false; return; }

            using var db = new AppDbContext();
            var results = db.Products
                .Where(p => p.Name.Contains(query) ||
                            (p.SKU != null && p.SKU.Contains(query)) ||
                            (p.ALU != null && p.ALU.Contains(query)))
                .OrderBy(p => p.Name)
                .Take(20)
                .ToList();

            ProductList.ItemsSource = results;
            ProductPopup.IsOpen = results.Count > 0;
        }

        private void ScanBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var sku = ScanBoxTop.Text.Trim();
                ClearScanBox();
                if (DataContext is PosViewModel vm)
                    vm.ScanSkuAndAddToCart(sku);
                e.Handled = true;
            }
            else if (e.Key == Key.Down && ProductPopup.IsOpen)
            {
                ProductList.Focus();
                if (ProductList.Items.Count > 0)
                    ProductList.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ProductPopup.IsOpen = false;
            }
        }

        private void ScanBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!ProductList.IsKeyboardFocusWithin)
                ProductPopup.IsOpen = false;
        }

        private void ScanDropdownArrow_Click(object sender, RoutedEventArgs e)
        {
            var query = ScanBoxTop.Text.Trim();
            using var db = new AppDbContext();
            var results = db.Products
                .OrderBy(p => p.Name)
                .Take(30)
                .ToList();

            if (!string.IsNullOrEmpty(query))
                results = db.Products
                    .Where(p => p.Name.Contains(query) ||
                                (p.SKU != null && p.SKU.Contains(query)) ||
                                (p.ALU != null && p.ALU.Contains(query)))
                    .OrderBy(p => p.Name)
                    .Take(30)
                    .ToList();

            ProductList.ItemsSource = results;
            ProductPopup.IsOpen = results.Count > 0;
            ScanBoxTop.Focus();
        }

        private void ProductList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProductList.SelectedItem is Product p)
                AddProductToCart(p);
        }

        private void ProductList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ProductList.SelectedItem is Product p)
            {
                AddProductToCart(p);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ProductPopup.IsOpen = false;
                ScanBoxTop.Focus();
            }
        }

        private void AddProductToCart(Product p)
        {
            ProductPopup.IsOpen = false;
            ClearScanBox();
            if (DataContext is PosViewModel vm)
            {
                var identifier = !string.IsNullOrWhiteSpace(p.SKU) ? p.SKU : p.ALU;
                vm.ScanSkuAndAddToCart(identifier ?? "");
            }
            ScanBoxTop.Focus();
        }

        private void ClearScanBox()
        {
            _suppressScanTextChanged = true;
            ScanBoxTop.Clear();
            _suppressScanTextChanged = false;
        }

        // ═══════════════════════════════════════
        // CUSTOMER SEARCH (stubbed)
        // ═══════════════════════════════════════

        private void CustomerDropdownArrow_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Customers screen coming next.");
        }

        private void CustomerBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CustomerPopup.IsOpen = false;
        }

        private void CustomerBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                CustomerPopup.IsOpen = false;
        }

        private void CustomerBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CustomerPopup.IsOpen = false;
        }

        private void CustomerList_MouseDoubleClick(object sender, MouseButtonEventArgs e) { }

        private void CustomerList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CustomerPopup.IsOpen = false;
                CustomerBoxTop.Focus();
            }
        }

        // ═══════════════════════════════════════
        // ROW ACTIONS
        // ═══════════════════════════════════════

        private CartLine? SelectedLine => ReceiptGrid.SelectedItem as CartLine;

        private void EditLine_Click(object sender, RoutedEventArgs e) => OpenQtyPriceDiscount();
        private void QtyPriceDiscount_Click(object sender, RoutedEventArgs e) => OpenQtyPriceDiscount();

        private void ReturnLine_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (SelectedLine == null) return;
            SelectedLine.Qty = -System.Math.Abs(SelectedLine.Qty);
            vm.RefreshTotals();
        }

        private void OpenQtyPriceDiscount()
        {
            if (DataContext is not PosViewModel vm) return;
            if (SelectedLine == null) return;
            var win = new QtyPriceDiscountWindow(SelectedLine) { Owner = this };
            if (win.ShowDialog() == true)
                vm.RefreshTotals();
        }

        // ═══════════════════════════════════════
        // PAYMENTS
        // Each method adds a split. Multiple splits = split payment.
        // Amount defaults to remaining balance so partial splits work naturally.
        // ═══════════════════════════════════════

        // ── Shared helper: ask "how much for this method?" ───────────────
        // Shows total, already paid, still owing. Pre-fills with owing.
        // Returns entered amount or null if cancelled.
        private decimal? AskPaymentAmount(string method, PosViewModel vm)
        {
            var owing = vm.AmountDue > 0 ? vm.AmountDue : vm.Total;

            var win = new Window
            {
                Title = $"{method} Payment",
                Width = 380,
                Height = 310,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x11, 0x11, 0x11))
            };

            var outer = new System.Windows.Controls.StackPanel
            { Margin = new Thickness(24, 20, 24, 16) };

            // ── Summary rows ──────────────────────────────────────────────
            void AddRow(string label, string value, bool bold = false, string hexColor = "#FFFFFF")
            {
                var row = new System.Windows.Controls.Grid();
                row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());
                row.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition
                { Width = new GridLength(110) });
                var lbl = new System.Windows.Controls.TextBlock
                {
                    Text = label,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#BBBBBB")),
                    FontSize = 13
                };
                var val = new System.Windows.Controls.TextBlock
                {
                    Text = value,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor)),
                    FontSize = 13,
                    FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
                };
                System.Windows.Controls.Grid.SetColumn(val, 1);
                row.Children.Add(lbl);
                row.Children.Add(val);
                row.Margin = new Thickness(0, 2, 0, 2);
                outer.Children.Add(row);
            }

            AddRow("Sale Total", $"{vm.Total:N2}");
            if (vm.TotalTendered > 0)
                AddRow("Already Paid", $"{vm.TotalTendered:N2}", hexColor: "#88CC66");
            AddRow("Still Owing", $"{owing:N2}", bold: true, hexColor: "#FF8C00");

            // ── Separator ─────────────────────────────────────────────────
            outer.Children.Add(new System.Windows.Controls.Separator
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33)),
                Margin = new Thickness(0, 12, 0, 12)
            });

            // ── Amount entry ──────────────────────────────────────────────
            outer.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = $"Enter {method} amount:",
                Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var amtBox = new System.Windows.Controls.TextBox
            {
                Text = owing.ToString("N2"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x1A)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2F, 0x66, 0xC8)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 6, 8, 6),
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 16)
            };
            amtBox.SelectAll();
            outer.Children.Add(amtBox);

            // ── Buttons ───────────────────────────────────────────────────
            var btnRow = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var btnOk = new System.Windows.Controls.Button
            {
                Content = "Confirm",
                Width = 100,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2F, 0x66, 0xC8)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            var btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 32,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };

            decimal? result = null;
            btnOk.Click += (s, ev) =>
            {
                if (decimal.TryParse(amtBox.Text.Replace(",", ""), out var amt) && amt > 0)
                {
                    result = amt;
                    win.DialogResult = true;
                }
                else
                    System.Windows.MessageBox.Show("Please enter a valid amount.", "Invalid Amount",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
            };
            btnCancel.Click += (s, ev) => win.Close();

            // Confirm on Enter key
            amtBox.KeyDown += (s, ev) => { if (ev.Key == Key.Enter) btnOk.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent)); };

            btnRow.Children.Add(btnOk);
            btnRow.Children.Add(btnCancel);
            outer.Children.Add(btnRow);
            win.Content = outer;
            win.Loaded += (s, ev) => amtBox.Focus();
            win.ShowDialog();
            return result;
        }

        // ── Payment handlers ─────────────────────────────────────────────

        private void Cash_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (vm.Cart.Count == 0) { MessageBox.Show("Add items first."); return; }

            // Cash uses CashPaymentWindow (has quick R5/R10/R20 buttons)
            var owing = vm.AmountDue > 0 ? vm.AmountDue : vm.Total;
            var cashWin = new CashPaymentWindow(owing, saleTotal: vm.Total, alreadyPaid: vm.TotalTendered) { Owner = this };
            if (cashWin.ShowDialog() != true) return;

            var tendered = cashWin.AmountTendered;
            if (tendered <= 0) { MessageBox.Show("Amount tendered must be greater than zero."); return; }

            vm.AddSplit("Cash", tendered, label: "Cash");
            vm.RefreshTotals();
            UpdatePaymentCard(vm, BtnCash);
        }

        private void Credit_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (vm.Cart.Count == 0) { MessageBox.Show("Add items first."); return; }

            var amount = AskPaymentAmount("Credit", vm);
            if (amount == null) return;

            vm.AddSplit("Credit", amount.Value, label: "Credit");
            vm.RefreshTotals();
            UpdatePaymentCard(vm, BtnCredit);
        }

        private void Eft_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (vm.Cart.Count == 0) { MessageBox.Show("Add items first."); return; }

            // Step 1 — ask amount
            var amount = AskPaymentAmount("EFT", vm);
            if (amount == null) return;

            // Step 2 — instant or pending?
            var win = new Window
            {
                Title = "EFT — Confirm Receipt",
                Width = 380,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x11, 0x11, 0x11))
            };
            var outer = new System.Windows.Controls.StackPanel { Margin = new Thickness(24, 20, 24, 16) };
            outer.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = $"EFT Amount:  {amount.Value:N2}",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 18)
            });
            outer.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = "Has this EFT been received instantly?",
                Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            });
            var rbInstant = new System.Windows.Controls.RadioButton { Content = "Yes — Instant (confirmed)", Foreground = System.Windows.Media.Brushes.White, FontSize = 13, IsChecked = true, Margin = new Thickness(0, 0, 0, 6), GroupName = "EftType2" };
            var rbPending = new System.Windows.Controls.RadioButton { Content = "No — Pending (awaiting confirmation)", Foreground = System.Windows.Media.Brushes.LightGray, FontSize = 13, GroupName = "EftType2" };
            outer.Children.Add(rbInstant);
            outer.Children.Add(rbPending);
            var btnRow = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            var btnOk = new System.Windows.Controls.Button { Content = "Confirm", Width = 100, Height = 32, Margin = new Thickness(0, 0, 10, 0), Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2F, 0x66, 0xC8)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0) };
            var btnCancel = new System.Windows.Controls.Button { Content = "Cancel", Width = 80, Height = 32, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x33, 0x33, 0x33)), Foreground = System.Windows.Media.Brushes.White, BorderThickness = new Thickness(0) };
            bool confirmed = false;
            btnOk.Click += (s, ev) => { confirmed = true; win.Close(); };
            btnCancel.Click += (s, ev) => win.Close();
            btnRow.Children.Add(btnOk); btnRow.Children.Add(btnCancel);
            outer.Children.Add(btnRow);
            win.Content = outer;
            win.ShowDialog();

            if (!confirmed) return;

            if (rbInstant.IsChecked == true)
            {
                vm.AddSplit("EFT", amount.Value, label: "EFT");
                vm.RefreshTotals();
                UpdatePaymentCard(vm, BtnEft);
            }
            else
            {
                bool held = vm.PutOnHoldWithNote(_currentUser.Username, "Pending EFT", "PendingEFT");
                if (held)
                {
                    ResetPaymentCard();
                    RefreshHoldButton();
                    MessageBox.Show($"Sale moved to Held Receipts.\n\nStatus: Pending EFT\nAmount: {amount.Value:N2}\n\nComplete it once EFT is confirmed.",
                        "Pending EFT", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Gift_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;

            var owing = vm.AmountDue > 0 ? vm.AmountDue : vm.Total;
            var giftWin = new GiftCardWindow(owing, _currentUser.Username) { Owner = this };
            if (giftWin.ShowDialog() != true) return;

            if (giftWin.IsRedeem && vm.Cart.Count > 0)
            {
                vm.AddSplit("Gift Card", giftWin.AmountRedeemed, giftCardId: giftWin.CardId,
                            label: $"Gift Card #{giftWin.CardNumber}");
                vm.RefreshTotals();
                UpdatePaymentCard(vm, BtnGift);
            }
        }

        private void Account_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (vm.Cart.Count == 0) { MessageBox.Show("Add items first."); return; }

            var amount = AskPaymentAmount("Account", vm);
            if (amount == null) return;

            vm.AddSplit("Account", amount.Value, label: "Account");
            vm.RefreshTotals();
            UpdatePaymentCard(vm, BtnAccount);
        }

        // ── Payment card helpers ─────────────────────────────────────────

        // Builds the payment card from all current splits
        private void UpdatePaymentCard(PosViewModel vm, Button activeBtn)
        {
            // Highlight active button
            if (_activePayBtn != null) _activePayBtn.Background = _defaultPayBrush;
            _activePayBtn = activeBtn;
            activeBtn.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x2E, 0x7D, 0x32));

            PaymentCardBorder.Visibility = Visibility.Visible;
            PaymentCardBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x2E, 0x7D, 0x32));
            PaymentCardBorder.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x1B, 0x5E, 0x20));

            if (vm.Splits.Count == 1)
            {
                var s = vm.Splits[0];
                PaymentStatusTxt.Text = $"✔  {s.Method}";
                PaymentStatusTxt.Foreground = System.Windows.Media.Brushes.White;
                PaymentStatusTxt.FontStyle = FontStyles.Normal;
                PaymentAmountTxt.Text = $"{s.Amount:C2}";
                PaymentAmountTxt.Visibility = Visibility.Visible;
                PaymentChangeTxt.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Split payment — show each line
                var lines = string.Join("   ", vm.Splits.Select(s => $"{s.Method}: {s.Amount:C2}"));
                PaymentStatusTxt.Text = $"✔  Split Payment";
                PaymentStatusTxt.Foreground = System.Windows.Media.Brushes.White;
                PaymentStatusTxt.FontStyle = FontStyles.Normal;
                PaymentAmountTxt.Text = lines;
                PaymentAmountTxt.Visibility = Visibility.Visible;
                PaymentChangeTxt.Visibility = Visibility.Collapsed;
            }

            if (vm.AmountDue > 0)
            {
                PaymentChangeTxt.Text = $"Still owing: {vm.AmountDue:C2}";
                PaymentChangeTxt.Foreground = System.Windows.Media.Brushes.OrangeRed;
                PaymentChangeTxt.Visibility = Visibility.Visible;
            }
            else if (vm.CashChange > 0)
            {
                PaymentChangeTxt.Text = $"Change: {vm.CashChange:C2}";
                PaymentChangeTxt.Foreground = System.Windows.Media.Brushes.LightGreen;
                PaymentChangeTxt.Visibility = Visibility.Visible;
            }
        }

        private void RemovePayment_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (!vm.HasSplits) { MessageBox.Show("No payment to remove."); return; }
            vm.RemovePaymentMethod();
            ResetPaymentCard();
        }

        private Button? _activePayBtn = null;
        private readonly System.Windows.Media.SolidColorBrush _defaultPayBrush =
            new(System.Windows.Media.Color.FromRgb(47, 102, 200));

        private void ResetPaymentCard()
        {
            if (_activePayBtn != null)
            {
                _activePayBtn.Background = _defaultPayBrush;
                _activePayBtn = null;
            }
            PaymentStatusTxt.Text = "No payment selected";
            PaymentStatusTxt.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(136, 136, 136));
            PaymentStatusTxt.FontStyle = System.Windows.FontStyles.Italic;
            PaymentAmountTxt.Visibility = System.Windows.Visibility.Collapsed;
            PaymentChangeTxt.Visibility = System.Windows.Visibility.Collapsed;
            PaymentCardBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(58, 58, 58));
            PaymentCardBorder.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(26, 26, 26));
        }

        // ═══════════════════════════════════════
        // FOOTER — Save buttons are the only way to finalize
        // ═══════════════════════════════════════

        // ── Smart Hold/Held Receipts button ─────────────────────────────
        // Label is "Put on Hold" when cart has items, "Held Receipts" when empty.
        // Called by Cart_CollectionChanged in PosViewModel via RefreshHoldButton.
        public void RefreshHoldButton()
        {
            if (HoldBtn == null) return;
            if (DataContext is PosViewModel vm && vm.Cart.Count > 0)
                HoldBtn.Content = "Put on Hold";
            else
                HoldBtn.Content = "Held Receipts";
        }

        private void HoldBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;

            if (vm.Cart.Count > 0)
            {
                // Cart has items → put on hold
                vm.PutOnHold(_currentUser.Username);
                ResetPaymentCard();
                RefreshHoldButton();
            }
            else
            {
                // Cart empty → open Held Receipts window
                OpenHeldReceipts(vm);
            }
        }

        private void OpenHeldReceipts(PosViewModel vm)
        {
            var win = new HeldReceiptsWindow { Owner = this };
            win.OnUnhold = held =>
            {
                vm.RestoreFromHeld(held);
                RefreshHoldButton();
            };
            win.ShowDialog();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            vm.CancelReceipt();
            ResetPaymentCard();
        }

        private void SaveOnly_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (!CanFinalize(vm)) return;
            vm.FinalizeSale(vm.PaymentMethod);
            ResetPaymentCard();
        }

        private void SaveEmail_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (!CanFinalize(vm)) return;
            vm.FinalizeSale(vm.PaymentMethod);
            ResetPaymentCard();
        }

        private void SavePrint_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (!CanFinalize(vm)) return;
            vm.FinalizeSale(vm.PaymentMethod);
            ResetPaymentCard();
        }

        private bool CanFinalize(PosViewModel vm)
        {
            if (vm.Cart.Count == 0) { MessageBox.Show("Add items first."); return false; }
            if (!vm.HasSplits) { MessageBox.Show("Please select a payment method first."); return false; }
            if (vm.AmountDue > 0)
            {
                var r = MessageBox.Show(
                    $"There is still {vm.AmountDue:C2} owing.\n\nDo you want to save anyway?",
                    "Amount Still Owing", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (r != MessageBoxResult.Yes) return false;
            }
            return true;
        }

        // ═══════════════════════════════════════
        // NAV
        // ═══════════════════════════════════════

        private void HomeBtn_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            Session.Logout();
            new LoginWindow().ShowDialog();
            Close();
        }

        // ── Inline cell validation — fires immediately on Enter or focus loss ──

        // Shared: run Qty validation now and return whether it passed
        private bool ValidateQtyTextBox(System.Windows.Controls.TextBox tb)
        {
            if (tb.DataContext is not CartLine line) return true;

            // Parse whatever is currently in the box
            if (!int.TryParse(tb.Text.Trim(), out int entered) || entered < 1)
            {
                ShowCellError("Quantity must be 1 or more.\n\nEntering 0 is not allowed — use 'Remove' to delete the line.");
                tb.Text = System.Math.Max(1, line.Qty).ToString();
                tb.SelectAll();
                return false;
            }

            if (line.StockQty > 0 && entered > line.StockQty)
            {
                var action = ShowInsufficientStockDialog(line, entered);
                if (action == InsufficientStockAction.RemoveItem)
                {
                    if (DataContext is PosViewModel vmR) vmR.Cart.Remove(line);
                    return false;
                }
                else if (action == InsufficientStockAction.Cancel)
                {
                    line.Qty = line.StockQty;
                    tb.Text = line.StockQty.ToString();
                    tb.SelectAll();
                    return false;
                }
                // Continue — oversell accepted
            }

            line.Qty = entered;
            if (DataContext is PosViewModel vm) vm.RefreshTotals();
            return true;
        }

        private void QtyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;
            if (sender is not System.Windows.Controls.TextBox tb) return;
            e.Handled = true;
            ValidateQtyTextBox(tb);
        }

        private void QtyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
                ValidateQtyTextBox(tb);
        }

        // Shared: run Price validation now
        private bool ValidatePriceTextBox(System.Windows.Controls.TextBox tb)
        {
            if (tb.DataContext is not CartLine line) return true;

            var clean = tb.Text.Trim().Replace(",", "");
            if (!decimal.TryParse(clean,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal entered))
            {
                ShowCellError("Please enter a valid price.");
                tb.Text = line.UnitPrice.ToString("N2");
                tb.SelectAll();
                return false;
            }

            if (entered < 0)
            {
                ShowCellError("Price cannot be negative.\n\nEnter 0 for a 100% discount.");
                tb.Text = "0.00";
                line.UnitPrice = 0;
                tb.SelectAll();
                return false;
            }

            line.UnitPrice = entered;
            if (DataContext is PosViewModel vm) vm.RefreshTotals();
            return true;
        }

        private void PriceTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;
            if (sender is not System.Windows.Controls.TextBox tb) return;
            e.Handled = true;
            ValidatePriceTextBox(tb);
        }

        private void PriceTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox tb)
                ValidatePriceTextBox(tb);
        }

        // CellEditEnding still handles Name and TaxCode (no special immediacy needed)
        private void ReceiptGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row.Item is not CartLine line) return;

            var header = e.Column.Header?.ToString() ?? "";

            if (e.EditingElement is System.Windows.Controls.TextBox tb)
                tb.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();

            switch (header)
            {
                case "Item Name":
                    if (string.IsNullOrWhiteSpace(line.Name))
                    {
                        ShowCellError("Item Name cannot be empty.");
                        e.Cancel = true;
                    }
                    break;

                case "Tax Code":
                    if (string.IsNullOrWhiteSpace(line.TaxCode))
                    {
                        ShowCellError("Tax Code cannot be empty.\nSelect 'No Tax' if not applicable.");
                        e.Cancel = true;
                    }
                    break;
            }

            if (!e.Cancel && DataContext is PosViewModel vm)
                vm.RefreshTotals();
        }

        // ── Insufficient stock dialog ────────────────────────────────────

        private enum InsufficientStockAction { Continue, RemoveItem, Cancel }

        private InsufficientStockAction ShowInsufficientStockDialog(CartLine line, int requestedQty)
        {
            // Build a QB-style info window
            var win = new Window
            {
                Title = "Insufficient Quantity",
                Width = 480,
                Height = 340,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            var result = InsufficientStockAction.Cancel;

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

            // Blue header
            var header = new System.Windows.Controls.Border
            {
                Background = System.Windows.Media.Brushes.SteelBlue,
                Padding = new Thickness(12, 8, 12, 8)
            };
            var headerTxt = new System.Windows.Controls.TextBlock
            {
                Text = "You don't have sufficient quantity to sell this item:",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 13,
                TextWrapping = System.Windows.TextWrapping.Wrap
            };
            header.Child = headerTxt;
            System.Windows.Controls.Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Item info
            var infoPanel = new System.Windows.Controls.StackPanel { Margin = new Thickness(16, 10, 16, 4) };
            void AddRow(string label, string value)
            {
                var rowGrid = new System.Windows.Controls.Grid();
                rowGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(160) });
                rowGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                var lbl = new System.Windows.Controls.TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Foreground = System.Windows.Media.Brushes.Black, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 2, 8, 2) };
                var val = new System.Windows.Controls.TextBlock { Text = value, Foreground = System.Windows.Media.Brushes.Black, FontSize = 12, Margin = new Thickness(0, 2, 0, 2) };
                System.Windows.Controls.Grid.SetColumn(lbl, 0);
                System.Windows.Controls.Grid.SetColumn(val, 1);
                rowGrid.Children.Add(lbl);
                rowGrid.Children.Add(val);
                infoPanel.Children.Add(rowGrid);
            }

            AddRow("Item Name", line.Name);
            AddRow("Attribute", line.Attribute);
            AddRow("Size", line.Size);

            // Separator
            infoPanel.Children.Add(new System.Windows.Controls.Separator { Margin = new Thickness(0, 6, 0, 6), Background = System.Windows.Media.Brushes.LightGray });

            void AddQtyRow(string label, int qty, bool bold = false)
            {
                var rowGrid = new System.Windows.Controls.Grid();
                rowGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(60) });
                var fw = bold ? FontWeights.Bold : FontWeights.Normal;
                var lbl = new System.Windows.Controls.TextBlock { Text = label, FontWeight = fw, Foreground = System.Windows.Media.Brushes.Black, FontSize = 12, Margin = new Thickness(0, 2, 0, 2) };
                var val = new System.Windows.Controls.TextBlock { Text = qty.ToString(), FontWeight = fw, Foreground = System.Windows.Media.Brushes.Black, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 2, 0, 2) };
                System.Windows.Controls.Grid.SetColumn(lbl, 0);
                System.Windows.Controls.Grid.SetColumn(val, 1);
                rowGrid.Children.Add(lbl);
                rowGrid.Children.Add(val);
                infoPanel.Children.Add(rowGrid);
            }

            AddQtyRow("Quantity on hand:", line.StockQty);
            AddQtyRow("Quantity on Customer Orders:", 0);
            infoPanel.Children.Add(new System.Windows.Controls.Separator { Margin = new Thickness(0, 4, 0, 4), Background = System.Windows.Media.Brushes.LightGray });
            AddQtyRow("Quantity Available:", line.StockQty, bold: true);

            // Entered qty note
            infoPanel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = $"Entered sales receipt quantity:  {requestedQty}",
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.Black,
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 0)
            });

            System.Windows.Controls.Grid.SetRow(infoPanel, 1);
            grid.Children.Add(infoPanel);

            // Button row
            var btnPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(12, 6, 12, 10)
            };

            var btnContinue = new System.Windows.Controls.Button
            {
                Content = "Continue",
                Width = 90,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 0),
                Background = System.Windows.Media.Brushes.SteelBlue,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            var btnRemove = new System.Windows.Controls.Button
            {
                Content = "Remove Item",
                Width = 100,
                Height = 30,
                Background = System.Windows.Media.Brushes.SteelBlue,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };

            btnContinue.Click += (s, ev) => { result = InsufficientStockAction.Continue; win.Close(); };
            btnRemove.Click += (s, ev) => { result = InsufficientStockAction.RemoveItem; win.Close(); };

            btnPanel.Children.Add(btnContinue);
            btnPanel.Children.Add(btnRemove);
            System.Windows.Controls.Grid.SetRow(btnPanel, 3);
            grid.Children.Add(btnPanel);

            win.Content = grid;
            win.ShowDialog();
            return result;
        }

        private void ShowCellError(string message)
        {
            MessageBox.Show(message, "Invalid Value",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>Only allow digits in Qty cells.</summary>
        private void NumericOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        /// <summary>Only allow digits and one decimal point in Price cells.</summary>
        private void DecimalOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var tb = sender as System.Windows.Controls.TextBox;
            bool hasPoint = tb?.Text.Contains('.') == true;
            e.Handled = !(char.IsDigit(e.Text[0]) || (e.Text[0] == '.' && !hasPoint));
        }

        /// <summary>
        /// When Tax Code ComboBox changes update TaxRate so totals recalculate.
        /// </summary>
        private void TaxCodeCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (sender is not System.Windows.Controls.ComboBox combo) return;
            if (combo.DataContext is not CartLine line) return;
            if (combo.SelectedItem is not string code) return;

            line.TaxCode = code;
            line.TaxRate = ParseTaxRate(code);

            if (DataContext is PosViewModel vm)
                vm.RefreshTotals();
        }

        private static decimal ParseTaxRate(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return 0m;
            var clean = code.Replace("%", "").Trim();
            if (decimal.TryParse(clean,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var rate))
                return rate;
            return 0m;
        }
    }
}