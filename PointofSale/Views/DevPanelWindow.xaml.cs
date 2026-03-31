using Microsoft.Win32;
using PointofSale.Data;
using PointofSale.Services;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PointofSale.Views
{
    public partial class DevPanelWindow : Window
    {
        public DevPanelWindow()
        {
            InitializeComponent();
            LoadAll();
        }

        private void LoadAll()
        {
            // Features
            FeatureCustomers.IsChecked = DevSettings.IsEnabled(DevSettings.Feature_Customers);
            FeatureInventory.IsChecked = DevSettings.IsEnabled(DevSettings.Feature_Inventory);
            FeatureReceiveStock.IsChecked = DevSettings.IsEnabled(DevSettings.Feature_ReceiveStock);
            FeatureSalesHistory.IsChecked = DevSettings.IsEnabled(DevSettings.Feature_SalesHistory);
            FeatureReports.IsChecked = DevSettings.IsEnabled(DevSettings.Feature_Reports);
            FeatureSuppliers.IsChecked = DevSettings.IsEnabled(DevSettings.Feature_Suppliers);
            FeatureDepartments.IsChecked = DevSettings.IsEnabled(DevSettings.Feature_Departments);
            FeatureEndOfDay.IsChecked = DevSettings.IsEnabled(DevSettings.Feature_EndOfDay);

            // Backup
            BackupIntervalBox.Text = DevSettings.Get(DevSettings.Backup_AutoIntervalHours, "24");
            var lastBackup = DevSettings.Get(DevSettings.Backup_LastBackupTime, "");
            LastBackupTxt.Text = string.IsNullOrWhiteSpace(lastBackup)
                ? "Last backup: Never"
                : $"Last backup: {lastBackup}";

            // Currency
            CurrencySymbolBox.Text = DevSettings.Get(DevSettings.Currency_Symbol, "R");
            CurrencyCodeBox.Text = DevSettings.Get(DevSettings.Currency_Code, "ZAR");
            var dp = DevSettings.Get(DevSettings.Currency_DecimalPlaces, "2");
            foreach (ComboBoxItem item in DecimalPlacesBox.Items)
                if (item.Content?.ToString() == dp)
                { DecimalPlacesBox.SelectedItem = item; break; }

            // Analytics
            AnalyticsEnabledCheck.IsChecked = DevSettings.IsEnabled(DevSettings.Analytics_Enabled);
            var clientId = DevSettings.Get(DevSettings.Analytics_ClientId, "");
            if (string.IsNullOrWhiteSpace(clientId))
            {
                clientId = Guid.NewGuid().ToString("N")[..12].ToUpper();
                DevSettings.Set(DevSettings.Analytics_ClientId, clientId);
                DevSettings.Set(DevSettings.Analytics_InstallDate,
                    DateTime.Now.ToString("yyyy-MM-dd"));
            }
            ClientIdBox.Text = clientId;
            ClientIdTxt.Text = $"Client ID: {clientId}";
            RefreshAnalyticsStats();

            // Remote
            RemoteEnabledCheck.IsChecked = DevSettings.IsEnabled(DevSettings.Remote_Enabled);
            RemoteUrlBox.Text = DevSettings.Get(DevSettings.Remote_ConfigUrl, "https://");
            var lastSync = DevSettings.Get(DevSettings.Remote_LastSync, "");
            RemoteLastSyncTxt.Text = string.IsNullOrWhiteSpace(lastSync)
                ? "Last sync: Never"
                : $"Last sync: {lastSync}";
        }

        // ── FEATURES ──────────────────────────────────────────────────────
        private void SaveFeatures()
        {
            DevSettings.SetEnabled(DevSettings.Feature_Customers, FeatureCustomers.IsChecked == true);
            DevSettings.SetEnabled(DevSettings.Feature_Inventory, FeatureInventory.IsChecked == true);
            DevSettings.SetEnabled(DevSettings.Feature_ReceiveStock, FeatureReceiveStock.IsChecked == true);
            DevSettings.SetEnabled(DevSettings.Feature_SalesHistory, FeatureSalesHistory.IsChecked == true);
            DevSettings.SetEnabled(DevSettings.Feature_Reports, FeatureReports.IsChecked == true);
            DevSettings.SetEnabled(DevSettings.Feature_Suppliers, FeatureSuppliers.IsChecked == true);
            DevSettings.SetEnabled(DevSettings.Feature_Departments, FeatureDepartments.IsChecked == true);
            DevSettings.SetEnabled(DevSettings.Feature_EndOfDay, FeatureEndOfDay.IsChecked == true);
        }

        // ── ADMIN ─────────────────────────────────────────────────────────
        private void CreateAdminBtn_Click(object sender, RoutedEventArgs e)
        {
            var fullName = AdminFullNameBox.Text.Trim();
            var username = AdminUsernameBox.Text.Trim();
            var password = AdminPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            { ShowStatus("Username and password are required.", error: true); return; }

            try
            {
                using var db = new AppDbContext();
                if (db.Users.Any(u => u.Username == username))
                { ShowStatus($"Username '{username}' already exists.", error: true); return; }

                var auth = new AuthService();
                auth.CreateUser(db, username, password, "Admin");
                var user = db.Users.First(u => u.Username == username);
                user.FullName = fullName;
                db.SaveChanges();

                AdminFullNameBox.Clear();
                AdminUsernameBox.Clear();
                AdminPasswordBox.Clear();
                ShowStatus($"Admin '{username}' created successfully.");
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}", error: true);
            }
        }

        private void SaveMasterCodeBtn_Click(object sender, RoutedEventArgs e)
        {
            var code = MasterCodeBox.Password;
            if (string.IsNullOrWhiteSpace(code))
            { ShowStatus("Master code cannot be empty.", error: true); return; }
            DevSettings.Set(DevSettings.Dev_MasterUnlockCode, code);
            MasterCodeBox.Clear();
            ShowStatus("Master unlock code saved.");
        }

        private void SaveDevCodeBtn_Click(object sender, RoutedEventArgs e)
        {
            var code = DevCodeBox.Password;
            if (string.IsNullOrWhiteSpace(code))
            { ShowStatus("Dev code cannot be empty.", error: true); return; }
            DevSettings.Set(DevSettings.Dev_SecretCode, code);
            DevCodeBox.Clear();
            ShowStatus("Dev secret code updated.");
        }

        // ── BACKUP ────────────────────────────────────────────────────────
        private void SaveBackupIntervalBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(BackupIntervalBox.Text.Trim(), out var hours) || hours < 1)
            { ShowStatus("Enter a valid number of hours (1 or more).", error: true); return; }
            DevSettings.Set(DevSettings.Backup_AutoIntervalHours, hours.ToString());
            ShowStatus($"Auto backup set to every {hours} hour(s).");
        }

        private void BackupNowBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save Backup",
                Filter = "SQLite Database|*.db",
                FileName = $"pos_backup_{DateTime.Now:yyyyMMdd_HHmm}.db",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var source = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PointofSale", "pos.db");
                File.Copy(source, dlg.FileName, overwrite: true);

                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                DevSettings.Set(DevSettings.Backup_LastBackupPath, dlg.FileName);
                DevSettings.Set(DevSettings.Backup_LastBackupTime, now);
                LastBackupTxt.Text = $"Last backup: {now}";
                BackupStatusTxt.Text = $"✔  Backup saved to: {dlg.FileName}";
                BackupStatusTxt.Visibility = Visibility.Visible;
                ShowStatus("Backup completed.");
            }
            catch (Exception ex) { ShowStatus($"Backup failed: {ex.Message}", error: true); }
        }

        private void RestoreBackupBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Title = "Select Backup File", Filter = "SQLite Database|*.db" };
            if (dlg.ShowDialog() != true) return;

            var confirm = MessageBox.Show(
                "Restoring will replace ALL current data with the backup.\n\nContinue?",
                "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var dest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PointofSale", "pos.db");
                File.Copy(dlg.FileName, dest, overwrite: true);
                MessageBox.Show("Restore complete. The application will now close.",
                    "Restored", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
            catch (Exception ex) { ShowStatus($"Restore failed: {ex.Message}", error: true); }
        }

        // ── CURRENCY ──────────────────────────────────────────────────────
        private void SaveCurrencyBtn_Click(object sender, RoutedEventArgs e)
        {
            DevSettings.Set(DevSettings.Currency_Symbol, CurrencySymbolBox.Text.Trim());
            DevSettings.Set(DevSettings.Currency_Code, CurrencyCodeBox.Text.Trim());
            DevSettings.Set(DevSettings.Currency_DecimalPlaces,
                (DecimalPlacesBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "2");
            ShowStatus("Currency settings saved.");
        }

        // ── ANALYTICS ─────────────────────────────────────────────────────
        private void RefreshStatsBtn_Click(object sender, RoutedEventArgs e)
            => RefreshAnalyticsStats();

        private void RefreshAnalyticsStats()
        {
            try
            {
                using var db = new AppDbContext();
                var saleCount = db.Sales.Count();
                var productCount = db.Products.Count();
                var customerCount = db.Customers.Count(c => c.IsActive);
                var installDate = DevSettings.Get(DevSettings.Analytics_InstallDate, "Unknown");

                AnalyticsStatsTxt.Text =
                    $"Install date:     {installDate}\n" +
                    $"Total sales:      {saleCount}\n" +
                    $"Products:         {productCount}\n" +
                    $"Active customers: {customerCount}\n" +
                    $"Last analytics ping: {DevSettings.Get(DevSettings.Analytics_LastPing, "Never")}";
            }
            catch { AnalyticsStatsTxt.Text = "Could not load stats."; }
        }

        // ── REMOTE CONFIG ─────────────────────────────────────────────────
        private void SaveRemoteBtn_Click(object sender, RoutedEventArgs e)
        {
            DevSettings.SetEnabled(DevSettings.Remote_Enabled, RemoteEnabledCheck.IsChecked == true);
            DevSettings.Set(DevSettings.Remote_ConfigUrl, RemoteUrlBox.Text.Trim());
            RemoteStatusTxt.Text = "✔  Remote config URL saved.";
            RemoteStatusTxt.Visibility = Visibility.Visible;
            ShowStatus("Remote config saved.");
        }

        private void SyncNowBtn_Click(object sender, RoutedEventArgs e)
        {
            RemoteStatusTxt.Text = "⚠  Remote sync not yet implemented.";
            RemoteStatusTxt.Foreground = System.Windows.Media.Brushes.Orange;
            RemoteStatusTxt.Visibility = Visibility.Visible;
        }

        // ── DANGER ZONE ───────────────────────────────────────────────────
        private void ResetDatabaseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmWithMasterCode("reset the database")) return;
            try
            {
                using var db = new AppDbContext();
                db.SaleItems.RemoveRange(db.SaleItems);
                db.Sales.RemoveRange(db.Sales);
                db.HeldReceiptItems.RemoveRange(db.HeldReceiptItems);
                db.HeldReceipts.RemoveRange(db.HeldReceipts);
                db.Products.RemoveRange(db.Products);
                db.Customers.RemoveRange(db.Customers);
                db.GiftCards.RemoveRange(db.GiftCards);
                db.SaveChanges();
                ShowStatus("Database reset. Sales, products, customers cleared.");
            }
            catch (Exception ex) { ShowStatus($"Reset failed: {ex.Message}", error: true); }
        }

        private void FactoryResetBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmWithMasterCode("perform a full FACTORY RESET")) return;

            var final = MessageBox.Show(
                "This will permanently delete EVERYTHING including all settings.\n\nAre you absolutely sure?",
                "Final Warning", MessageBoxButton.YesNo, MessageBoxImage.Stop);
            if (final != MessageBoxResult.Yes) return;

            try
            {
                var dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PointofSale", "pos.db");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (File.Exists(dbPath)) File.Delete(dbPath);
                MessageBox.Show("Factory reset complete. The application will now close.",
                    "Done", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
            catch (Exception ex) { ShowStatus($"Factory reset failed: {ex.Message}", error: true); }
        }

        // ── SAVE ALL ──────────────────────────────────────────────────────
        private void SaveAllBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFeatures();
            DevSettings.SetEnabled(DevSettings.Analytics_Enabled, AnalyticsEnabledCheck.IsChecked == true);
            DevSettings.SetEnabled(DevSettings.Remote_Enabled, RemoteEnabledCheck.IsChecked == true);
            ShowStatus("✔  All settings saved successfully.");
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        // ── Helpers ───────────────────────────────────────────────────────
        private bool ConfirmWithMasterCode(string action)
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Enter your master dev code to {action}:", "Master Code Required", "");
            if (string.IsNullOrWhiteSpace(input)) return false;
            if (!DevSettings.VerifyMasterCode(input))
            {
                MessageBox.Show("Incorrect master code.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Stop);
                return false;
            }
            return true;
        }

        private void ShowStatus(string msg, bool error = false)
        {
            StatusTxt.Text = msg;
            StatusTxt.Foreground = error
                ? System.Windows.Media.Brushes.OrangeRed
                : System.Windows.Media.Brushes.LightGreen;
            StatusTxt.Visibility = Visibility.Visible;
        }
    }
}