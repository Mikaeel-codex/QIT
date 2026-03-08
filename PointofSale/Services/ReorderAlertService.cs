using PointofSale.Data;
using System;
using System.Linq;
using System.Text;
using System.Windows;

namespace PointofSale.Services
{
    public static class ReorderAlertService
    {
        /// <summary>
        /// Shows a popup listing products where StockQty <= ReorderPoint (and ReorderPoint > 0).
        /// Call this after sales / when opening POS / when opening inventory.
        /// </summary>
        public static void ShowLowStockPopup(Window? owner = null)
        {
            try
            {
                using var db = new AppDbContext();

                var low = db.Products
                    .Where(p => p.ReorderPoint > 0 && p.StockQty <= p.ReorderPoint)
                    .OrderBy(p => p.Name)
                    .Select(p => new
                    {
                        p.Name,
                        p.SKU,
                        p.StockQty,
                        p.ReorderPoint
                    })
                    .ToList();

                if (low.Count == 0)
                    return;

                var sb = new StringBuilder();
                sb.AppendLine("Low Stock Warning");
                sb.AppendLine("-------------------------");
                sb.AppendLine($"Items below (or equal to) reorder point: {low.Count}");
                sb.AppendLine();

                foreach (var p in low.Take(25))
                {
                    sb.AppendLine($"• {p.Name} ({p.SKU})  Stock: {p.StockQty}  Reorder: {p.ReorderPoint}");
                }

                if (low.Count > 25)
                {
                    sb.AppendLine();
                    sb.AppendLine($"...and {low.Count - 25} more.");
                }

                if (owner != null)
                    MessageBox.Show(owner, sb.ToString(), "Low Stock Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    MessageBox.Show(sb.ToString(), "Low Stock Alert", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                // Don't crash the app if something goes wrong
                MessageBox.Show(ex.Message, "Low Stock Alert Error");
            }
        }
    }
}