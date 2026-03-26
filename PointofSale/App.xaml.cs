using Microsoft.EntityFrameworkCore;
using PointofSale.Data;
using PointofSale.Services;
using System.Windows;

namespace QuickInventoryTill
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using var db = new AppDbContext();
            db.Database.Migrate();

            // Apply the user's saved theme before any window opens
            ThemeService.ApplySaved();
        }
    }
}