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
            if (!ScanBoxTop.IsKeyboardFocusWithin && !ProductPopup.IsOpen)
                ScanBoxTop.Focus();
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
        // PAYMENTS — record method only, stay on page
        // No popups, no navigation, no saving.
        // Totals update live on screen.
        // ═══════════════════════════════════════

        private void Cash_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (vm.Cart.Count == 0) { MessageBox.Show("Add items first."); return; }

            var cashWin = new CashPaymentWindow(vm.Total) { Owner = this };
            if (cashWin.ShowDialog() != true) return;

            var tendered = cashWin.AmountTendered;
            if (tendered < vm.Total) { MessageBox.Show("Amount tendered is less than total."); return; }

            vm.PaymentMethod = "Cash";
            vm.CashTendered = tendered;
            vm.RefreshTotals();
            // Stay on page — user must press Save Only / Save & Print / Save & Email to finalize
        }

        private void Credit_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (vm.Cart.Count == 0) { MessageBox.Show("Add items first."); return; }
            vm.PaymentMethod = "Credit";
            vm.CashTendered = vm.Total;
            vm.RefreshTotals();
        }

        private void Check_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (vm.Cart.Count == 0) { MessageBox.Show("Add items first."); return; }
            vm.PaymentMethod = "Check";
            vm.CashTendered = vm.Total;
            vm.RefreshTotals();
        }

        private void Gift_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show("Gift payment flow can be added next.");

        private void RemovePayment_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (string.IsNullOrEmpty(vm.PaymentMethod)) { MessageBox.Show("No payment method to remove."); return; }
            vm.RemovePaymentMethod();
        }

        private void Account_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show("Account payment flow can be added next.");

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
        }

        private void SaveOnly_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (!CanFinalize(vm)) return;
            vm.FinalizeSale(vm.PaymentMethod);
        }

        private void SaveEmail_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (!CanFinalize(vm)) return;
            // TODO: hook up email sending
            vm.FinalizeSale(vm.PaymentMethod);
        }

        private void SavePrint_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PosViewModel vm) return;
            if (!CanFinalize(vm)) return;
            // TODO: hook up printing
            vm.FinalizeSale(vm.PaymentMethod);
        }

        private bool CanFinalize(PosViewModel vm)
        {
            if (vm.Cart.Count == 0)
            {
                MessageBox.Show("Add items first.");
                return false;
            }
            if (string.IsNullOrEmpty(vm.PaymentMethod))
            {
                MessageBox.Show("Please select a payment method first (Cash, Credit, Check, etc).");
                return false;
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
    }
}