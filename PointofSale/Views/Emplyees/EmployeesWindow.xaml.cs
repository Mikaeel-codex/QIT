using PointofSale.Data;
using PointofSale.Models;
using PointofSale.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace PointofSale.Views
{
    public partial class EmployeesWindow : Window
    {
        private readonly ObservableCollection<AppUser> _items = new();
        private ICollectionView? _view;

        private AppUser? SelectedEmployee => EmployeesGrid.SelectedItem as AppUser;

        public EmployeesWindow()
        {
            InitializeComponent();
            LoadEmployees();
            SetupFilter();
            SetMessage("Ready.");
        }

        private void LoadEmployees()
        {
            using var db = new AppDbContext();
            var list = db.Users
                         .OrderBy(u => u.FullName)
                         .ToList();

            _items.Clear();
            foreach (var u in list)
                _items.Add(u);

            EmployeesGrid.ItemsSource = _items;
        }

        private void SetupFilter()
        {
            _view = CollectionViewSource.GetDefaultView(_items);
            _view.Filter = FilterItem;
        }

        private bool FilterItem(object obj)
        {
            if (obj is not AppUser u) return false;
            var term = (SearchBox.Text ?? "").Trim().ToLower();
            if (string.IsNullOrWhiteSpace(term)) return true;

            return (u.FullName ?? "").ToLower().Contains(term)
                || u.Username.ToLower().Contains(term)
                || u.Role.ToLower().Contains(term)
                || (u.Phone ?? "").ToLower().Contains(term)
                || (u.Email ?? "").ToLower().Contains(term);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => _view?.Refresh();

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployees();
            SetupFilter();
            SetMessage("Refreshed.");
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var win = new EmployeeEditWindow(null) { Owner = this };
            win.ShowDialog();
            if (win.Saved)
            {
                LoadEmployees();
                SetMessage("Employee added.");
            }
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEmployee == null) { MessageBox.Show("Select an employee first."); return; }
            var win = new EmployeeEditWindow(SelectedEmployee.Id) { Owner = this };
            win.ShowDialog();
            if (win.Saved)
            {
                LoadEmployees();
                SetMessage("Employee saved.");
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var u = SelectedEmployee;
            if (u == null) { MessageBox.Show("Select an employee first."); return; }

            // Prevent deleting yourself
            if (Session.CurrentUser?.Id == u.Id)
            {
                MessageBox.Show("You cannot delete your own account.", "Not Allowed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Permanently delete '{u.DisplayName}'?\n\nThis cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            using var db = new AppDbContext();
            var emp = db.Users.FirstOrDefault(x => x.Id == u.Id);
            if (emp == null) { MessageBox.Show("Employee not found. Refresh and try again."); return; }

            db.Users.Remove(emp);
            db.SaveChanges();

            LoadEmployees();
            SetupFilter();
            SetMessage($"Deleted: {u.DisplayName}");
        }

        private void ReactivateBtn_Click(object sender, RoutedEventArgs e)
        {
            var u = SelectedEmployee;
            if (u == null) return;
            SetActiveStatus(u.Id, true);
            SetMessage($"Reactivated: {u.DisplayName}");
        }

        private void DeactivateBtn_Click(object sender, RoutedEventArgs e)
        {
            var u = SelectedEmployee;
            if (u == null) return;

            if (Session.CurrentUser?.Id == u.Id)
            {
                MessageBox.Show("You cannot deactivate your own account.", "Not Allowed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Deactivate '{u.DisplayName}'?\n\nThey will not be able to log in until reactivated.",
                "Confirm Deactivate",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            SetActiveStatus(u.Id, false);
            SetMessage($"Deactivated: {u.DisplayName}");
        }

        private void ResetPasswordBtn_Click(object sender, RoutedEventArgs e)
        {
            var u = SelectedEmployee;
            if (u == null) return;

            var win = new ResetPasswordWindow(u.Id, u.DisplayName) { Owner = this };
            win.ShowDialog();
            if (win.Saved)
                SetMessage($"Password reset for: {u.DisplayName}");
        }

        private void SetActiveStatus(int userId, bool active)
        {
            using var db = new AppDbContext();
            var emp = db.Users.FirstOrDefault(x => x.Id == userId);
            if (emp == null) return;
            emp.IsActive = active;
            db.SaveChanges();
            LoadEmployees();
            SetupFilter();
        }

        private void EmployeesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var u = SelectedEmployee;
            if (u == null) { SetMessage("Ready."); return; }
            SetMessage($"Selected: {u.DisplayName}  |  Role: {u.Role}  |  Username: {u.Username}");
        }

        private void EmployeesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedEmployee != null)
                EditBtn_Click(sender, e);
        }

        private void SetMessage(string msg) => MsgText.Text = msg;
    }
}