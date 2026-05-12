using PointofSale.Models;
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
    /// Prints a quotation document with a prominent QUOTATION — NOT A TAX INVOICE watermark.
    /// Uses WPF FlowDocument — no external libraries needed.
    /// </summary>
    public static class QuotePrinter
    {
        public static bool PrintQuote(QuoteData quote, Window owner)
        {
            var dlg = new System.Windows.Controls.PrintDialog();
            if (dlg.ShowDialog() != true) return false;

            var doc = BuildDocument(quote);
            doc.PageWidth = dlg.PrintableAreaWidth;
            doc.PagePadding = new Thickness(60, 50, 60, 50);
            doc.ColumnWidth = dlg.PrintableAreaWidth;

            var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
            paginator.PageSize = new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);

            dlg.PrintDocument(paginator, $"Quote {quote.QuoteNumber}");
            return true;
        }

        public static void PreviewQuote(QuoteData quote, Window owner)
        {
            var doc = BuildDocument(quote);
            doc.PagePadding = new Thickness(40, 30, 40, 30);
            doc.ColumnWidth = double.NaN;

            var viewer = new FlowDocumentScrollViewer
            {
                Document = doc,
                Background = Brushes.White,
                Foreground = Brushes.Black,
                IsToolBarVisible = false,
                Zoom = 100,
            };

            var preview = new Window
            {
                Title = $"Quote Preview — {quote.QuoteNumber}",
                Width = 750,
                Height = 900,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var paper = new Border
            {
                Background = Brushes.White,
                Margin = new Thickness(16),
                CornerRadius = new CornerRadius(4),
            };
            paper.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                Opacity = 0.15,
                BlurRadius = 8,
                ShadowDepth = 2
            };
            paper.Child = viewer;
            Grid.SetRow(paper, 0);
            grid.Children.Add(paper);

            var btnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 14),
            };
            Grid.SetRow(btnRow, 1);

            var printBtn = new Button
            {
                Content = "🖨  Print",
                Width = 120,
                Height = 34,
                Margin = new Thickness(0, 0, 12, 0),
                Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x7D, 0x44)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
            };
            printBtn.Click += (s, ev) =>
            {
                var dlg = new System.Windows.Controls.PrintDialog();
                if (dlg.ShowDialog() == true)
                {
                    var doc2 = BuildDocument(quote);
                    doc2.PageWidth = dlg.PrintableAreaWidth;
                    doc2.PagePadding = new Thickness(60, 50, 60, 50);
                    doc2.ColumnWidth = dlg.PrintableAreaWidth;
                    var paginator = ((IDocumentPaginatorSource)doc2).DocumentPaginator;
                    paginator.PageSize = new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);
                    dlg.PrintDocument(paginator, $"Quote {quote.QuoteNumber}");
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
            grid.Children.Add(btnRow);
            preview.Content = grid;
            preview.ShowDialog();
        }

        // ── Document builder ──────────────────────────────────────────────
        private static FlowDocument BuildDocument(QuoteData q)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                Foreground = Brushes.Black,
                Background = Brushes.White,
            };

            // ── WATERMARK (large background text) ────────────────────────
            // We overlay it as a centred paragraph with low opacity
            var watermark = new Paragraph(new Run("QUOTATION\nNOT A TAX INVOICE"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0),
                Padding = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromArgb(30, 180, 0, 0)),
                FontSize = 52,
                FontWeight = FontWeights.Bold,
                LineHeight = 60,
            };
            doc.Blocks.Add(watermark);

            // ── STORE HEADER ──────────────────────────────────────────────
            doc.Blocks.Add(CentredPara(q.StoreName, 18, bold: true));
            if (!string.IsNullOrWhiteSpace(q.StoreAddress))
                doc.Blocks.Add(CentredPara(q.StoreAddress, 10));
            if (!string.IsNullOrWhiteSpace(q.StorePhone))
                doc.Blocks.Add(CentredPara($"Tel: {q.StorePhone}", 10));

            doc.Blocks.Add(HRule());

            // ── QUOTATION HEADER ──────────────────────────────────────────
            doc.Blocks.Add(CentredPara("QUOTATION — NOT A TAX INVOICE", 14,
                bold: true, foreground: Brushes.DarkRed));
            doc.Blocks.Add(new Paragraph(new Run("")) { Margin = new Thickness(0, 4, 0, 4) });

            // Quote meta
            var meta = new Table { CellSpacing = 0 };
            meta.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            meta.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var metaRg = new TableRowGroup();

            metaRg.Rows.Add(TableRow2("Quote No:", q.QuoteNumber,
                "Date:", q.CreatedAt.ToString("dd MMM yyyy")));
            metaRg.Rows.Add(TableRow2("Prepared by:", q.CreatedBy,
                "Valid Until:", q.ExpiresAt.ToString("dd MMM yyyy")));

            if (!string.IsNullOrWhiteSpace(q.CustomerName))
            {
                metaRg.Rows.Add(TableRow2("Customer:", q.CustomerName,
                    "Phone:", q.CustomerPhone));
                if (!string.IsNullOrWhiteSpace(q.CustomerEmail))
                    metaRg.Rows.Add(TableRow2("Email:", q.CustomerEmail, "", ""));
            }

            meta.RowGroups.Add(metaRg);
            doc.Blocks.Add(meta);
            doc.Blocks.Add(HRule());

            // ── LINE ITEMS TABLE ──────────────────────────────────────────
            var tbl = new Table { CellSpacing = 0, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0.5) };
            tbl.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
            tbl.Columns.Add(new TableColumn { Width = new GridLength(60) });
            tbl.Columns.Add(new TableColumn { Width = new GridLength(80) });
            tbl.Columns.Add(new TableColumn { Width = new GridLength(60) });
            tbl.Columns.Add(new TableColumn { Width = new GridLength(80) });

            var headerRg = new TableRowGroup();
            var headerRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xE8)) };
            headerRow.Cells.Add(HeaderCell("Item Description"));
            headerRow.Cells.Add(HeaderCell("Qty", TextAlignment.Center));
            headerRow.Cells.Add(HeaderCell("Unit Price", TextAlignment.Right));
            headerRow.Cells.Add(HeaderCell("Disc %", TextAlignment.Center));
            headerRow.Cells.Add(HeaderCell("Total", TextAlignment.Right));
            headerRg.Rows.Add(headerRow);
            tbl.RowGroups.Add(headerRg);

            var bodyRg = new TableRowGroup();
            bool alt = false;
            foreach (var line in q.Lines)
            {
                var row = new TableRow
                {
                    Background = alt
                        ? new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xF8))
                        : Brushes.White,
                };
                var desc = line.Name;
                if (!string.IsNullOrWhiteSpace(line.Size)) desc += $"  |  {line.Size}";
                if (!string.IsNullOrWhiteSpace(line.Attribute)) desc += $"  |  {line.Attribute}";

                row.Cells.Add(DataCell(desc));
                row.Cells.Add(DataCell(line.Qty.ToString(), TextAlignment.Center));
                row.Cells.Add(DataCell($"R {line.UnitPrice:N2}", TextAlignment.Right));
                row.Cells.Add(DataCell(line.DiscountPct > 0 ? $"{line.DiscountPct:N0}%" : "-", TextAlignment.Center));
                row.Cells.Add(DataCell($"R {line.LineTotal:N2}", TextAlignment.Right));
                bodyRg.Rows.Add(row);
                alt = !alt;
            }
            tbl.RowGroups.Add(bodyRg);
            doc.Blocks.Add(tbl);

            // ── TOTALS ────────────────────────────────────────────────────
            doc.Blocks.Add(new Paragraph(new Run("")) { Margin = new Thickness(0, 6, 0, 0) });
            doc.Blocks.Add(RightAlignedPara($"Subtotal:   R {q.Subtotal:N2}"));
            doc.Blocks.Add(RightAlignedPara($"Tax:        R {q.Tax:N2}"));
            doc.Blocks.Add(RightAlignedPara($"TOTAL:      R {q.Total:N2}",
                bold: true, fontSize: 13));

            // ── NOTES ─────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(q.Notes))
            {
                doc.Blocks.Add(HRule());
                doc.Blocks.Add(new Paragraph(new Run("Notes / Terms:"))
                { FontWeight = FontWeights.Bold, Margin = new Thickness(0, 4, 0, 2) });
                doc.Blocks.Add(new Paragraph(new Run(q.Notes))
                { FontSize = 10, Foreground = Brushes.DimGray });
            }

            // ── FOOTER WATERMARK ──────────────────────────────────────────
            doc.Blocks.Add(HRule());
            doc.Blocks.Add(CentredPara(
                "This is a quotation only and does not constitute a tax invoice or receipt.",
                9, foreground: Brushes.Gray));
            doc.Blocks.Add(CentredPara(
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}  |  Prepared by: {q.CreatedBy}",
                8, foreground: Brushes.LightGray));

            return doc;
        }

        // ── Block/cell helpers ────────────────────────────────────────────
        private static Paragraph CentredPara(string text, double size = 11,
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

        private static Paragraph RightAlignedPara(string text,
            bool bold = false, double fontSize = 11) =>
            new(new Run(text)
            {
                FontSize = fontSize,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            })
            {
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 1, 0, 1),
            };

        private static BlockUIContainer HRule()
        {
            var sep = new System.Windows.Shapes.Rectangle
            {
                Height = 0.5,
                Fill = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                Margin = new Thickness(0, 6, 0, 6),
            };
            return new BlockUIContainer(sep);
        }

        private static TableRow TableRow2(string l1, string v1, string l2, string v2)
        {
            var row = new TableRow();
            var p1 = new Paragraph();
            p1.Inlines.Add(new Run(l1) { FontWeight = FontWeights.SemiBold, FontSize = 10 });
            p1.Inlines.Add(new Run("  " + v1) { FontSize = 10 });
            var c1 = new TableCell(p1) { Padding = new Thickness(0, 2, 8, 2) };

            var p2 = new Paragraph();
            p2.Inlines.Add(new Run(l2) { FontWeight = FontWeights.SemiBold, FontSize = 10 });
            p2.Inlines.Add(new Run("  " + v2) { FontSize = 10 });
            var c2 = new TableCell(p2) { Padding = new Thickness(0, 2, 0, 2) };

            row.Cells.Add(c1);
            row.Cells.Add(c2);
            return row;
        }

        private static TableCell HeaderCell(string text,
            TextAlignment align = TextAlignment.Left)
        {
            var p = new Paragraph(new Run(text)
            { FontWeight = FontWeights.Bold, FontSize = 10 })
            { TextAlignment = align, Padding = new Thickness(4, 3, 4, 3) };
            return new TableCell(p)
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 1),
            };
        }

        private static TableCell DataCell(string text,
            TextAlignment align = TextAlignment.Left)
        {
            var p = new Paragraph(new Run(text) { FontSize = 10 })
            { TextAlignment = align, Padding = new Thickness(4, 3, 4, 3) };
            return new TableCell(p)
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)),
                BorderThickness = new Thickness(0, 0, 0, 0.5),
            };
        }
    }
}