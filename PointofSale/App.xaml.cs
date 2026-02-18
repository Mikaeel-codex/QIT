using Microsoft.EntityFrameworkCore;
using PointofSale.Data; 
using System.Windows;

namespace QuickInventoryTill
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using var db = new AppDbContext();
            db.Database.Migrate(); // creates missing tables like Suppliers automatically
        }
    }
}
