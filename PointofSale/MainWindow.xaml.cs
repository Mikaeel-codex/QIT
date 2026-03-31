using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PointofSale.Services;
using PointofSale.ViewModels;
using PointofSale.Views;

namespace PointofSale
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _clock = new();
        private bool _profileOpen = false;

        public MainWindow()
        {
            InitializeComponent();
            ApplyPermissions();
            StartClock();
        }

        // ── Clock ─────────────────────────────────────────────────────────
        private void StartClock()
        {
            _clock.Interval = TimeSpan.FromSeconds(1);
            _clock.Tick += (s, e) => UpdateClock();
            _clock.Start();
            UpdateClock();
        }

        private void UpdateClock()
            => ClockTxt.Text = DateTime.Now.ToString("dddd, MMMM d, yyyy  •  HH:mm:ss");

        // ── Startup ───────────────────────────────────────────────────────
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Session.CurrentUser == null)
            {
                new LoginWindow().ShowDialog();
                ApplyPermissions();
            }
        }

        // ── Hamburger toggle ──────────────────────────────────────────────
        private void CloseProfilePopup()
        {
            _profileOpen = false;
            ProfilePopup.IsOpen = false;
            AnimateHamburger(false);
        }

        private void ProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            _profileOpen = !_profileOpen;
            ProfilePopup.IsOpen = _profileOpen;
            AnimateHamburger(_profileOpen);
        }

        private void MainGrid_PreviewMouseLeftButtonDown(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!_profileOpen) return;
            if (ProfileToggle.IsMouseOver) return;
            CloseProfilePopup();
        }

        private void AnimateHamburger(bool open)
        {
            var duration = new Duration(TimeSpan.FromMilliseconds(220));
            var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };

            Line1Rotate.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(open ? 45 : 0, duration) { EasingFunction = ease });
            Line1Translate.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(open ? 6 : 0, duration) { EasingFunction = ease });
            Line2.BeginAnimation(OpacityProperty,
                new DoubleAnimation(open ? 0 : 1, duration) { EasingFunction = ease });
            Line3Rotate.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(open ? -45 : 0, duration) { EasingFunction = ease });
            Line3Translate.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(open ? -6 : 0, duration) { EasingFunction = ease });

            var strokeBrush = new SolidColorBrush(open
                ? Color.FromRgb(0xF4, 0xC5, 0x42)
                : Color.FromRgb(0xAF, 0xAF, 0xAF));
            Line1.Stroke = strokeBrush;
            Line2.Stroke = strokeBrush;
            Line3.Stroke = strokeBrush;
        }

        // ── Dropdown menu handlers ─────────────────────────────────────────
        private void SignInMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CloseProfilePopup();
            new LoginWindow().ShowDialog();
            ApplyPermissions();
        }

        private void MyAccount_Click(object sender, RoutedEventArgs e)
        {
            CloseProfilePopup();
            MessageBox.Show("My Account coming soon.", "Coming Soon",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Subscription_Click(object sender, RoutedEventArgs e)
        {
            CloseProfilePopup();
            MessageBox.Show("Subscription", "Coming Soon",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void HelpCenter_Click(object sender, RoutedEventArgs e)
        {
            CloseProfilePopup();
            MessageBox.Show("Help Center coming soon.", "Coming Soon",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            CloseProfilePopup();
            var userName = Session.CurrentUser?.Username ?? "";
            Session.Logout();
            ApplyPermissions();
            MessageBox.Show($"{userName} has been signed out successfully.",
                "Signed Out", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Permissions + feature flags ───────────────────────────────────
        private void ApplyPermissions()
        {
            var user = Session.CurrentUser;

            // Step 1 — Apply dev feature flags first (hides tiles completely)
            ApplyFeatureFlags();

            if (user == null)
            {
                SetNavbarUser("Sign In", "", "?");
                WelcomeTxt.Text = "Welcome! Click Account menu to sign in.";

                SignInMenuItem.Visibility = Visibility.Visible;
                Subscription.Visibility = Visibility.Collapsed;
                MyAccountMenuItem.Visibility = Visibility.Collapsed;
                MenuSeparator.Visibility = Visibility.Collapsed;
                HelpCenterMenuItem.Visibility = Visibility.Collapsed;
                SignOutMenuItem.Visibility = Visibility.Collapsed;

                SetEnabled(false,
                    MakeSaleBtn, HeldReceiptsBtn,
                    ProductsBtn_Tile, CustomersListBtn_Tile,
                    EmployeesBtn_Tile, ReportsBtn_Tile,
                    EndOfDayBtn_Tile, SalesHistoryBtn,
                    SettingsBtn_Tile, SupplierTileBtn,
                    DepartmentsTileBtn, ReceiveStockBtn_Tile);

                UpdateButtonVisuals();
                return;
            }

            var displayName = user.Username;
            SetNavbarUser(displayName, user.Role, BuildInitials(displayName));
            WelcomeTxt.Text = $"Welcome back, {displayName.Split(' ')[0]}!";

            SignInMenuItem.Visibility = Visibility.Collapsed;
            Subscription.Visibility = Visibility.Visible;
            MyAccountMenuItem.Visibility = Visibility.Visible;
            MenuSeparator.Visibility = Visibility.Visible;
            HelpCenterMenuItem.Visibility = Visibility.Visible;
            SignOutMenuItem.Visibility = Visibility.Visible;

            // Step 2 — Apply user role permissions
            SetEnabled(user.CanMakeSales, MakeSaleBtn);
            SetEnabled(user.CanMakeSales, HeldReceiptsBtn);
            SetEnabled(user.CanMakeSales, EndOfDayBtn_Tile);
            SetEnabled(user.CanViewInventory, ProductsBtn_Tile);
            SetEnabled(user.CanManageInventory, ReceiveStockBtn_Tile);
            SetEnabled(user.CanManageCustomers, CustomersListBtn_Tile);
            SetEnabled(user.CanManageEmployees, EmployeesBtn_Tile);
            SetEnabled(user.CanViewReports, ReportsBtn_Tile);
            SetEnabled(user.CanViewSalesHistory, SalesHistoryBtn);
            SetEnabled(user.CanAccessSettings, SettingsBtn_Tile);
            SetEnabled(user.CanManageSuppliers, SupplierTileBtn);
            SetEnabled(user.CanManageSuppliers, DepartmentsTileBtn);

            // Step 3 — Feature flags override permissions (disabled = also force off)
            if (!DevSettings.CustomersEnabled) SetEnabled(false, CustomersListBtn_Tile);
            if (!DevSettings.InventoryEnabled) SetEnabled(false, ProductsBtn_Tile);
            if (!DevSettings.ReceiveStockEnabled) SetEnabled(false, ReceiveStockBtn_Tile);
            if (!DevSettings.SalesHistoryEnabled) SetEnabled(false, SalesHistoryBtn);
            if (!DevSettings.ReportsEnabled) SetEnabled(false, ReportsBtn_Tile);
            if (!DevSettings.SuppliersEnabled) SetEnabled(false, SupplierTileBtn);
            if (!DevSettings.DepartmentsEnabled) SetEnabled(false, DepartmentsTileBtn);
            if (!DevSettings.EndOfDayEnabled) SetEnabled(false, EndOfDayBtn_Tile);

            UpdateButtonVisuals();
        }

        /// <summary>
        /// Collapses entire tile cards based on dev feature flags.
        /// Runs before permission checks — zero trace for disabled features.
        /// </summary>
        private void ApplyFeatureFlags()
        {
            SetVisible(DevSettings.CustomersEnabled, CustomersListBtn_Tile);
            SetVisible(DevSettings.InventoryEnabled, ProductsBtn_Tile);
            SetVisible(DevSettings.ReceiveStockEnabled, ReceiveStockBtn_Tile);
            SetVisible(DevSettings.SalesHistoryEnabled, SalesHistoryBtn);
            SetVisible(DevSettings.ReportsEnabled, ReportsBtn_Tile);
            SetVisible(DevSettings.SuppliersEnabled, SupplierTileBtn);
            SetVisible(DevSettings.DepartmentsEnabled, DepartmentsTileBtn);
            SetVisible(DevSettings.EndOfDayEnabled, EndOfDayBtn_Tile);
        }

        /// <summary>
        /// Walks up the visual tree to find the wrapping Border tile
        /// and collapses the whole card — not just the button.
        /// </summary>
        private static void SetVisible(bool visible, Button? btn)
        {
            if (btn == null) return;
            var visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            var parent = VisualTreeHelper.GetParent(btn);
            while (parent != null)
            {
                if (parent is Border border)
                {
                    border.Visibility = visibility;
                    return;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }

            // Fallback
            btn.Visibility = visibility;
        }

        private void SetNavbarUser(string name, string role, string initials)
        {
            NavUserNameTxt.Text = name;
            NavUserRoleTxt.Text = role;
            NavAvatarTxt.Text = initials;
            PopupUserNameTxt.Text = name;
            PopupUserRoleTxt.Text = role;
            PopupAvatarTxt.Text = initials;
        }

        private static string BuildInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 1
                ? parts[0][0].ToString().ToUpper()
                : $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        }

        private static void SetEnabled(bool enabled, params Button?[] buttons)
        {
            foreach (var btn in buttons)
                if (btn != null) btn.IsEnabled = enabled;
        }

        private void UpdateButtonVisuals()
        {
            void Apply(Button? btn)
            {
                if (btn == null) return;
                btn.Opacity = btn.IsEnabled ? 1.0 : 0.35;
                btn.IsTabStop = btn.IsEnabled;
            }
            Apply(MakeSaleBtn); Apply(HeldReceiptsBtn);
            Apply(ProductsBtn_Tile); Apply(CustomersListBtn_Tile);
            Apply(EmployeesBtn_Tile); Apply(ReportsBtn_Tile);
            Apply(EndOfDayBtn_Tile); Apply(SalesHistoryBtn);
            Apply(ReceiveStockBtn_Tile); Apply(SettingsBtn_Tile);
            Apply(SupplierTileBtn); Apply(DepartmentsTileBtn);
        }

        private bool RequireLogin()
        {
            if (Session.CurrentUser != null) return true;
            var result = MessageBox.Show(
                "You need to be signed in.\n\nWould you like to sign in now?",
                "Sign In Required", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                new LoginWindow().ShowDialog();
                ApplyPermissions();
                return Session.CurrentUser != null;
            }
            return false;
        }

        // ── Tile handlers ─────────────────────────────────────────────────
        private void MakeSale_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            _clock.Stop();
            new PointOfSaleWindow(Session.CurrentUser!).Show();
            Close();
        }

        private void HeldReceipts_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            var win = new HeldReceiptsWindow { Owner = this };
            win.OnUnhold = held =>
            {
                _clock.Stop();
                var posWin = new PointOfSaleWindow(Session.CurrentUser!);
                posWin.Show();
                if (posWin.DataContext is PosViewModel vm)
                    vm.RestoreFromHeld(held);
                Close();
            };
            win.ShowDialog();
        }

        private void Products_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            new ProductsWindow().ShowDialog();
        }

        private void CustomersList_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            new CustomersWindow { Owner = this }.ShowDialog();
        }

        private void Users_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            new EmployeesWindow { Owner = this }.ShowDialog();
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            new ReportsWindow { Owner = this }.ShowDialog();
        }

        private void EndOfDay_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            MessageBox.Show("End of Day screen coming next.");
        }

        private void SalesHistory_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            new SalesHistoryWindow { Owner = this }.ShowDialog();
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
            new SuppliersWindow { Owner = this }.ShowDialog();
        }

        private void Departments_Click(object sender, RoutedEventArgs e)
        {
            if (!RequireLogin()) return;
            new DepartmentsWindow { Owner = this }.ShowDialog();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            _clock.Stop();
            Session.Logout();
            new LoginWindow().ShowDialog();
            Close();
        }
    }
}