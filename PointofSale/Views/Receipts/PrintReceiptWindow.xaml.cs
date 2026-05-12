using System.Windows;

namespace PointofSale.Views
{
    public enum PrintReceiptChoice { Print, Preview, SendDigital, Skip, Cancelled }

    public partial class PrintReceiptWindow : Window
    {
        public PrintReceiptChoice Choice { get; private set; } = PrintReceiptChoice.Cancelled;

        public PrintReceiptWindow(string receiptNumber, bool showDigitalOption = false)
        {
            InitializeComponent();
            ReceiptNumberTxt.Text = $"Receipt: {receiptNumber}";

            if (showDigitalOption)
                SendDigitalRadio.Visibility = System.Windows.Visibility.Visible;
        }

        private void ContinueBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SendDigitalRadio.IsChecked == true)
                Choice = PrintReceiptChoice.SendDigital;
            else if (PreviewRadio.IsChecked == true)
                Choice = PrintReceiptChoice.Preview;
            else
                Choice = PrintReceiptChoice.Print;

            DialogResult = true;
            Close();
        }


        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Choice = PrintReceiptChoice.Cancelled;
            Close();
        }
    }
}