using PointofSale.Models;
using PointofSale.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Windows;

namespace PointofSale.Views
{
    public partial class SendReceiptWindow : Window
    {
        private readonly ReceiptData _data;
        private string? _pdfPath;

        public SendReceiptWindow(ReceiptData data)
        {
            InitializeComponent();
            _data = data;

            // Pre-fill contact details from receipt data if available
            if (!string.IsNullOrEmpty(data.CustomerEmail))
            {
                EmailBox.Text = data.CustomerEmail;
                ChkEmail.IsChecked = true;
            }
            if (!string.IsNullOrEmpty(data.CustomerPhone))
            {
                WhatsAppBox.Text = data.CustomerPhone;
                ChkWhatsApp.IsChecked = true;
            }

            SubtitleTxt.Text = $"Receipt #{data.ReceiptNumber}  —  {data.Total:N2}";
        }

        // ── Toggle input field visibility ────────────────────────────────
        private void Option_Changed(object sender, RoutedEventArgs e)
        {
            if (EmailFields != null)
                EmailFields.Visibility = ChkEmail.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (WhatsAppFields != null)
                WhatsAppFields.Visibility = ChkWhatsApp.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Send ─────────────────────────────────────────────────────────
        private void Send_Click(object sender, RoutedEventArgs e)
        {
            var sendEmail = ChkEmail.IsChecked == true;
            var sendWhatsApp = ChkWhatsApp.IsChecked == true;

            if (!sendEmail && !sendWhatsApp)
            {
                ShowStatus("Please select at least one option, or click Skip.", isError: true);
                return;
            }

            // Validate inputs
            if (sendEmail && string.IsNullOrWhiteSpace(EmailBox.Text))
            { ShowStatus("Please enter an email address.", isError: true); return; }

            if (sendWhatsApp && string.IsNullOrWhiteSpace(WhatsAppBox.Text))
            { ShowStatus("Please enter a WhatsApp number.", isError: true); return; }

            // Generate PDF
            ShowStatus("Generating PDF...", isError: false);
            BtnSend.IsEnabled = false;
            try
            {
                _pdfPath = ReceiptPdfService.Generate(_data);
            }
            catch (Exception ex)
            {
                ShowStatus($"PDF error: {ex.Message}", isError: true);
                BtnSend.IsEnabled = true;
                return;
            }

            // Send via selected channels
            var errors = "";

            if (sendEmail)
            {
                try { SendEmail(EmailBox.Text.Trim(), _pdfPath); }
                catch (Exception ex) { errors += $"Email: {ex.Message}\n"; }
            }

            if (sendWhatsApp)
            {
                try { OpenWhatsApp(WhatsAppBox.Text.Trim(), _pdfPath); }
                catch (Exception ex) { errors += $"WhatsApp: {ex.Message}\n"; }
            }

            BtnSend.IsEnabled = true;

            if (!string.IsNullOrEmpty(errors))
            {
                ShowStatus(errors.Trim(), isError: true);
                return;
            }

            DialogResult = true;
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // ── Email ────────────────────────────────────────────────────────
        /// <summary>
        /// Opens the default mail client with the PDF attached.
        /// For a production app you'd configure SMTP here instead.
        /// </summary>
        private void SendEmail(string toAddress, string pdfPath)
        {
            var fromAddress = StoreSettingsService.Get("EmailAddress");
            var appPassword = StoreSettingsService.Get("EmailAppPassword");
            var senderName = StoreSettingsService.Get("EmailSenderName",
                                  StoreSettingsService.Get("StoreName", "My Store"));

            if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(appPassword))
            {
                MessageBox.Show(
                    "Email is not configured yet.\n\nGo to ⚙ Settings → Email tab to add your Gmail address and App Password.",
                    "Email Not Configured", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var message = new MailMessage();
            message.From = new MailAddress(fromAddress, senderName);
            message.To.Add(toAddress);
            message.Subject = $"Your Receipt #{_data.ReceiptNumber}";
            message.Body =
                $"Hi {_data.CustomerName},\n\n" +
                $"Please find your receipt attached.\n\n" +
                $"Receipt #: {_data.ReceiptNumber}\n" +
                $"Date: {_data.SaleDate:dd/MM/yyyy}\n" +
                $"Total: R{_data.Total:N2}\n\n" +
                $"{StoreSettingsService.Get("ReceiptFooter", "Thank you for your business!")}\n\n" +
                $"{senderName}";

            message.Attachments.Add(new Attachment(pdfPath));

            using var smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential(fromAddress, appPassword);
            smtp.Send(message);

            MessageBox.Show(
                $"Receipt emailed successfully to {toAddress}",
                "Email Sent", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── WhatsApp ─────────────────────────────────────────────────────
        /// <summary>
        /// Opens WhatsApp Web / WhatsApp Desktop with a pre-filled message.
        /// The PDF must be shared manually in the chat once WhatsApp opens.
        /// </summary>
        private void OpenWhatsApp(string phone, string pdfPath)
        {
            // Normalise phone — strip spaces, dashes, brackets
            var cleaned = Regex.Replace(phone, @"[\s\-\(\)]", "");
            if (!cleaned.StartsWith("+"))
                cleaned = "+" + cleaned;

            // Remove the leading +
            var digits = cleaned.TrimStart('+');

            var message = Uri.EscapeDataString(
                $"Hi! Here is your receipt from {_data.StoreName}.\n\n" +
                $"Receipt #: {_data.ReceiptNumber}\n" +
                $"Date: {_data.SaleDate:dd/MM/yyyy}\n" +
                $"Total: {_data.Total:N2}\n\n" +
                $"Thank you for your purchase!");

            var url = $"https://wa.me/{digits}?text={message}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            // Open PDF location so cashier can share it in the WhatsApp chat
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{pdfPath}\"")
            { UseShellExecute = true });

            MessageBox.Show(
                $"WhatsApp has been opened in your browser.\n\n" +
                $"Receipt PDF saved to:\n{pdfPath}\n\n" +
                $"Attach the PDF in the WhatsApp chat to send it to the customer.",
                "WhatsApp Ready", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private void ShowStatus(string msg, bool isError)
        {
            StatusTxt.Text = msg;
            StatusTxt.Foreground = isError
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE5, 0x39, 0x35))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x88, 0xCC, 0x66));
            StatusTxt.Visibility = Visibility.Visible;
        }
    }
}