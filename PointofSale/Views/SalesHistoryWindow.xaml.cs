using Microsoft.EntityFrameworkCore;
using PointofSale.Data;
using PointofSale.Models;
using PointofSale.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PointofSale.Views
{
    public partial class SalesHistoryWindow : Window
    {
        private List<Sale> _allSales = new();
        private Sale? _selected;

        public SalesHistoryWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadSales();
        }

        // ── Load all sales from DB ────────────────────────────────────────
        private void LoadSales()
        {
            using var db = new AppDbContext();
            _allSales = db.Sales
                .Include(s => s.Items)
                .OrderByDescending(s => s.SaleDate)
                .ToList();

            // Populate payment method filter
            var methods = _allSales
                .Select(s => s.PaymentMethod)
                .Distinct()
                .OrderBy(m => m)
                .ToList();
            methods.Insert(0, "All Methods");
            PaymentFilter.ItemsSource = methods;
            PaymentFilter.SelectedIndex = 0;

            // Set default date range — last 30 days
            DateFrom.SelectedDate = DateTime.Today.AddDays(-30);
            DateTo.SelectedDate = DateTime.Today;

            ApplyFilters();
            ClearDetail();
        }

        // ── Filtering ────────────────────────────────────────────────────
        private void Filter_Changed(object sender, EventArgs e) => ApplyFilters();

        private void ApplyFilters()
        {
            var query = SearchBox.Text.Trim().ToLower();
            var from = DateFrom.SelectedDate ?? DateTime.MinValue;
            var to = (DateTo.SelectedDate ?? DateTime.Today).AddDays(1);
            var method = PaymentFilter.SelectedItem as string ?? "All Methods";

            var filtered = _allSales.Where(s =>
                s.SaleDate >= from && s.SaleDate < to &&
                (string.IsNullOrEmpty(query) ||
                    s.ReceiptNumber.ToLower().Contains(query) ||
                    s.CustomerName.ToLower().Contains(query) ||
                    s.Cashier.ToLower().Contains(query)) &&
                (method == "All Methods" || s.PaymentMethod == method)
            ).ToList();

            SalesGrid.ItemsSource = filtered;

            // Exclude voided sales from summary totals
            var activeSales = filtered.Where(s => s.Status != "Voided").ToList();
            var total = activeSales.Sum(s => s.Total);
            var count = activeSales.Count;
            var voidedCount = filtered.Count - count;

            var summary = $"{count} sales  •  Total: R{total:N2}";
            if (voidedCount > 0)
                summary += $"  •  {voidedCount} voided";
            SummaryTxt.Text = summary;
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            DateFrom.SelectedDate = DateTime.Today.AddDays(-30);
            DateTo.SelectedDate = DateTime.Today;
            PaymentFilter.SelectedIndex = 0;
            ApplyFilters();
        }

        // ── Selection → detail panel ──────────────────────────────────────
        private void SalesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SalesGrid.SelectedItem is not Sale sale)
            {
                ClearDetail();
                return;
            }
            _selected = sale;
            ShowDetail(sale);
        }

        private void ShowDetail(Sale sale)
        {
            DetailReceiptTxt.Text = $"Receipt #{sale.ReceiptNumber}";
            DetailDateTxt.Text = sale.SaleDate.ToString("dddd, dd MMMM yyyy  HH:mm");
            DetailCustomerTxt.Text = string.IsNullOrEmpty(sale.CustomerName) ? "—" : sale.CustomerName;
            DetailCashierTxt.Text = string.IsNullOrEmpty(sale.Cashier) ? "—" : sale.Cashier;
            DetailPaymentTxt.Text = sale.PaymentMethod;
            DetailSubtotalTxt.Text = $"R{sale.Subtotal:N2}";
            DetailTaxTxt.Text = $"R{sale.Tax:N2}";
            DetailTotalTxt.Text = $"R{sale.Total:N2}";
            ItemsGrid.ItemsSource = sale.Items;

            // Show voided banner in detail panel if applicable
            if (sale.Status == "Voided")
            {
                DetailStatusTxt.Text = "⚠ THIS SALE HAS BEEN VOIDED";
                DetailStatusTxt.Visibility = Visibility.Visible;
            }
            else
            {
                DetailStatusTxt.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearDetail()
        {
            _selected = null;
            DetailReceiptTxt.Text = "Select a sale to view details";
            DetailDateTxt.Text = "";
            DetailCustomerTxt.Text = "";
            DetailCashierTxt.Text = "";
            DetailPaymentTxt.Text = "";
            DetailSubtotalTxt.Text = "";
            DetailTaxTxt.Text = "";
            DetailTotalTxt.Text = "";
            DetailStatusTxt.Visibility = Visibility.Collapsed;
            ItemsGrid.ItemsSource = null;
        }

        // ── Reprint ───────────────────────────────────────────────────────
        private void Reprint_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) { MessageBox.Show("Select a sale first."); return; }

            try
            {
                var receipt = BuildReceiptData(_selected);

                var printWin = new PrintReceiptWindow(receipt.ReceiptNumber, showDigitalOption: true) { Owner = this };
                printWin.ShowDialog();

                switch (printWin.Choice)
                {
                    case PrintReceiptChoice.Print:
                        Services.ThermalReceiptPrinter.PrintReceipt(receipt, this);
                        break;
                    case PrintReceiptChoice.Preview:
                        Services.ThermalReceiptPrinter.PreviewReceipt(receipt, this);
                        break;
                    case PrintReceiptChoice.SendDigital:
                        new SendReceiptWindow(receipt) { Owner = this }.ShowDialog();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not reprint: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Void Sale ─────────────────────────────────────────────────────
        private void VoidSale_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) { MessageBox.Show("Select a sale first."); return; }
            if (_selected.Status == "Voided")
            {
                MessageBox.Show("This sale has already been voided.");
                return;
            }

            // If current user cannot void sales, require a manager override
            if (!(Session.CurrentUser?.CanVoidSales ?? false))
            {
                var overrideWin = new ManagerOverrideWindow(
                    $"Void Sale — Receipt #{_selected.ReceiptNumber}",
                    "VoidSales")
                { Owner = this };
                overrideWin.ShowDialog();

                if (!overrideWin.Authorized) return;
            }

            var confirm = MessageBox.Show(
                $"Are you sure you want to void Receipt #{_selected.ReceiptNumber}?\n\n" +
                $"Total: R{_selected.Total:N2}\n\n" +
                $"This will mark the sale as voided and restore stock.",
                "Void Sale", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var db = new AppDbContext();
                var sale = db.Sales.Include(s => s.Items).First(s => s.Id == _selected.Id);
                sale.Status = "Voided";

                // Restore stock
                foreach (var item in sale.Items)
                {
                    var product = db.Products.Find(item.ProductId);
                    if (product != null)
                        product.StockQty += item.Quantity;
                }

                db.SaveChanges();

                _selected.Status = "Voided";
                MessageBox.Show($"Receipt #{sale.ReceiptNumber} has been voided and stock restored.",
                    "Sale Voided", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadSales();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to void sale: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Build ReceiptData from a saved Sale ───────────────────────────
        private static ReceiptData BuildReceiptData(Sale sale)
        {
            return new ReceiptData
            {
                ReceiptNumber = sale.ReceiptNumber,
                SaleDate = sale.SaleDate,
                Cashier = sale.Cashier,
                CustomerName = sale.CustomerName,
                StoreName = StoreSettingsService.Get("StoreName", "My Store"),
                StoreAddress = StoreSettingsService.Get("StoreAddress", ""),
                StorePhone = StoreSettingsService.Get("StorePhone", ""),
                StoreEmail = StoreSettingsService.Get("StoreEmail", ""),
                ReceiptFooter = StoreSettingsService.Get("ReceiptFooter", "Thank you for your business!"),
                LogoPath = StoreSettingsService.Get("LogoPath", ""),
                Subtotal = sale.Subtotal,
                Tax = sale.Tax,
                Total = sale.Total,
                Lines = sale.Items.Select(i => new ReceiptLineItem
                {
                    SKU = i.SKU,
                    Name = i.ProductName,
                    Attribute = "",
                    Size = "",
                    Qty = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal,
                    TaxCode = "",
                    DiscountPct = 0,
                }).ToList(),
                Payments = new List<ReceiptPaymentLine>
                {
                    new() { Label = sale.PaymentMethod, Amount = sale.Total }
                },
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}