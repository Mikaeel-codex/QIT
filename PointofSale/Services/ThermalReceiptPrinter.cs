using PointofSale.Models;
using PointofSale.Services;
using System;
using System.Collections.Generic;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PointofSale.Services
{
    /// <summary>
    /// Prints or previews a thermal-style receipt (80mm paper) using WPF's
    /// built-in PrintDialog and FlowDocumentScrollViewer — no external libraries.
    /// </summary>
    public static class ThermalReceiptPrinter
    {
        private const double PageWidth = 302; // 80mm ≈ 302 device-independent units

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Shows the Windows print dialog then prints immediately.</summary>
        public static bool PrintReceipt(ReceiptData receipt, Window owner)
        {
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() != true) return false;

            var doc = BuildDocument(receipt);
            doc.PageWidth = PageWidth;
            doc.PagePadding = new Thickness(8, 12, 8, 12);
            doc.ColumnWidth = PageWidth;

            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
            paginator.PageSize = new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);

            dlg.PrintDocument(paginator, $"Receipt {receipt.ReceiptNumber}");
            return true;
        }

        /// <summary>
        /// Opens a preview window showing the receipt content.
        /// User can then click Print to send to printer.
        /// </summary>
        public static void PreviewReceipt(ReceiptData receipt, Window owner)
        {
            // Build a fresh document for display — FlowDocumentScrollViewer
            // renders FlowDocument natively with no conversion needed.
            var doc = BuildDocument(receipt);
            doc.PagePadding = new Thickness(16, 16, 16, 16);
            doc.ColumnWidth = double.NaN; // auto-fit to viewer width
            doc.TextAlignment = TextAlignment.Left;

            var viewer = new FlowDocumentScrollViewer
            {
                Document = doc,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = Brushes.White,
                Foreground = Brushes.Black,
                IsToolBarVisible = false,
                Zoom = 120,
            };

            // ── Preview window ────────────────────────────────────────────
            var preview = new Window
            {
                Title = $"Receipt Preview — {receipt.ReceiptNumber}",
                Width = 420,
                Height = 660,
                MinWidth = 340,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
                ResizeMode = ResizeMode.CanResize,
            };

            var outerGrid = new Grid();
            outerGrid.RowDefinitions.Add(new RowDefinition
            { Height = new GridLength(1, GridUnitType.Star) });
            outerGrid.RowDefinitions.Add(new RowDefinition
            { Height = GridLength.Auto });

            // Receipt paper card — white with shadow feel
            var paper = new Border
            {
                Background = Brushes.White,
                Margin = new Thickness(16, 16, 16, 8),
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(4),
            };
            paper.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.18,
                BlurRadius = 8,
                ShadowDepth = 2,
                Direction = 270,
            };
            paper.Child = viewer;
            Grid.SetRow(paper, 0);
            outerGrid.Children.Add(paper);

            // Button row
            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14),
            };
            Grid.SetRow(btnRow, 1);

            var printBtn = new Button
            {
                Content = "🖨   Print",
                Width = 120,
                Height = 34,
                Margin = new Thickness(0, 0, 12, 0),
                Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x7D, 0x44)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand,
            };
            printBtn.Click += (s, ev) =>
            {
                var dlg = new PrintDialog();
                if (dlg.ShowDialog() == true)
                {
                    // Build a fresh doc for printing with correct page settings
                    var printDoc = BuildDocument(receipt);
                    printDoc.PageWidth = PageWidth;
                    printDoc.PagePadding = new Thickness(8, 12, 8, 12);
                    printDoc.ColumnWidth = PageWidth;

                    var paginator = ((IDocumentPaginatorSource)printDoc).DocumentPaginator;
                    paginator.PageSize = new Size(
                        dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);
                    dlg.PrintDocument(paginator, $"Receipt {receipt.ReceiptNumber}");
                    preview.Close();
                }
            };

            var closeBtn = new Button
            {
                Content = "Close",
                Width = 80,
                Height = 34,
                Background = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand,
            };
            closeBtn.Click += (s, ev) => preview.Close();

            btnRow.Children.Add(printBtn);
            btnRow.Children.Add(closeBtn);
            outerGrid.Children.Add(btnRow);

            preview.Content = outerGrid;
            preview.ShowDialog();
        }

        // ── Document builder ──────────────────────────────────────────────

        private static FlowDocument BuildDocument(ReceiptData r)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Courier New"),
                FontSize = 9,
                Foreground = Brushes.Black,
                Background = Brushes.White,
            };

            // ── STORE NAME ────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(r.StoreName))
                doc.Blocks.Add(CentredPara(r.StoreName, 13, bold: true));

            if (!string.IsNullOrWhiteSpace(r.StoreAddress))
                doc.Blocks.Add(CentredPara(r.StoreAddress, 8));

            if (!string.IsNullOrWhiteSpace(r.StorePhone))
                doc.Blocks.Add(CentredPara($"Tel: {r.StorePhone}", 8));

            if (!string.IsNullOrWhiteSpace(r.StoreEmail))
                doc.Blocks.Add(CentredPara(r.StoreEmail, 8));

            doc.Blocks.Add(Divider());

            // ── RECEIPT META ──────────────────────────────────────────────
            doc.Blocks.Add(TwoCol("Receipt #:", r.ReceiptNumber));
            doc.Blocks.Add(TwoCol("Date:", r.SaleDate.ToString("dd MMM yyyy  HH:mm")));
            doc.Blocks.Add(TwoCol("Cashier:", r.Cashier));

            if (!string.IsNullOrWhiteSpace(r.CustomerName))
                doc.Blocks.Add(TwoCol("Customer:", r.CustomerName));

            doc.Blocks.Add(Divider());

            // ── COLUMN HEADERS ────────────────────────────────────────────
            doc.Blocks.Add(ItemHeader());
            doc.Blocks.Add(ThinDivider());

            // ── LINE ITEMS ────────────────────────────────────────────────
            foreach (var line in r.Lines)
            {
                doc.Blocks.Add(ItemLine(line));
                if (line.DiscountPct > 0)
                    doc.Blocks.Add(DiscountLine(line.DiscountPct));
            }

            doc.Blocks.Add(Divider());

            // ── TOTALS ────────────────────────────────────────────────────
            doc.Blocks.Add(TwoCol("Subtotal:", $"R {r.Subtotal:N2}"));
            doc.Blocks.Add(TwoCol("Tax:", $"R {r.Tax:N2}"));
            doc.Blocks.Add(BoldTwoCol("TOTAL:", $"R {r.Total:N2}", 11));

            doc.Blocks.Add(ThinDivider());

            // ── PAYMENTS ──────────────────────────────────────────────────
            foreach (var p in r.Payments)
                doc.Blocks.Add(TwoCol(p.Label + ":", $"R {p.Amount:N2}"));

            if (r.CashChange > 0)
                doc.Blocks.Add(TwoCol("Change:", $"R {r.CashChange:N2}"));

            doc.Blocks.Add(Divider());

            // ── FOOTER ────────────────────────────────────────────────────
            var footer = string.IsNullOrWhiteSpace(r.ReceiptFooter)
                ? StoreSettingsService.Get("ReceiptFooter", "Thank you for your business!")
                : r.ReceiptFooter;

            if (!string.IsNullOrWhiteSpace(footer))
            {
                doc.Blocks.Add(new Paragraph(new Run(""))
                { Margin = new Thickness(0, 4, 0, 4) });
                doc.Blocks.Add(CentredPara(footer, 8));
            }

            doc.Blocks.Add(CentredPara(
                r.SaleDate.ToString("yyyy-MM-dd HH:mm:ss"), 7,
                foreground: Brushes.Gray));

            return doc;
        }

        // ── Block helpers ─────────────────────────────────────────────────

        private static Paragraph CentredPara(string text, double size = 9,
            bool bold = false, Brush? foreground = null) =>
            new(new Run(text)
            {
                FontSize = size,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Foreground = foreground ?? Brushes.Black,
            })
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 1, 0, 1),
            };

        private static Paragraph Divider() =>
            new(new Run(new string('─', 38)))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 3, 0, 3),
                FontSize = 8,
            };

        private static Paragraph ThinDivider() =>
            new(new Run(new string('-', 38)))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 2, 0, 2),
                FontSize = 8,
            };

        private static Paragraph TwoCol(string label, string value,
            bool bold = false, double size = 9)
        {
            var p = new Paragraph { Margin = new Thickness(0, 1, 0, 1), FontSize = size };
            var padding = new string(' ',
                Math.Max(1, 36 - label.Length - value.Length));
            p.Inlines.Add(new Run(label)
            { FontWeight = bold ? FontWeights.Bold : FontWeights.Normal });
            p.Inlines.Add(new Run(padding + value)
            { FontWeight = bold ? FontWeights.Bold : FontWeights.Normal });
            return p;
        }

        private static Paragraph BoldTwoCol(string label, string value,
            double size = 11) => TwoCol(label, value, bold: true, size: size);

        private static Paragraph ItemHeader() =>
            new(new Run(
                PadRight("Item", 20) +
                PadLeft("Qty", 4) +
                PadLeft("Price", 7) +
                PadLeft("Total", 7)))
            {
                Margin = new Thickness(0, 1, 0, 1),
                FontSize = 8,
                FontWeight = FontWeights.Bold,
            };

        private static Paragraph ItemLine(ReceiptLineItem line)
        {
            var p = new Paragraph { Margin = new Thickness(0, 1, 0, 1), FontSize = 8 };
            var name = line.Name.Length > 20 ? line.Name[..20] : line.Name;

            p.Inlines.Add(new Run(
                PadRight(name, 20) +
                PadLeft(line.Qty.ToString(), 4) +
                PadLeft($"{line.UnitPrice:N2}", 7) +
                PadLeft($"{line.LineTotal:N2}", 7)));

            var detail = new List<string>();
            if (!string.IsNullOrWhiteSpace(line.Size)) detail.Add(line.Size);
            if (!string.IsNullOrWhiteSpace(line.Attribute)) detail.Add(line.Attribute);
            if (!string.IsNullOrWhiteSpace(line.SKU)) detail.Add($"SKU:{line.SKU}");

            if (detail.Count > 0)
            {
                p.Inlines.Add(new LineBreak());
                p.Inlines.Add(new Run("  " + string.Join(" | ", detail))
                { Foreground = Brushes.Gray, FontSize = 7 });
            }
            return p;
        }

        private static Paragraph DiscountLine(decimal pct)
        {
            var p = new Paragraph { Margin = new Thickness(0, 0, 0, 1), FontSize = 7 };
            p.Inlines.Add(new Run($"  ** {pct:N0}% discount applied **")
            { Foreground = Brushes.Gray });
            return p;
        }

        private static string PadRight(string s, int width)
            => s.Length >= width ? s[..width] : s + new string(' ', width - s.Length);

        private static string PadLeft(string s, int width)
            => s.Length >= width ? s[..width] : new string(' ', width - s.Length) + s;
    }
}