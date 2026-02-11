using PointofSale.Models;
using System.Text;

namespace PointofSale.Helpers
{
    public static class ReceiptTextBuilder
    {
        public static string Build(int saleId, string cashier, decimal total, System.Collections.Generic.IEnumerable<CartLine> cart)
        {
            var sb = new StringBuilder();

            sb.AppendLine("===== RECEIPT =====");
            sb.AppendLine($"Sale #{saleId}");
            sb.AppendLine($"Cashier: {cashier}");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine("-------------------");

            foreach (var line in cart)
            {
                sb.AppendLine($"{line.Name}");
                sb.AppendLine($"{line.SKU} | R {line.UnitPrice:0.00} x {line.Qty} = R {line.LineTotal:0.00}");
                sb.AppendLine();
            }

            sb.AppendLine("-------------------");
            sb.AppendLine($"TOTAL: R {total:0.00}");
            sb.AppendLine("Thank you for shopping with us!💛");

            return sb.ToString();
        }
    }
}
