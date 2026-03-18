using PointofSale.Data;
using PointofSale.Models;
using System.Linq;

namespace PointofSale.Services
{
    /// <summary>
    /// Simple helper to read and write key-value settings from the StoreSettings table.
    /// Usage:
    ///   var name = StoreSettingsService.Get("StoreName");
    ///   StoreSettingsService.Set("StoreName", "My Shop");
    /// </summary>
    public static class StoreSettingsService
    {
        public static string Get(string key, string defaultValue = "")
        {
            using var db = new AppDbContext();
            return db.StoreSettings.FirstOrDefault(s => s.Key == key)?.Value ?? defaultValue;
        }

        public static void Set(string key, string value)
        {
            using var db = new AppDbContext();
            var existing = db.StoreSettings.FirstOrDefault(s => s.Key == key);
            if (existing != null)
                existing.Value = value;
            else
                db.StoreSettings.Add(new StoreSetting { Key = key, Value = value });
            db.SaveChanges();
        }
    }
}