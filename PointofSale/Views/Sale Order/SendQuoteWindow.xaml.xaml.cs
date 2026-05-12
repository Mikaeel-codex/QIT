using PointofSale.Models;
using PointofSale.Services;
using System;
using System.Net;
using System.Net.Mail;
using System.Windows;

namespace PointofSale.Views
{
    public partial class SendQuoteWindow : Window
    {
        private readonly QuoteData _quote;

        public SendQuoteWindow(QuoteData quote)
        {
            InitializeComponent();
            _quote = quote;

            QuoteRefTxt.Text = $"Quote: {quote.QuoteNumber}  |  Customer: {quote.CustomerName}";
            EmailBox.Text = quote.CustomerEmail;
            WhatsAppBox.Text = quote.CustomerPhone;
            SubjectBox.Text = $"Quotation {quote.QuoteNumber} from {quote.StoreName}";
        }

        // ── Email ─────────────────────────────────────────────────────────
        private void SendEmail_Click(object sender, RoutedEventArgs e)
        {
            var to = EmailBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(to))
            { ShowStatus("Enter a recipient email address.", error: true); return; }

            var fromEmail = StoreSettingsService.Get("EmailAddress", "");
            var appPass = StoreSettingsService.Get("EmailAppPassword", "");

            if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(appPass))
            {
                ShowStatus("Email not configured. Set Email Address and App Password in Store Settings.",
                    error: true);
                return;
            }

            try
            {
                var body = BuildEmailBody();

                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(fromEmail, appPass),
                };

                var msg = new MailMessage
                {
                    From = new MailAddress(fromEmail, _quote.StoreName),
                    Subject = SubjectBox.Text.Trim(),
                    Body = body,
                    IsBodyHtml = true,
                };
                msg.To.Add(to);

                client.Send(msg);
                ShowStatus($"✔  Email sent to {to}");
            }
            catch (Exception ex)
            {
                ShowStatus($"Email failed: {ex.Message}", error: true);
            }
        }

        private string BuildEmailBody()
        {
            var lines = "";
            foreach (var l in _quote.Lines)
            {
                lines += $"<tr>" +
                         $"<td style='padding:6px 8px;border-bottom:1px solid #eee'>{l.Name}" +
                         (string.IsNullOrWhiteSpace(l.Size) ? "" : $" | {l.Size}") +
                         (string.IsNullOrWhiteSpace(l.Attribute) ? "" : $" | {l.Attribute}") +
                         $"</td>" +
                         $"<td style='padding:6px 8px;border-bottom:1px solid #eee;text-align:center'>{l.Qty}</td>" +
                         $"<td style='padding:6px 8px;border-bottom:1px solid #eee;text-align:right'>R {l.UnitPrice:N2}</td>" +
                         $"<td style='padding:6px 8px;border-bottom:1px solid #eee;text-align:right'>R {l.LineTotal:N2}</td>" +
                         $"</tr>";
            }

            return $@"
<html><body style='font-family:Segoe UI,Arial;color:#222;max-width:680px;margin:auto'>
  <div style='background:#1a1a2e;padding:20px 30px;border-radius:8px 8px 0 0'>
    <h2 style='color:#f4c542;margin:0'>{_quote.StoreName}</h2>
    <p style='color:#aaa;margin:4px 0 0 0'>{_quote.StoreAddress} | {_quote.StorePhone}</p>
  </div>
  <div style='background:#fff;padding:24px 30px;border:1px solid #eee'>
    <div style='background:#fff3cd;border:1px solid #ffc107;border-radius:4px;padding:10px 16px;margin-bottom:20px'>
      <strong>⚠ QUOTATION — NOT A TAX INVOICE</strong>
    </div>
    <table style='width:100%;font-size:13px;margin-bottom:16px'>
      <tr><td><strong>Quote No:</strong> {_quote.QuoteNumber}</td>
          <td><strong>Date:</strong> {_quote.CreatedAt:dd MMM yyyy}</td></tr>
      <tr><td><strong>Customer:</strong> {_quote.CustomerName}</td>
          <td><strong>Valid Until:</strong> {_quote.ExpiresAt:dd MMM yyyy}</td></tr>
    </table>
    <table style='width:100%;border-collapse:collapse;font-size:13px'>
      <thead>
        <tr style='background:#f5f5f5'>
          <th style='padding:8px;text-align:left;border-bottom:2px solid #ddd'>Item</th>
          <th style='padding:8px;text-align:center;border-bottom:2px solid #ddd'>Qty</th>
          <th style='padding:8px;text-align:right;border-bottom:2px solid #ddd'>Unit Price</th>
          <th style='padding:8px;text-align:right;border-bottom:2px solid #ddd'>Total</th>
        </tr>
      </thead>
      <tbody>{lines}</tbody>
    </table>
    <div style='text-align:right;margin-top:16px;font-size:13px'>
      <div>Subtotal: <strong>R {_quote.Subtotal:N2}</strong></div>
      <div>Tax: <strong>R {_quote.Tax:N2}</strong></div>
      <div style='font-size:16px;margin-top:8px'>
        <strong>TOTAL: R {_quote.Total:N2}</strong>
      </div>
    </div>
    {(string.IsNullOrWhiteSpace(_quote.Notes) ? "" : $"<div style='margin-top:16px;padding:10px;background:#f9f9f9;border-left:3px solid #ccc'><strong>Notes:</strong><br>{_quote.Notes}</div>")}
    <hr style='margin:20px 0;border:none;border-top:1px solid #eee'/>
    <p style='color:#888;font-size:11px'>
      This is a quotation only and does not constitute a tax invoice or receipt.<br>
      Generated: {DateTime.Now:yyyy-MM-dd HH:mm} | Prepared by: {_quote.CreatedBy}
    </p>
  </div>
</body></html>";
        }

        // ── WhatsApp ──────────────────────────────────────────────────────
        private void SendWhatsApp_Click(object sender, RoutedEventArgs e)
        {
            var phone = WhatsAppBox.Text.Trim()
                .Replace(" ", "").Replace("+", "").Replace("-", "");

            if (string.IsNullOrWhiteSpace(phone))
            { ShowStatus("Enter a phone number.", error: true); return; }

            // Build text message
            var msg = BuildWhatsAppMessage();
            var encoded = Uri.EscapeDataString(msg);
            var url = $"https://wa.me/{phone}?text={encoded}";

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true,
                });
                ShowStatus("✔  WhatsApp opened in browser.");
            }
            catch (Exception ex)
            {
                ShowStatus($"Could not open WhatsApp: {ex.Message}", error: true);
            }
        }

        private string BuildWhatsAppMessage()
        {
            var lines = "";
            foreach (var l in _quote.Lines)
            {
                var desc = l.Name;
                if (!string.IsNullOrWhiteSpace(l.Size)) desc += $" {l.Size}";
                if (!string.IsNullOrWhiteSpace(l.Attribute)) desc += $" {l.Attribute}";
                lines += $"\n  • {desc}  x{l.Qty}  @R{l.UnitPrice:N2}  =  R{l.LineTotal:N2}";
            }

            return $"*{_quote.StoreName}*\n" +
                   $"⚠ QUOTATION — NOT A TAX INVOICE\n\n" +
                   $"Quote No: *{_quote.QuoteNumber}*\n" +
                   $"Date: {_quote.CreatedAt:dd MMM yyyy}\n" +
                   $"Valid Until: {_quote.ExpiresAt:dd MMM yyyy}\n" +
                   $"Customer: {_quote.CustomerName}\n\n" +
                   $"*Items:*{lines}\n\n" +
                   $"Subtotal: R{_quote.Subtotal:N2}\n" +
                   $"Tax:      R{_quote.Tax:N2}\n" +
                   $"*TOTAL:   R{_quote.Total:N2}*\n\n" +
                   (string.IsNullOrWhiteSpace(_quote.Notes) ? "" : $"Notes: {_quote.Notes}\n\n") +
                   $"_This is a quotation only._";
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void ShowStatus(string msg, bool error = false)
        {
            StatusTxt.Text = msg;
            StatusTxt.Foreground = error
                ? System.Windows.Media.Brushes.OrangeRed
                : System.Windows.Media.Brushes.LightGreen;
            StatusTxt.Visibility = Visibility.Visible;
        }
    }
}