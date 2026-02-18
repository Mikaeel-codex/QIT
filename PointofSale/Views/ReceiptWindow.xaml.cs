using System.Windows;
using System.Windows.Controls;
using System.Printing;

namespace PointofSale.Views
{
    public partial class ReceiptWindow : Window
    {
        public ReceiptWindow(object vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // user cancelled
            Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true; // user confirmed
            Close();
        }

    }
}
