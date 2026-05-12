using PointofSale.Data;
using PointofSale.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PointofSale.Views
{
    public partial class GiftCardWindow : Window
    {
        private readonly decimal _saleTotal;
        private readonly string _cashier;

        public decimal AmountRedeemed { get; private set; }
        public int CardId { get; private set; }
        public bool IsRedeem { get; private set; }
        public string CardNumber { get; private set; } = "";

        public GiftCardWindow(decimal saleTotal, string cashier)
        {
            InitializeComponent();
            _saleTotal = saleTotal;
            _cashier = cashier;
            AmountBox.Text = _saleTotal > 0 ? _saleTotal.ToString("N2") : "";
            CardNumberBox.Focus();
        }

        private void Mode_Changed(object sender, RoutedEventArgs e)
        {
            if (AmountBox == null) return;
            AmountBox.Text = RbRedeem.IsChecked == true
                ? (_saleTotal > 0 ? _saleTotal.ToString("N2") : "")
                : "";
            BalancePanel.Visibility = Visibility.Collapsed;
        }

        private void CardNumber_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (RbRedeem.IsChecked != true) return;
            var cardNo = CardNumberBox.Text.Trim();
            if (cardNo.Length < 4) { BalancePanel.Visibility = Visibility.Collapsed; return; }
            try
            {
                using var db = new AppDbContext();
                var card = db.GiftCards.FirstOrDefault(g => g.CardNumber == cardNo);
                if (card != null)
                {
                    BalanceTxt.Text = card.Status == "Active"
                        ? $"Card found  ·  Balance: R{card.Balance:N2}"
                        : $"Card status: {card.Status} — cannot redeem";
                    BalancePanel.Background = card.Status == "Active"
                        ? System.Windows.Media.Brushes.Honeydew
                        : System.Windows.Media.Brushes.MistyRose;
                    BalanceTxt.Foreground = card.Status == "Active"
                        ? System.Windows.Media.Brushes.DarkGreen
                        : System.Windows.Media.Brushes.DarkRed;
                    BalancePanel.Visibility = Visibility.Visible;
                }
                else { BalancePanel.Visibility = Visibility.Collapsed; }
            }
            catch { BalancePanel.Visibility = Visibility.Collapsed; }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var cardNo = CardNumberBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(cardNo))
            { MessageBox.Show("Please enter or swipe a gift card number.", "Required"); return; }

            if (!decimal.TryParse(AmountBox.Text, NumberStyles.Any,
                    CultureInfo.CurrentCulture, out decimal amount) || amount <= 0)
            { MessageBox.Show("Please enter a valid amount.", "Invalid Amount"); return; }

            try
            {
                using var db = new AppDbContext();

                if (RbRedeem.IsChecked == true)
                {
                    // VALIDATE ONLY — no DB write. Deduction happens in FinalizeSale.
                    var card = db.GiftCards.FirstOrDefault(g => g.CardNumber == cardNo);
                    if (card == null)
                    { MessageBox.Show("Gift card not found.", "Card Not Found", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                    if (card.Status != "Active")
                    { MessageBox.Show($"This card is {card.Status} and cannot be redeemed.", "Card Unavailable", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                    if (card.Balance < amount)
                    {
                        var p = MessageBox.Show(
                            $"Card balance (R{card.Balance:N2}) is less than the amount (R{amount:N2}).\n\nApply the full card balance and collect the remaining R{amount - card.Balance:N2} separately?",
                            "Insufficient Balance", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (p != MessageBoxResult.Yes) return;
                        amount = card.Balance;
                    }
                    CardId = card.Id; AmountRedeemed = amount; IsRedeem = true; CardNumber = card.CardNumber ?? card.Id.ToString(); DialogResult = true;
                }
                else
                {
                    // Purchase/Recharge — standalone transaction, write immediately
                    var existing = db.GiftCards.FirstOrDefault(g => g.CardNumber == cardNo);
                    if (existing != null)
                    {
                        existing.Balance += amount; existing.Status = "Active"; existing.LastUsedAt = DateTime.Now;
                        db.SaveChanges();
                        MessageBox.Show($"Card recharged.\nNew balance: R{existing.Balance:N2}", "Recharge Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        db.GiftCards.Add(new GiftCard { CardNumber = cardNo, Balance = amount, IssuedValue = amount, IssuedAt = DateTime.Now, Status = "Active", IssuedBy = _cashier });
                        db.SaveChanges();
                        MessageBox.Show($"New gift card issued.\nCard: {cardNo}\nBalance: R{amount:N2}", "Card Issued", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    IsRedeem = false; DialogResult = true;
                }
            }
            catch (Exception ex) { MessageBox.Show($"Gift card error: {ex.InnerException?.Message ?? ex.Message}", "Error"); }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Decimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"[\d\.]");
    }
}