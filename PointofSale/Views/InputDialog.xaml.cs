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
    public partial class InputDialog : Window
    {
        public string Value { get; private set; } = "";

        public InputDialog(string title, string placeholder = "")
        {
            InitializeComponent();
            TitleText.Text = title;
            ValueBox.Text = placeholder;
            ValueBox.SelectAll();
            ValueBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Value = ValueBox.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
