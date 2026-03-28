using PointofSale.Data;
using PointofSale.Models;
using PointofSale.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PointofSale.Views
{
    public partial class EmployeeEditWindow : Window
    {
        private readonly int? _editId;
        public bool Saved { get; private set; } = false;

        // All permission checkboxes mapped to their permission key
        private Dictionary<CheckBox, string> _permMap = new();

        public EmployeeEditWindow(int? editUserId = null)
        {
            InitializeComponent();
            _editId = editUserId;
            Title = _editId == null ? "Add Employee" : "Edit Employee";

            // Build permission map
            _permMap = new Dictionary<CheckBox, string>
            {
                { PermMakeSales,        "MakeSales"        },
                { PermViewSalesHistory, "ViewSalesHistory"  },
                { PermVoidSales,        "VoidSales"         },
                { PermViewInventory,    "ViewInventory"     },
                { PermManageInventory,  "ManageInventory"   },
                { PermManageCustomers,  "ManageCustomers"   },
                { PermManageSuppliers,  "ManageSuppliers"   },
                { PermViewReports,      "ViewReports"       },
                { PermAccessSettings,   "AccessSettings"    },
                { PermManageEmployees,  "ManageEmployees"   },
            };

            if (_editId != null)
            {
                PasswordHintTxt.Visibility = Visibility.Visible;
                LoadForEdit();
            }
        }

        private void LoadForEdit()
        {
            using var db = new AppDbContext();
            var u = db.Users.FirstOrDefault(x => x.Id == _editId!.Value);
            if (u == null) { MessageBox.Show("Employee not found."); Close(); return; }

            FullNameBox.Text = u.FullName ?? "";
            UsernameBox.Text = u.Username;
            PhoneBox.Text = u.Phone ?? "";
            EmailBox.Text = u.Email ?? "";
            IsActiveCheck.IsChecked = u.IsActive;

            // Set role ComboBox
            foreach (ComboBoxItem item in RoleBox.Items)
                if (item.Content?.ToString() == u.Role)
                { RoleBox.SelectedItem = item; break; }

            // Load permissions
            var existing = u.GetPermissions().ToHashSet();
            foreach (var (cb, key) in _permMap)
                cb.IsChecked = existing.Contains(key);

            UpdatePermissionsPanel();
        }

        private void RoleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => UpdatePermissionsPanel();

        private void UpdatePermissionsPanel()
        {
            var role = (RoleBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            bool isAdminRole = role == "Admin" || role == "Co-Admin";

            AdminNoticeBorder.Visibility = isAdminRole
                ? Visibility.Visible : Visibility.Collapsed;

            // Disable all checkboxes for admin roles — they get everything automatically
            foreach (var cb in _permMap.Keys)
            {
                cb.IsEnabled = !isAdminRole;
                if (isAdminRole) cb.IsChecked = true;
            }
        }

        private bool ValidateInputs(out string error)
        {
            error = "";

            if (string.IsNullOrWhiteSpace(UsernameBox.Text))
            { error = "Username is required."; return false; }

            if (RoleBox.SelectedItem == null)
            { error = "Please select a role."; return false; }

            // Password required for new employees
            if (_editId == null && string.IsNullOrWhiteSpace(PasswordBox.Password))
            { error = "Password is required for new employees."; return false; }

            // If password entered, confirm must match
            if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                { error = "Passwords do not match."; return false; }

                if (PasswordBox.Password.Length < 4)
                { error = "Password must be at least 4 characters."; return false; }
            }

            // Check username uniqueness
            using var db = new AppDbContext();
            var existing = db.Users.FirstOrDefault(u => u.Username == UsernameBox.Text.Trim());
            if (existing != null && existing.Id != (_editId ?? -1))
            { error = $"Username '{UsernameBox.Text.Trim()}' is already taken."; return false; }

            return true;
        }

        private void SaveInternal()
        {
            using var db = new AppDbContext();
            var auth = new AuthService();

            AppUser u;
            if (_editId == null)
            {
                u = new AppUser();
                db.Users.Add(u);
            }
            else
            {
                u = db.Users.First(x => x.Id == _editId.Value);
            }

            u.FullName = FullNameBox.Text.Trim();
            u.Username = UsernameBox.Text.Trim();
            u.Phone = PhoneBox.Text.Trim();
            u.Email = EmailBox.Text.Trim();
            u.Role = (RoleBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cashier";
            u.IsActive = IsActiveCheck.IsChecked ?? true;

            // Set password if provided
            if (!string.IsNullOrWhiteSpace(PasswordBox.Password))
                auth.SetPassword(u, PasswordBox.Password);

            // Save permissions
            var role = u.Role;
            bool isAdminRole = role == "Admin" || role == "Co-Admin";

            if (isAdminRole)
            {
                // Admin gets all permissions
                u.SetPermissions(_permMap.Values);
            }
            else
            {
                var selected = _permMap
                    .Where(kv => kv.Key.IsChecked == true)
                    .Select(kv => kv.Value);
                u.SetPermissions(selected);
            }

            db.SaveChanges();
            Saved = true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out var error))
            {
                StatusTxt.Text = error;
                StatusTxt.Visibility = Visibility.Visible;
                return;
            }
            StatusTxt.Visibility = Visibility.Collapsed;
            SaveInternal();
            if (Saved) Close();
        }

        private void SaveAndNew_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out var error))
            {
                StatusTxt.Text = error;
                StatusTxt.Visibility = Visibility.Visible;
                return;
            }
            StatusTxt.Visibility = Visibility.Collapsed;
            SaveInternal();
            if (Saved) ClearFormForNew();
        }

        private void ClearFormForNew()
        {
            FullNameBox.Text = "";
            UsernameBox.Text = "";
            PhoneBox.Text = "";
            EmailBox.Text = "";
            PasswordBox.Clear();
            ConfirmPasswordBox.Clear();
            RoleBox.SelectedIndex = -1;
            IsActiveCheck.IsChecked = true;
            foreach (var cb in _permMap.Keys) cb.IsChecked = false;
            Saved = false;
            Title = "Add Employee";
            FullNameBox.Focus();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Saved = false;
            Close();
        }
    }
}