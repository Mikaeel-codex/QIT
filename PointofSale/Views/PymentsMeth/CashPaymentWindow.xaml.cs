using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace PointofSale.Views
{
    public partial class CashPaymentWindow : Window
    {
        private readonly decimal _owing;
        public decimal AmountTendered { get; private set; }

        /// <param name="owing">Amount still due (remaining after other splits).</param>
        /// <param name="saleTotal">Full sale total — shown in summary.</param>
        /// <param name="alreadyPaid">Sum of splits already added — shown in summary.</param>
        public CashPaymentWindow(decimal owing, decimal saleTotal = 0, decimal alreadyPaid = 0, bool isReturn = false)
        {
            InitializeComponent();
            _owing = owing;

            if (isReturn)
            {
                Title = "Cash Refund";
                SaleTotalLbl.Text  = "Refund Total";
                OwingLbl.Text      = "Still to Refund";
                AlreadyPaidLbl.Text = "Already Refunded";
            }

            var total = saleTotal > 0 ? saleTotal : owing;
            TotalTxt.Text = total.ToString("N2");
            OwingTxt.Text = owing.ToString("N2");

            if (alreadyPaid > 0)
            {
                AlreadyPaidLbl.Visibility = Visibility.Visible;
                AlreadyPaidTxt.Visibility = Visibility.Visible;
                AlreadyPaidTxt.Text = alreadyPaid.ToString("N2");
            }

            AmountBox.Text = owing.ToString("N2");
            AmountBox.SelectAll();
            AmountBox.Focus();
        }

        // Quick buttons: adds tag value to current amount
        // Round up to next whole rand
        private void NextRand_Click(object sender, RoutedEventArgs e)
        {
            var current = ParseAmount();
            var next = current == Math.Floor(current)
                ? Math.Floor(current) + 1
                : Math.Ceiling(current);
            AmountBox.Text = next.ToString("N2");
            AmountBox.SelectAll();
        }

        private void Exact_Click(object sender, RoutedEventArgs e)
        {
            AmountBox.Text = _owing.ToString("N2");
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