using PointofSale.Services;
using System.Windows;

namespace PointofSale
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _authService = new AuthService();

        public LoginWindow()
        {
            InitializeComponent();
            _authService.EnsureSeedUsers();
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Visibility = Visibility.Collapsed;

            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter both username and password.");
                return;
            }

            var user = _authService.Login(username, password);
            if (user == null)
            {
                ShowError("Invalid username or password.");
                return;
            }

            Session.SetUser(user);

            DialogResult = true;
            Close();
        }

        private void ShowError(string msg)
        {
            ErrorText.Text = msg;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}