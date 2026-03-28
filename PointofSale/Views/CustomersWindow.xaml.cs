using PointofSale.Data;
using PointofSale.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace PointofSale.Views
{
    public partial class CustomersWindow : Window
    {
        private readonly ObservableCollection<Customer> _items = new();
        private ICollectionView? _view;

        private Customer? SelectedCustomer => CustomersGrid.SelectedItem as Customer;

        public CustomersWindow()
        {
            InitializeComponent();
            LoadCustomers();
            SetupFilter();
            SetMessage("Ready.");
        }

        private void LoadCustomers()
        {
            using var db = new AppDbContext();

            // Load ALL customers — active and inactive
            var list = db.Customers
                         .OrderBy(c => c.LastName)
                         .ThenBy(c => c.FirstName)
                         .ToList();

            _items.Clear();
            foreach (var c in list)
                _items.Add(c);

            CustomersGrid.ItemsSource = _items;
        }

        private void SetupFilter()
        {
            _view = CollectionViewSource.GetDefaultView(_items);
            _view.Filter = FilterItem;
        }

        private bool FilterItem(object obj)
        {
            if (obj is not Customer c) return false;
            var term = (SearchBox.Text ?? "").Trim().ToLower();
            if (string.IsNullOrWhiteSpace(term)) return true;

            return c.FirstName.ToLower().Contains(term)
                || c.LastName.ToLower().Contains(term)
                || (c.Phone ?? "").ToLower().Contains(term)
                || (c.Email ?? "").ToLower().Contains(term)
                || (c.City ?? "").ToLower().Contains(term)
                || c.Id.ToString().Contains(term);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => _view?.Refresh();

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadCustomers();
            SetupFilter();
            SetMessage("Refreshed.");
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var win = new CustomerEditWindow(null) { Owner = this };
            win.ShowDialog();
            if (win.Saved)
            {
                LoadCustomers();
                SetMessage("Customer added.");
            }
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCustomer == null)
            {
                MessageBox.Show("Select a customer first.");
                return;
            }
            var win = new CustomerEditWindow(SelectedCustomer.Id) { Owner = this };
            win.ShowDialog();
            if (win.Saved)
            {
                LoadCustomers();
                SetMessage("Customer saved.");
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var c = SelectedCustomer;
            if (c == null) { MessageBox.Show("Select a customer first."); return; }

            var confirm = MessageBox.Show(
                $"Permanently delete '{c.FullName}'?\n\nThis cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            using var db = new AppDbContext();
            var cust = db.Customers.FirstOrDefault(x => x.Id == c.Id);
            if (cust == null) { MessageBox.Show("Customer not found. Refresh and try again."); return; }

            db.Customers.Remove(cust);
            db.SaveChanges();

            LoadCustomers();
            SetupFilter();
            SetMessage($"Deleted: {c.FullName}");
        }

        private void DeactivateBtn_Click(object sender, RoutedEventArgs e)
        {
            var c = SelectedCustomer;
            if (c == null) return;

            var confirm = MessageBox.Show(
                $"Deactivate '{c.FullName}'?\n\nThey will remain visible but greyed out.\nYou can reactivate them at any time.",
                "Confirm Deactivate",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            SetActiveStatus(c.Id, false);
            SetMessage($"Deactivated: {c.FullName}");
        }

        private void ReactivateBtn_Click(object sender, RoutedEventArgs e)
        {
            var c = SelectedCustomer;
            if (c == null) return;

            SetActiveStatus(c.Id, true);
            SetMessage($"Reactivated: {c.FullName}");
        }

        private void SetActiveStatus(int customerId, bool active)
        {
            using var db = new AppDbContext();
            var cust = db.Customers.FirstOrDefault(x => x.Id == customerId);
            if (cust == null) return;

            cust.IsActive = active;
            db.SaveChanges();

            LoadCustomers();
            SetupFilter();
        }

        private void CustomersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var c = SelectedCustomer;
            if (c == null) { SetMessage("Ready."); return; }

            var status = c.IsActive ? "Active" : "Inactive";
            SetMessage($"Selected: {c.FullName}  |  Phone: {c.Phone}  |  Balance: R{c.AccountBalance:N2}  |  Status: {status}");
        }

        private void CustomersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedCustomer != null && SelectedCustomer.IsActive)
                EditBtn_Click(sender, e);
        }

        private void SetMessage(string msg) => MsgText.Text = msg;
    }
}