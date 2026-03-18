using System.Windows;
using System.Windows.Controls;
using PointofSale.Services;
using PointofSale.Views;

namespace PointofSale
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ApplyPermissions();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Session.CurrentUser == null)
            {
                var login = new LoginWindow();
                login.ShowDialog();
                ApplyPermissions();
            }
        }

        // ── Sign In / Sign Out button ────────────────────────────────────
        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            if (Session.CurrentUser == null)
            {
                PromptLogin();
            }
            else
            {
                Session.Logout();
                ApplyPermissions();
                MessageBox.Show("Signed out successfully.", "Signed Out",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ── Central login prompt (reused everywhere) ─────────────────────
        /// <summary>
        /// Shows the login dialog and refreshes permissions.
        /// Returns true if the user is now logged in.
        /// </summary>
        private bool PromptLogin()
        {
            var login = new LoginWindow();
            login.ShowDialog();
            ApplyPermissions();
            return Session.CurrentUser != null;
        }

        // ── Permission / visual state ────────────────────────────────────
        private void ApplyPermissions()
        {
            var user = Session.CurrentUser;

            if (user == null)
            {
                SignInBtn.Content = "Sign In";

                // ALL buttons disabled when no user
                SetEnabled(false,
                    MakeSaleBtn, HeldReceiptsBtn,
                    ReceiveStockBtn, ProductsBtn, ProductsBtn_Tile,
                    CustomersListBtn, CustomersListBtn_Tile,
                    EmployeesBtn, EmployeesBtn_Tile,
                    ReportsBtn, ReportsBtn_Tile,
                    EndOfDayBtn, EndOfDayBtn_Tile,
                    SalesHistoryBtn,
                    SettingsBtn, SettingsBtn_Tile,
                    SupplierTileBtn, DepartmentsTileBtn,
                    ReceiveStockBtn_Tile);

                UpdateButtonVisuals();
                return;
            }

            SignInBtn.Content = $"{user.Role}: {user.Username}";

            bool isAdmin = user.Role == "Admin";
            bool isCashier = user.Role == "Cashier";

            // Always available once logged in
            SetEnabled(true,
                MakeSaleBtn, HeldReceiptsBtn, EndOfDayBtn, EndOfDayBtn_Tile);

            // Admin-only
            SetEnabled(isAdmin,
                ProductsBtn, ProductsBtn_Tile,
                CustomersListBtn, CustomersListBtn_Tile,
                EmployeesBtn, EmployeesBtn_Tile,
                ReportsBtn, ReportsBtn_Tile,
                SalesHistoryBtn,
                ReceiveStockBtn, ReceiveStockBtn_Tile,
                SettingsBtn, SettingsBtn_Tile,
                SupplierTileBtn, DepartmentsTileBtn);

            UpdateButtonVisuals();
        }

        private static void SetEnabled(bool enabled, params Button?[] buttons)
        {
            foreach (var btn in buttons)
                if (btn != null) btn.IsEnabled = enabled;
        }

        private void UpdateButtonVisuals()
        {
            void ApplyVisual(Button? btn)
            {
                if (btn == null) return;
                btn.Opacity = btn.IsEnabled ? 1.0 : 0.35;
                btn.IsTabStop = btn.IsEnabled;
            }

            ApplyVisual(MakeSaleBtn);
            ApplyVisual(HeldReceiptsBtn);
            ApplyVisual(ProductsBtn);
            ApplyVisual(ProductsBtn_Tile);
            ApplyVisual(CustomersListBtn);
            ApplyVisual(CustomersListBtn_Tile);
            ApplyVisual(EmployeesBtn);
            ApplyVisual(EmployeesBtn_Tile);
            ApplyVisual(ReportsBtn);
            ApplyVisual(ReportsBtn_Tile);
            ApplyVisual(EndOfDayBtn);
            ApplyVisual(EndOfDayBtn_Tile);
            ApplyVisual(SalesHistoryBtn);
            ApplyVisual(ReceiveStockBtn);
            ApplyVisual(ReceiveStockBtn_Tile);
            ApplyVisual(SettingsBtn);
            ApplyVisual(SettingsBtn_Tile);
            ApplyVisual(SupplierTileBtn);
            ApplyVisual(DepartmentsTileBtn);
        }

        // ── Guard helper used in every click handler ─────────────────────
        /// <summary>
        /// Returns true if a user is logged in.
        /// If not, shows a prompt and offers to open the login dialog.
        /// </summary>
        private bool RequireLogin()
        {
            if (Session.CurrentUser != null) return true;

            var result = MessageBox.Show(
                "You need to be signed in to use this feature.\n\nWould you like to sign in now?",
                "Sign In Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
                return PromptLogin();

            return false;
        }

        // ── Click handlers ───────────────────────────────────────────────
        private void MakeSale_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;

            var win = new PointOfSaleWindow(Session.CurrentUser!);
            win.Show();
            Close();
        }

        private void HeldReceipts_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;

            var win = new HeldReceiptsWindow();
            win.ShowDialog();
        }

        private void Products_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;

            var win = new ProductsWindow();
            win.ShowDialog();
        }

        private void CustomersList_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            MessageBox.Show("Customers screen coming next.");
        }

        private void Users_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            MessageBox.Show("Users screen coming next.");
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            MessageBox.Show("Reports screen coming next.");
        }

        private void EndOfDay_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            MessageBox.Show("End of Day screen coming next.");
        }

        private void SalesHistory_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            MessageBox.Show("Sales History screen coming next.");
        }

        private void ReceiveStock_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            MessageBox.Show("Receive Stock screen coming next.");
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            new StoreSettingsWindow { Owner = this }.ShowDialog();
        }

        private void Supplier_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;

            var win = new SuppliersWindow { Owner = this };
            win.ShowDialog();
        }

        private void Departments_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;

            var win = new DepartmentsWindow { Owner = this };
            win.ShowDialog();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Session.Logout();
            var login = new LoginWindow();
            login.ShowDialog();
            Close();
        }
    }
}