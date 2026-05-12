using Microsoft.EntityFrameworkCore;
using PointofSale.Data;
using PointofSale.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PointofSale.Views
{
    public partial class ReturnItemWindow : Window
    {
        private readonly CartLine _line;

        public bool Confirmed { get; private set; } = false;
        public string ReceiptNumber { get; private set; } = "";
        public string ReturnReason { get; private set; } = "";

        public ReturnItemWindow(CartLine line)
        {
            InitializeComponent();
            _line = line;
            ItemNameTxt.Text = $"Item: {line.Name}  |  Qty: {Math.Abs(line.Qty)}  |  Price: R{line.UnitPrice:N2}";
        }

        private void ReceiptNoBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) LookupSale();
        }

        private void LookupBtn_Click(object sender, RoutedEventArgs e) => LookupSale();

        private void LookupSale()
        {
            HideError();
            ResultPanel.Visibility = Visibility.Collapsed;
            ConfirmBtn.IsEnabled = false;

            var receiptNo = ReceiptNoBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(receiptNo))
            {
                ShowError("Please enter a receipt number.");
                return;
            }

            using var db = new AppDbContext();
            var sale = db.Sales
                .Include(s => s.Items)
                .FirstOrDefault(s => s.ReceiptNumber == receiptNo);

            if (sale == null)
            {
                ShowError($"Receipt \"{receiptNo}\" was not found.");
                return;
            }

            if (sale.Status == "Voided")
            {
                ShowError("This receipt has been voided and cannot be used for a return.");
                return;
            }

            // Check if this product was on the original sale
            var originalItem = sale.Items.FirstOrDefault(i => i.ProductId == _line.ProductId);
            if (originalItem == null)
            {
                ShowError($"\"{_line.Name}\" was not on receipt {receiptNo}.\nPlease check the receipt number and try again.");
                return;
            }

            // Check return qty doesn't exceed original qty
            var returnQty = Math.Abs(_line.Qty);
            if (returnQty > originalItem.Quantity)
            {
                ShowError($"Cannot return {returnQty} — only {originalItem.Quantity} was sold on this receipt.");
                return;
            }

            // All good — show result
            SaleDateTxt.Text  = $"Sale Date:  {sale.SaleDate:dd MMM yyyy  HH:mm}";
            SaleTotalTxt.Text = $"Receipt Total:  R{sale.Total:N2}  |  Cashier: {sale.Cashier}";
            ItemFoundTxt.Text = $"✓  {_line.Name} — {originalItem.Quantity} sold at R{originalItem.UnitPrice:N2}";
            ItemFoundTxt.Foreground = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));

            ResultPanel.Visibility = Visibility.Visible;
            ConfirmBtn.IsEnabled = true;
            ReceiptNumber = receiptNo;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var reason = (ReasonBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "";
            var notes  = ReasonNotesBox.Text.Trim();
            ReturnReason = string.IsNullOrEmpty(notes) ? reason : $"{reason} — {notes}";
            Confirmed = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void ShowError(string msg)
        {
            ErrorTxt.Text = msg;
            ErrorTxt.Visibility = Visibility.Visible;
        }

        private void HideError() => ErrorTxt.Visibility = Visibility.Collapsed;
    }
}
