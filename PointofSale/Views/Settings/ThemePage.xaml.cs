using PointofSale.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PointofSale.Views.Settings
{
    public partial class ThemePage : Page
    {
        public ThemePage()
        {
            InitializeComponent();
            Loaded += (s, e) => HighlightActive();
        }

        private void Theme_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border card) return;
            var key = card.Tag?.ToString() ?? "Dark";

            ThemeService.Apply(key);
            HighlightActive();

            StatusTxt.Text = $"✔  Theme changed to {key}.";
            StatusTxt.Visibility = Visibility.Visible;
        }

        private void HighlightActive()
        {
            var current = StoreSettingsService.Get(ThemeService.SettingKey, ThemeService.DefaultTheme);

            var activeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107"));
            var inactiveBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E2E2E"));

            CardDark.BorderBrush = current == "Dark" ? activeBrush : inactiveBrush;
            CardForest.BorderBrush = current == "Forest" ? activeBrush : inactiveBrush;
            CardOcean.BorderBrush = current == "Ocean" ? activeBrush : inactiveBrush;
            CardBlossom.BorderBrush = current == "Blossom" ? activeBrush : inactiveBrush;
            CardCandy.BorderBrush = current == "Candy" ? activeBrush : inactiveBrush;
            CardMocha.BorderBrush = current == "Mocha" ? activeBrush : inactiveBrush;

            LabelDark.FontWeight = current == "Dark" ? FontWeights.Bold : FontWeights.Normal;
            LabelForest.FontWeight = current == "Forest" ? FontWeights.Bold : FontWeights.Normal;
            LabelOcean.FontWeight = current == "Ocean" ? FontWeights.Bold : FontWeights.Normal;
            LabelBlossom.FontWeight = current == "Blossom" ? FontWeights.Bold : FontWeights.Normal;
            LabelCandy.FontWeight = current == "Candy" ? FontWeights.Bold : FontWeights.Normal;
            LabelMocha.FontWeight = current == "Mocha" ? FontWeights.Bold : FontWeights.Normal;
        }
    }
}