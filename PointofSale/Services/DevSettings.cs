namespace PointofSale.Services
{
    /// <summary>
    /// Central registry of all developer-controlled feature flag keys.
    /// Values stored in StoreSettings table — never exposed to clients.
    /// </summary>
    public static class DevSettings
    {
        // ── Feature flags ─────────────────────────────────────────────────
        // Customers flag controls BOTH the customers module AND account payments.
        public const string Feature_Customers = "Dev_Feature_Customers";
        public const string Feature_Reports = "Dev_Feature_Reports";
        public const string Feature_Suppliers = "Dev_Feature_Suppliers";
        public const string Feature_Departments = "Dev_Feature_Departments";
        public const string Feature_EndOfDay = "Dev_Feature_EndOfDay";
        public const string Feature_ReceiveStock = "Dev_Feature_ReceiveStock";
        public const string Feature_SalesHistory = "Dev_Feature_SalesHistory";
        public const string Feature_Inventory = "Dev_Feature_Inventory";

        // ── Backup ────────────────────────────────────────────────────────
        public const string Backup_AutoIntervalHours = "Dev_Backup_AutoIntervalHours";
        public const string Backup_LastBackupPath = "Dev_Backup_LastBackupPath";
        public const string Backup_LastBackupTime = "Dev_Backup_LastBackupTime";

        // ── Currency ──────────────────────────────────────────────────────
        public const string Currency_Symbol = "Dev_Currency_Symbol";
        public const string Currency_Code = "Dev_Currency_Code";
        public const string Currency_DecimalPlaces = "Dev_Currency_DecimalPlaces";

        // ── Analytics / billing ───────────────────────────────────────────
        public const string Analytics_ClientId = "Dev_Analytics_ClientId";
        public const string Analytics_Enabled = "Dev_Analytics_Enabled";
        public const string Analytics_LastPing = "Dev_Analytics_LastPing";
        public const string Analytics_SaleCount = "Dev_Analytics_SaleCount";
        public const string Analytics_InstallDate = "Dev_Analytics_InstallDate";

        // ── Remote config ─────────────────────────────────────────────────
        public const string Remote_ConfigUrl = "Dev_Remote_ConfigUrl";
        public const string Remote_LastSync = "Dev_Remote_LastSync";
        public const string Remote_Enabled = "Dev_Remote_Enabled";

        // ── Dev auth ──────────────────────────────────────────────────────
        public const string Dev_SecretCode = "Dev_SecretCode";
        public const string Dev_MasterUnlockCode = "Dev_MasterUnlockCode";

        // ── Helpers ───────────────────────────────────────────────────────
        public static bool IsEnabled(string key) =>
            StoreSettingsService.Get(key, "true").ToLower() == "true";

        public static void SetEnabled(string key, bool value) =>
            StoreSettingsService.Set(key, value ? "true" : "false");

        public static string Get(string key, string defaultValue = "") =>
            StoreSettingsService.Get(key, defaultValue);

        public static void Set(string key, string value) =>
            StoreSettingsService.Set(key, value);

        public static bool VerifyDevCode(string entered) =>
            entered == StoreSettingsService.Get(Dev_SecretCode, "DEV2024");

        public static bool VerifyMasterCode(string entered) =>
            entered == StoreSettingsService.Get(Dev_MasterUnlockCode, "MASTER9999");

        // ── Convenience properties ────────────────────────────────────────
        public static bool CustomersEnabled => IsEnabled(Feature_Customers);
        public static bool ReportsEnabled => IsEnabled(Feature_Reports);
        public static bool SuppliersEnabled => IsEnabled(Feature_Suppliers);
        public static bool DepartmentsEnabled => IsEnabled(Feature_Departments);
        public static bool EndOfDayEnabled => IsEnabled(Feature_EndOfDay);
        public static bool ReceiveStockEnabled => IsEnabled(Feature_ReceiveStock);
        public static bool SalesHistoryEnabled => IsEnabled(Feature_SalesHistory);
        public static bool InventoryEnabled => IsEnabled(Feature_Inventory);
    }
}