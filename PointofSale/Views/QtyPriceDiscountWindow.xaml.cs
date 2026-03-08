using PointofSale.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace PointofSale.Views
{
    public partial class QtyPriceDiscountWindow : Window
    {
        private readonly CartLine _line;
        private bool _upd = false;

        public QtyPriceDiscountWindow(CartLine line)
        {
            InitializeComponent();
            _line = line;
            QtyBox.Text = _line.Qty.ToString();
            UnitPriceBox.Text = _line.UnitPrice.ToString("N2");
            DiscountAmtBox.Text = "0.00";
            DiscountPctBox.Text = "0.00";
            RecalcExtendedPrice();
        }

        private void QuickDiscount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && decimal.TryParse(btn.Tag?.ToString(), out var pct))
            { _upd = true; DiscountPctBox.Text = pct.ToString("N2"); _upd = false; ApplyDiscountPct(pct); }
        }

        private void RecalcExtended(object sender, TextChangedEventArgs e) => RecalcExtendedPrice();

        private void RecalcExtendedPrice()
        {
            if (QtyBox == null || UnitPriceBox == null || ExtendedPriceBox == null) return;
            if (int.TryParse(QtyBox.Text, out var q) &&
                decimal.TryParse(UnitPriceBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var p))
                ExtendedPriceBox.Text = (q * p).ToString("N2");
        }

        private void DiscountAmt_Changed(object sender, TextChangedEventArgs e)
        {
            if (_upd || UnitPriceBox == null || DiscountPctBox == null) return;
            if (decimal.TryParse(DiscountAmtBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var a) &&
                decimal.TryParse(UnitPriceBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var p) && p > 0)
            { _upd = true; DiscountPctBox.Text = Math.Round(a / p * 100, 2).ToString("N2"); _upd = false; }
        }

        private void DiscountPct_Changed(object sender, TextChangedEventArgs e)
        {
            if (_upd || UnitPriceBox == null || DiscountAmtBox == null) return;
            if (decimal.TryParse(DiscountPctBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var pct) &&
                decimal.TryParse(UnitPriceBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var p))
            { _upd = true; DiscountAmtBox.Text = Math.Round(p * pct / 100, 2).ToString("N2"); _upd = false; }
        }

        private void ApplyDiscountPct(decimal pct)
        {
            if (decimal.TryParse(UnitPriceBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var p))
            { _upd = true; DiscountAmtBox.Text = Math.Round(p * pct / 100, 2).ToString("N2"); _upd = false; }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(QtyBox.Text, out var qty) || qty <= 0)
            { MessageBox.Show("Invalid quantity.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (!decimal.TryParse(UnitPriceBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var price) || price < 0)
            { MessageBox.Show("Invalid price.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            decimal.TryParse(DiscountPctBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var discPct);
            _line.Qty = qty;
            _line.UnitPrice = discPct > 0 ? Math.Round(price * (100m - discPct) / 100m, 2) : price;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}