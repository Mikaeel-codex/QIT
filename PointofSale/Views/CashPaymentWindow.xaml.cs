using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace PointofSale.Views
{
    public partial class CashPaymentWindow : Window
    {
        private readonly decimal _total;
        public decimal AmountTendered { get; private set; }

        public CashPaymentWindow(decimal total)
        {
            InitializeComponent();
            _total = total;
            AmountBox.Text = total.ToString("N2");
            AmountBox.SelectAll();
            AmountBox.Focus();
        }

        // Quick buttons: adds tag value to current amount
        private void Quick_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && decimal.TryParse(btn.Tag?.ToString(), out var add))
            {
                var current = ParseAmount();
                AmountBox.Text = (current + add).ToString("N2");
                AmountBox.SelectAll();
            }
        }

        // Round up to next whole dollar
        private void NextRand_Click(object sender, RoutedEventArgs e)
        {
            var current = ParseAmount();
            var next = current == Math.Floor(current)
                ? Math.Floor(current) + 1
                : Math.Ceiling(current);
            AmountBox.Text = next.ToString("N2");
            AmountBox.SelectAll();
        }

        // Set to exact total
        private void Exact_Click(object sender, RoutedEventArgs e)
        {
            AmountBox.Text = _total.ToString("N2");
            AmountBox.SelectAll();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var amt = ParseAmount();
            if (amt <= 0)
            {
                MessageBox.Show("Please enter a valid amount.", "Invalid Amount",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (amt < _total)
            {
                MessageBox.Show(string.Format("Amount tendered ({0:C}) is less than the total ({1:C}).", amt, _total),
                    "Insufficient Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            AmountTendered = amt;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private decimal ParseAmount()
        {
            var text = AmountBox.Text.Replace("R", "").Replace(",", "").Trim();
            return decimal.TryParse(text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.CurrentCulture, out var result) ? result : 0;
        }
    }
}