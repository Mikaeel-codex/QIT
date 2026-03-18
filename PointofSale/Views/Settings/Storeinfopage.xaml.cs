using PointofSale.Services;
using System;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace PointofSale.Views.Settings
{
    public partial class StoreInfoPage : Page
    {
        public StoreInfoPage()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadSettings();
        }

        private void LoadSettings()
        {
            TxtStoreName.Text = StoreSettingsService.Get("StoreName");
            TxtStoreAddress.Text = StoreSettingsService.Get("StoreAddress");
            TxtStorePhone.Text = StoreSettingsService.Get("StorePhone");
            TxtStoreEmail.Text = StoreSettingsService.Get("StoreEmail");
            TxtTaxNumber.Text = StoreSettingsService.Get("TaxNumber");
            TxtEmailAddress.Text = StoreSettingsService.Get("EmailAddress");
            TxtAppPassword.Password = StoreSettingsService.Get("EmailAppPassword");
            TxtSenderName.Text = StoreSettingsService.Get("EmailSenderName",
                                          StoreSettingsService.Get("StoreName"));
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            StoreSettingsService.Set("StoreName", TxtStoreName.Text.Trim());
            StoreSettingsService.Set("StoreAddress", TxtStoreAddress.Text.Trim());
            StoreSettingsService.Set("StorePhone", TxtStorePhone.Text.Trim());
            StoreSettingsService.Set("StoreEmail", TxtStoreEmail.Text.Trim());
            StoreSettingsService.Set("TaxNumber", TxtTaxNumber.Text.Trim());
            StoreSettingsService.Set("EmailAddress", TxtEmailAddress.Text.Trim());
            StoreSettingsService.Set("EmailAppPassword", TxtAppPassword.Password);
            StoreSettingsService.Set("EmailSenderName", TxtSenderName.Text.Trim());

            ShowStatus("✔  Settings saved.", success: true);
        }

        private void SendTest_Click(object sender, RoutedEventArgs e)
        {
            var to = TxtTestEmail.Text.Trim();
            var from = TxtEmailAddress.Text.Trim();
            var password = TxtAppPassword.Password;
            var senderName = TxtSenderName.Text.Trim();

            if (string.IsNullOrEmpty(to))
            { ShowTestResult("Enter a test email address.", false); return; }
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(password))
            { ShowTestResult("Enter your Gmail address and App Password first.", false); return; }

            try
            {
                using var msg = new MailMessage();
                msg.From = new MailAddress(from, senderName);
                msg.To.Add(to);
                msg.Subject = "POS Test Email";
                msg.Body = "This is a test email from your Point of Sale system.\n\nEmail settings are working correctly!";

                using var smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.EnableSsl = true;
                smtp.Credentials = new NetworkCredential(from, password);
                smtp.Send(msg);

                ShowTestResult("✔  Test email sent successfully!", true);
            }
            catch (Exception ex)
            {
                ShowTestResult($"✖  {ex.Message}", false);
            }
        }

        private void ShowStatus(string msg, bool success)
        {
            StatusTxt.Text = msg;
            StatusTxt.Foreground = Brush(success ? "#88CC66" : "#E53935");
            StatusTxt.Visibility = Visibility.Visible;
        }

        private void ShowTestResult(string msg, bool success)
        {
            TestResultTxt.Text = msg;
            TestResultTxt.Foreground = Brush(success ? "#88CC66" : "#E53935");
            TestResultTxt.Visibility = Visibility.Visible;
        }

        private static SolidColorBrush Brush(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));
    }
}