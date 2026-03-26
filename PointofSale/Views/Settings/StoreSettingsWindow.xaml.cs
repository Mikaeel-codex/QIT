using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PointofSale.Views
{
    public partial class StoreSettingsWindow : Window
    {
        private List<Button> _navButtons = new();

        public StoreSettingsWindow()
        {
            InitializeComponent();
            _navButtons.AddRange(new[] { NavStoreInfo, NavReceipt, NavTax, NavPrinter, NavTheme });
            NavigateTo("StoreInfo");
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
                NavigateTo(tag);
        }

        private void NavigateTo(string tag)
        {
            var inactive = FindResource("NavBtn") as Style;
            var active = FindResource("NavBtnActive") as Style;

            foreach (var btn in _navButtons)
                btn.Style = inactive;

            switch (tag)
            {
                case "StoreInfo":
                    NavStoreInfo.Style = active;
                    PageTitle.Text = "Store & Email";
                    PageSubtitle.Text = "Your store details and email configuration.";
                    ContentFrame.Navigate(new Settings.StoreInfoPage());
                    break;

                case "Receipt":
                    NavReceipt.Style = active;
                    PageTitle.Text = "Receipt";
                    PageSubtitle.Text = "Receipt numbering, footer message and format.";
                    ContentFrame.Navigate(new Settings.ReceiptSettingsPage());
                    break;

                case "Tax":
                    NavTax.Style = active;
                    PageTitle.Text = "Tax";
                    PageSubtitle.Text = "Configure tax rates and codes.";
                    ContentFrame.Navigate(new Settings.ComingSoonPage("Tax configuration coming soon."));
                    break;

                case "Printer":
                    NavPrinter.Style = active;
                    PageTitle.Text = "Printer";
                    PageSubtitle.Text = "Receipt printer and paper settings.";
                    ContentFrame.Navigate(new Settings.ComingSoonPage("Printer settings coming soon."));
                    break;

                case "Theme":
                    NavTheme.Style = active;
                    PageTitle.Text = "Appearance";
                    PageSubtitle.Text = "Choose a colour theme for the application.";
                    ContentFrame.Navigate(new Settings.ThemePage());
                    break;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}