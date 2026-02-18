using PointofSale.Models;
using PointofSale.Services;
using System.Windows;
using PointofSale.ViewModels;
using System.Windows.Input;

namespace PointofSale.Views
{
    public partial class PointOfSaleWindow : Window
    {
        private readonly AppUser _currentUser;

        public PointOfSaleWindow(AppUser user)
        {
            InitializeComponent();

            _currentUser = user;

            UserLabel.Text = $"{_currentUser.Role}: {_currentUser.Username}";
            ApplyRolePermissions();

            DataContext = new PosViewModel();
        }

        private void ApplyRolePermissions()
        {
            bool isAdmin = _currentUser.Role == "Admin";

            ProductsBtn.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            UsersBtn.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            ReportsBtn.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        // ?? NAV BAR EVENTS 
        private void ProductsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser.Role != "Admin")
            {
                MessageBox.Show("Access denied.");
                return;
            }

            var win = new Views.ProductsWindow
            {
                Owner = this
            };

            win.ShowDialog();
        }

        private void ScanBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var sku = ScanBox.Text.Trim();
            ScanBox.Clear();

            if (DataContext is PosViewModel vm)
                vm.ScanSkuAndAddToCart(sku);
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // If user starts scanning, force focus to ScanBox
            if (!ScanBox.IsKeyboardFocusWithin)
                ScanBox.Focus();
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ScanBox.Focus();
        }


        private void UsersBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("User management screen coming next.");
        }

        private void ReportsBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Reports screen coming next.");
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            Session.Logout();

            var login = new LoginWindow();
            login.ShowDialog();
            Close();
        }
    }
}
