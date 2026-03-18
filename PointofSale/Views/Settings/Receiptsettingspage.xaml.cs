using PointofSale.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PointofSale.Views.Settings
{
    public partial class ReceiptSettingsPage : Page
    {
        public ReceiptSettingsPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadSettings();
        }

        private void LoadSettings()
        {
            TxtPrefix.Text = StoreSettingsService.Get("ReceiptPrefix", "REC");
            TxtNextNumber.Text = StoreSettingsService.Get("NextReceiptNumber", "1");
            TxtFooter.Text = StoreSettingsService.Get("ReceiptFooter",
                                     "Thank you for your business!");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            StoreSettingsService.Set("ReceiptPrefix", TxtPrefix.Text.Trim());
            StoreSettingsService.Set("NextReceiptNumber", TxtNextNumber.Text.Trim());
            StoreSettingsService.Set("ReceiptFooter", TxtFooter.Text.Trim());

            StatusTxt.Text = "✔  Receipt settings saved.";
            StatusTxt.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#88CC66"));
            StatusTxt.Visibility = Visibility.Visible;
        }
    }
}