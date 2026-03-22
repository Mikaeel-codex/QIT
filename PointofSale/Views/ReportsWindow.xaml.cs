using Microsoft.EntityFrameworkCore;
using PointofSale.Data;
using PointofSale.Models;
using PointofSale.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PointofSale.Views
{
    public partial class ReportsWindow : Window
    {
        private string _activeReport = "SalesSummary";

        public ReportsWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                DateFrom.SelectedDate = DateTime.Today.AddDays(-30);
                DateTo.SelectedDate = DateTime.Today;
                RunReport();
            };
        }

        // ── Sidebar navigation ────────────────────────────────────────────
        private void NavBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            _activeReport = btn.Tag?.ToString() ?? "SalesSummary";
            UpdateSidebar();
            UpdateHeader();
            ShowPanel(_activeReport);
            RunReport();
        }

        private void UpdateSidebar()
        {
            BtnSalesSummary.Style = (Style)Resources[_activeReport == "SalesSummary" ? "ActiveSideBtn" : "SideBtn"];
            BtnProductSales.Style = (Style)Resources[_activeReport == "ProductSales" ? "ActiveSideBtn" : "SideBtn"];
            BtnInventory.Style = (Style)Resources[_activeReport == "Inventory" ? "ActiveSideBtn" : "SideBtn"];
            BtnProfitMargin.Style = (Style)Resources[_activeReport == "ProfitMargin" ? "ActiveSideBtn" : "SideBtn"];
        }

        private void UpdateHeader()
        {
            (ReportTitleTxt.Text, ReportSubtitleTxt.Text) = _activeReport switch
            {
                "SalesSummary" => ("Sales Summary", "Daily sales totals for the selected period"),
                "ProductSales" => ("Product Sales", "Which products sold the most in the selected period"),
                "Inventory" => ("Inventory Levels", "Current stock levels for all products"),
                "ProfitMargin" => ("Profit & Margin", "Estimated gross profit and margin per product"),
                _ => ("Report", "")
            };
        }

        private void ShowPanel(string report)
        {
            PanelSalesSummary.Visibility = report == "SalesSummary" ? Visibility.Visible : Visibility.Collapsed;
            PanelProductSales.Visibility = report == "ProductSales" ? Visibility.Visible : Visibility.Collapsed;
            PanelInventory.Visibility = report == "Inventory" ? Visibility.Visible : Visibility.Collapsed;
            PanelProfitMargin.Visibility = report == "ProfitMargin" ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Date filters ──────────────────────────────────────────────────
        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            var today = DateTime.Today;
            (DateFrom.SelectedDate, DateTo.SelectedDate) = btn.Tag?.ToString() switch
            {
                "Today" => (today, today),
                "ThisWeek" => (today.AddDays(-(int)today.DayOfWeek), today),
                "ThisMonth" => (new DateTime(today.Year, today.Month, 1), today),
                "LastMonth" => (new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                                new DateTime(today.Year, today.Month, 1).AddDays(-1)),
                _ => (DateFrom.SelectedDate, DateTo.SelectedDate)
            };
            RunReport();
        }

        private void DateFilter_Changed(object sender, EventArgs e) { /* manual run via button */ }

        private void RunReport_Click(object sender, RoutedEventArgs e) => RunReport();

        private void RunReport()
        {
            try
            {
                var from = DateFrom.SelectedDate ?? DateTime.Today.AddDays(-30);
                var to = (DateTo.SelectedDate ?? DateTime.Today).AddDays(1); // inclusive

                switch (_activeReport)
                {
                    case "SalesSummary": RunSalesSummary(from, to); break;
                    case "ProductSales": RunProductSales(from, to); break;
                    case "Inventory": RunInventory(); break;
                    case "ProfitMargin": RunProfitMargin(from, to); break;
                }
            }
            catch (Exception ex)
            {
                StatusTxt.Text = $"Error: {ex.Message}";
            }
        }

        // ── Sales Summary ─────────────────────────────────────────────────
        private void RunSalesSummary(DateTime from, DateTime to)
        {
            using var db = new AppDbContext();

            var sales = db.Sales
                .Include(s => s.Items)
                .Where(s => s.SaleDate >= from && s.SaleDate < to && s.Status != "Voided")
                .ToList();

            // KPIs
            var totalRevenue = sales.Sum(s => s.Total);
            var totalTax = sales.Sum(s => s.Tax);
            var count = sales.Count;
            var avg = count > 0 ? totalRevenue / count : 0;

            KpiRevenue.Text = $"R {totalRevenue:N2}";
            KpiSalesCount.Text = count.ToString();
            KpiAvgSale.Text = $"R {avg:N2}";
            KpiTax.Text = $"R {totalTax:N2}";

            // Group by day
            var rows = sales
                .GroupBy(s => s.SaleDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new SalesSummaryRow
                {
                    Date = g.Key.ToString("dd MMM yyyy"),
                    SalesCount = g.Count(),
                    ItemsSold = g.Sum(s => s.Items.Sum(i => i.Quantity)),
                    Subtotal = g.Sum(s => s.Subtotal),
                    Tax = g.Sum(s => s.Tax),
                    Total = g.Sum(s => s.Total),
                })
                .ToList();

            GridSalesSummary.ItemsSource = rows;
            StatusTxt.Text = $"{count} sales  •  Total: R{totalRevenue:N2}  •  Period: {from:dd MMM} – {to.AddDays(-1):dd MMM yyyy}";
        }

        // ── Product Sales ─────────────────────────────────────────────────
        private void RunProductSales(DateTime from, DateTime to)
        {
            using var db = new AppDbContext();

            var items = db.SaleItems
                .Include(i => i.Sale)
                .Where(i => i.Sale.SaleDate >= from && i.Sale.SaleDate < to && i.Sale.Status != "Voided")
                .ToList();

            var rows = items
                .GroupBy(i => new { i.ProductId, i.ProductName, i.SKU })
                .OrderByDescending(g => g.Sum(i => i.Quantity))
                .Select(g => new ProductSalesRow
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    SKU = g.Key.SKU,
                    QtySold = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.LineTotal),
                    AvgPrice = g.Sum(i => i.Quantity) > 0
                                  ? g.Sum(i => i.LineTotal) / g.Sum(i => i.Quantity)
                                  : 0,
                })
                .ToList();

            GridProductSales.ItemsSource = rows;
            StatusTxt.Text = $"{rows.Count} products sold  •  Period: {from:dd MMM} – {to.AddDays(-1):dd MMM yyyy}";
        }

        // ── Inventory Levels ──────────────────────────────────────────────
        private void RunInventory()
        {
            using var db = new AppDbContext();

            var products = db.Products.OrderBy(p => p.Name).ToList();

            var rows = products.Select(p => new InventoryRow
            {
                Id = p.Id,
                Name = p.Name,
                Department = p.Department ?? "",
                SKU = p.SKU ?? "",
                StockQty = p.StockQty,
                ReorderPoint = p.ReorderPoint,
                CostPrice = p.CostPrice,
                Price = p.Price,
                IsOutOfStock = p.IsOutOfStock,
                IsLowStock = p.IsLowStock && !p.IsOutOfStock,
                StockStatus = p.IsOutOfStock ? "Out of Stock"
                             : p.IsLowStock ? "Low Stock"
                             : "OK",
            }).ToList();

            GridInventory.ItemsSource = rows;
            StatusTxt.Text = $"{rows.Count} products  •  {rows.Count(r => r.IsOutOfStock)} out of stock  •  {rows.Count(r => r.IsLowStock)} low stock";
        }

        // ── Profit & Margin ───────────────────────────────────────────────
        private void RunProfitMargin(DateTime from, DateTime to)
        {
            using var db = new AppDbContext();

            var items = db.SaleItems
                .Include(i => i.Sale)
                .Where(i => i.Sale.SaleDate >= from && i.Sale.SaleDate < to && i.Sale.Status != "Voided")
                .ToList();

            // Get cost prices from products table
            var productCosts = db.Products.ToDictionary(p => p.Id, p => p.CostPrice);

            var rows = items
                .GroupBy(i => new { i.ProductId, i.ProductName })
                .OrderByDescending(g => g.Sum(i => i.LineTotal))
                .Select(g =>
                {
                    var revenue = g.Sum(i => i.LineTotal);
                    var qtySold = g.Sum(i => i.Quantity);
                    var sellPrice = qtySold > 0 ? revenue / qtySold : 0;
                    productCosts.TryGetValue(g.Key.ProductId, out var costPrice);
                    var cogs = costPrice * qtySold;
                    var grossProfit = revenue - cogs;
                    var marginPct = revenue > 0 ? (grossProfit / revenue) * 100 : 0;

                    return new ProfitMarginRow
                    {
                        ProductName = g.Key.ProductName,
                        QtySold = qtySold,
                        SellPrice = sellPrice,
                        CostPrice = costPrice,
                        Revenue = revenue,
                        GrossProfit = grossProfit,
                        MarginPct = marginPct,
                    };
                })
                .ToList();

            var totalRevenue = rows.Sum(r => r.Revenue);
            var totalProfit = rows.Sum(r => r.GrossProfit);
            var avgMargin = totalRevenue > 0 ? (totalProfit / totalRevenue) * 100 : 0;

            KpiProfitRevenue.Text = $"R {totalRevenue:N2}";
            KpiGrossProfit.Text = $"R {totalProfit:N2}";
            KpiAvgMargin.Text = $"{avgMargin:N1}%";

            // Colour the profit KPIs
            KpiGrossProfit.Foreground = totalProfit >= 0
                ? System.Windows.Media.Brushes.LightGreen
                : System.Windows.Media.Brushes.Salmon;
            KpiAvgMargin.Foreground = avgMargin >= 0
                ? System.Windows.Media.Brushes.LightGreen
                : System.Windows.Media.Brushes.Salmon;

            GridProfitMargin.ItemsSource = rows;
            StatusTxt.Text = $"{rows.Count} products  •  Revenue: R{totalRevenue:N2}  •  Est. Profit: R{totalProfit:N2}  •  Period: {from:dd MMM} – {to.AddDays(-1):dd MMM yyyy}";
        }

        // ── PDF Export ────────────────────────────────────────────────────
        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var from = DateFrom.SelectedDate ?? DateTime.Today.AddDays(-30);
                var to = DateTo.SelectedDate ?? DateTime.Today;

                var storeName = StoreSettingsService.Get("StoreName", "My Store");
                var csv = BuildCsvForReport();

                if (string.IsNullOrWhiteSpace(csv))
                {
                    MessageBox.Show("No data to export. Run the report first.");
                    return;
                }

                var outputDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "PointOfSale", "Reports");
                Directory.CreateDirectory(outputDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
                var reportSlug = _activeReport.ToLower();
                var pdfPath = Path.Combine(outputDir, $"{reportSlug}_{timestamp}.pdf");
                var csvPath = Path.Combine(Path.GetTempPath(), $"report_{timestamp}.csv");
                var pyScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "generate_report.py");

                File.WriteAllText(csvPath, csv, Encoding.UTF8);

                var title = ReportTitleTxt.Text;
                var period = $"{from:dd MMM yyyy} – {to:dd MMM yyyy}";

                var args = $"\"{pyScript}\" \"{csvPath}\" \"{pdfPath}\" \"{storeName}\" \"{title}\" \"{period}\"";

                var psi = new ProcessStartInfo("python", args)
                {
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                };

                var proc = Process.Start(psi);
                var err = proc?.StandardError.ReadToEnd();
                proc?.WaitForExit();

                if (proc?.ExitCode != 0 || !File.Exists(pdfPath))
                {
                    MessageBox.Show($"PDF generation failed:\n{err}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusTxt.Text = $"PDF exported: {pdfPath}";

                // Open the output folder
                Process.Start("explorer.exe", $"/select,\"{pdfPath}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildCsvForReport()
        {
            var sb = new StringBuilder();

            switch (_activeReport)
            {
                case "SalesSummary":
                    if (GridSalesSummary.ItemsSource is not IEnumerable<SalesSummaryRow> ss) return "";
                    sb.AppendLine("Date,No. of Sales,Items Sold,Subtotal,Tax,Total");
                    foreach (var r in ss)
                        sb.AppendLine($"{r.Date},{r.SalesCount},{r.ItemsSold},{r.Subtotal:N2},{r.Tax:N2},{r.Total:N2}");
                    break;

                case "ProductSales":
                    if (GridProductSales.ItemsSource is not IEnumerable<ProductSalesRow> ps) return "";
                    sb.AppendLine("Item #,Product Name,SKU,Qty Sold,Revenue,Avg Price");
                    foreach (var r in ps)
                        sb.AppendLine($"{r.ProductId},{r.ProductName},{r.SKU},{r.QtySold},{r.Revenue:N2},{r.AvgPrice:N2}");
                    break;

                case "Inventory":
                    if (GridInventory.ItemsSource is not IEnumerable<InventoryRow> inv) return "";
                    sb.AppendLine("Item #,Product Name,Department,SKU,On Hand,Reorder Pt.,Cost Price,Reg Price,Status");
                    foreach (var r in inv)
                        sb.AppendLine($"{r.Id},{r.Name},{r.Department},{r.SKU},{r.StockQty},{r.ReorderPoint},{r.CostPrice:N2},{r.Price:N2},{r.StockStatus}");
                    break;

                case "ProfitMargin":
                    if (GridProfitMargin.ItemsSource is not IEnumerable<ProfitMarginRow> pm) return "";
                    sb.AppendLine("Product Name,Qty Sold,Sell Price,Cost Price,Revenue,Est. Profit,Margin %");
                    foreach (var r in pm)
                        sb.AppendLine($"{r.ProductName},{r.QtySold},{r.SellPrice:N2},{r.CostPrice:N2},{r.Revenue:N2},{r.GrossProfit:N2},{r.MarginPct:N1}%");
                    break;
            }

            return sb.ToString();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }

    // ── Row models ────────────────────────────────────────────────────────

    public class SalesSummaryRow
    {
        public string Date { get; set; } = "";
        public int SalesCount { get; set; }
        public int ItemsSold { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }

    public class ProductSalesRow
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string SKU { get; set; } = "";
        public int QtySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal AvgPrice { get; set; }
    }

    public class InventoryRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Department { get; set; } = "";
        public string SKU { get; set; } = "";
        public int StockQty { get; set; }
        public int ReorderPoint { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public bool IsOutOfStock { get; set; }
        public bool IsLowStock { get; set; }
        public string StockStatus { get; set; } = "";
    }

    public class ProfitMarginRow
    {
        public string ProductName { get; set; } = "";
        public int QtySold { get; set; }
        public decimal SellPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Revenue { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal MarginPct { get; set; }
    }
}