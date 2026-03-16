using PointofSale.Models;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PointofSale.Views
{
    public partial class HeldReceiptDetailPage : Page
    {
        // Set by HeldReceiptsWindow so the page can close the detail panel
        public System.Action? OnCloseRequested { get; set; }

        private HeldReceipt? _receipt;
        private List<HeldReceiptItem> _items = new();

        public HeldReceiptDetailPage()
        {
            InitializeComponent();

            // Close detail when user clicks on the page background (outside the grid)
            MouseDown += (s, e) =>
            {
                // Only close if click is directly on the Page background, not a child
                if (e.Source == this)
                    OnCloseRequested?.Invoke();
            };
        }

        public void Load(HeldReceipt receipt, List<HeldReceiptItem> items)
        {
            _receipt = receipt;
            _items = items;

            // Header bar
            DateTxt.Text = receipt.HeldAt.ToString("MM/dd/yyyy   HH:mm");
            CashierTxt.Text = string.IsNullOrWhiteSpace(receipt.Cashier)
                                   ? "" : $"Cashier: {receipt.Cashier}";
            CustomerTxt.Text = string.IsNullOrWhiteSpace(receipt.CustomerName)
                                   ? "" : $"Customer: {receipt.CustomerName}";

            // Payment button
            PaymentBtn.Content = string.IsNullOrWhiteSpace(receipt.PaymentMethod)
                                     ? "None" : receipt.PaymentMethod;

            // Pending EFT badge + watermark
            bool isPendingEft = receipt.Status == "PendingEFT";
            PendingEftBadge.Visibility = isPendingEft ? Visibility.Visible : Visibility.Collapsed;
            WatermarkText.Text = isPendingEft ? "Pending EFT" : "Held";
            WatermarkText.FontSize = isPendingEft ? 60 : 96;

            // Items grid
            ItemsGrid.ItemsSource = items;

            // Totals
            SubtotalTxt.Text = $"${receipt.Subtotal:N2}";
            TaxTxt.Text = $"${receipt.Tax:N2}";
            TotalTxt.Text = $"${receipt.Total:N2}";
        }

        // ── Reprint ──────────────────────────────────────────────────────

        private void Reprint_Click(object sender, RoutedEventArgs e)
        {
            if (_receipt == null) return;

            // Build a simple print document
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Courier New"),
                FontSize = 12,
                PageWidth = 320,
                PagePadding = new Thickness(20)
            };

            // Header
            doc.Blocks.Add(MakePara("HELD RECEIPT - REPRINT", 14, FontWeights.Bold, TextAlignment.Center));
            doc.Blocks.Add(MakePara($"Date: {_receipt.HeldAt:MM/dd/yyyy HH:mm}", 11, FontWeights.Normal, TextAlignment.Center));
            if (!string.IsNullOrWhiteSpace(_receipt.Cashier))
                doc.Blocks.Add(MakePara($"Cashier: {_receipt.Cashier}", 11, FontWeights.Normal, TextAlignment.Center));
            doc.Blocks.Add(MakePara(new string('-', 40), 11, FontWeights.Normal, TextAlignment.Left));

            // Column headers
            doc.Blocks.Add(MakePara(
                $"{"Item",-22} {"Qty",4} {"Price",8} {"Total",8}",
                10, FontWeights.Bold, TextAlignment.Left));
            doc.Blocks.Add(MakePara(new string('-', 40), 10, FontWeights.Normal, TextAlignment.Left));

            // Line items
            foreach (var item in _items)
            {
                var name = item.Name.Length > 22 ? item.Name[..22] : item.Name;
                doc.Blocks.Add(MakePara(
                    $"{name,-22} {item.Qty,4} {item.UnitPrice,8:N2} {item.LineTotal,8:N2}",
                    10, FontWeights.Normal, TextAlignment.Left));
            }

            doc.Blocks.Add(MakePara(new string('-', 40), 10, FontWeights.Normal, TextAlignment.Left));
            doc.Blocks.Add(MakePara($"{"Subtotal:",-28} {_receipt.Subtotal,10:N2}", 11, FontWeights.Normal, TextAlignment.Left));
            doc.Blocks.Add(MakePara($"{"Tax:",-28} {_receipt.Tax,10:N2}", 11, FontWeights.Normal, TextAlignment.Left));
            doc.Blocks.Add(MakePara($"{"TOTAL:",-28} {_receipt.Total,10:N2}", 13, FontWeights.Bold, TextAlignment.Left));

            // Print
            var pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                doc.PageWidth = pd.PrintableAreaWidth;
                var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                pd.PrintDocument(paginator, "Held Receipt Reprint");
            }
        }

        private static Paragraph MakePara(string text, double size,
            FontWeight weight, TextAlignment align)
        {
            return new Paragraph(new Run(text))
            {
                FontSize = size,
                FontWeight = weight,
                TextAlignment = align,
                Margin = new Thickness(0, 1, 0, 1)
            };
        }
    }
}