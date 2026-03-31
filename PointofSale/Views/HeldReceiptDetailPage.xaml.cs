using PointofSale.Models;
using PointofSale.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
            SubtotalTxt.Text = $"R{receipt.Subtotal:N2}";
            TaxTxt.Text = $"R{receipt.Tax:N2}";
            TotalTxt.Text = $"R{receipt.Total:N2}";
        }

        // ── Reprint ──────────────────────────────────────────────────────

        private void Reprint_Click(object sender, RoutedEventArgs e)
        {
            if (_receipt == null) return;

            var receiptNumber = $"HELD-{_receipt.Id}";
            var receipt = new ReceiptData
            {
                ReceiptNumber = receiptNumber,
                SaleDate = _receipt.HeldAt,
                Cashier = _receipt.Cashier,
                CustomerName = _receipt.CustomerName,
                StoreName = StoreSettingsService.Get("StoreName", "My Store"),
                StoreAddress = StoreSettingsService.Get("StoreAddress", ""),
                StorePhone = StoreSettingsService.Get("StorePhone", ""),
                StoreEmail = StoreSettingsService.Get("StoreEmail", ""),
                ReceiptFooter = StoreSettingsService.Get("ReceiptFooter", "Thank you for your business!"),
                LogoPath = StoreSettingsService.Get("LogoPath", ""),
                Subtotal = _receipt.Subtotal,
                Tax = _receipt.Tax,
                Total = _receipt.Total,
                Lines = _items.Select(i => new ReceiptLineItem
                {
                    SKU = i.SKU,
                    Name = i.Name,
                    Attribute = i.Attribute,
                    Size = i.Size,
                    Qty = i.Qty,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal,
                    TaxCode = i.TaxCode,
                }).ToList(),
                Payments = new List<ReceiptPaymentLine>
                {
                    new() { Label = _receipt.PaymentMethod, Amount = _receipt.Total }
                },
            };

            var owner = Window.GetWindow(this);
            var printWin = new PrintReceiptWindow(receiptNumber, showDigitalOption: true) { Owner = owner };
            printWin.ShowDialog();

            switch (printWin.Choice)
            {
                case PrintReceiptChoice.Print:
                    ThermalReceiptPrinter.PrintReceipt(receipt, owner);
                    break;
                case PrintReceiptChoice.Preview:
                    ThermalReceiptPrinter.PreviewReceipt(receipt, owner);
                    break;
                case PrintReceiptChoice.SendDigital:
                    new SendReceiptWindow(receipt) { Owner = owner }.ShowDialog();
                    break;
            }
        }
    }
}