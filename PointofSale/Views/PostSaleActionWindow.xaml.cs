using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PointofSale.Views
{
    public partial class PostSaleActionWindow : Window
    {
        public enum ReceiptAction
        {
            None,
            Print,
            WhatsApp,
            Email
        }

        public ReceiptAction SelectedAction { get; private set; } = ReceiptAction.None;

        public PostSaleActionWindow()
        {
            InitializeComponent();
        }

        private void Option_Checked(object sender, RoutedEventArgs e)
        {
            if (PrintRb.IsChecked == true)
            {
                SelectedAction = ReceiptAction.Print;
                HintText.Text = "You will choose a printer next.";
            }
            else if (WhatsAppRb.IsChecked == true)
            {
                SelectedAction = ReceiptAction.WhatsApp;
                HintText.Text = "We will open WhatsApp with the receipt text.";
            }
            else if (EmailRb.IsChecked == true)
            {
                SelectedAction = ReceiptAction.Email;
                HintText.Text = "We will open your email app with a draft message.";
            }

            ContinueBtn.IsEnabled = SelectedAction != ReceiptAction.None;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
