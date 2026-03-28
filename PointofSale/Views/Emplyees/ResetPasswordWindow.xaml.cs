using PointofSale.Data;
using PointofSale.Services;
using System.Linq;
using System.Windows;

namespace PointofSale.Views
{
    public partial class ResetPasswordWindow : Window
    {
        private readonly int _userId;
        public bool Saved { get; private set; } = false;

        public ResetPasswordWindow(int userId, string displayName)
        {
            InitializeComponent();
            _userId = userId;
            HeaderTxt.Text = $"Reset password for: {displayName}";
        }

        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            var newPass = NewPassBox.Password;
            var confirmPass = ConfirmPassBox.Password;

            if (string.IsNullOrWhiteSpace(newPass))
            { ShowError("Please enter a new password."); return; }

            if (newPass.Length < 4)
            { ShowError("Password must be at least 4 characters."); return; }

            if (newPass != confirmPass)
            { ShowError("Passwords do not match."); return; }

            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.Id == _userId);
            if (user == null) { ShowError("User not found."); return; }

            var auth = new AuthService();
            auth.SetPassword(user, newPass);
            db.SaveChanges();

            Saved = true;
            MessageBox.Show("Password reset successfully.", "Done",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void ShowError(string msg)
        {
            ErrorTxt.Text = msg;
            ErrorTxt.Visibility = Visibility.Visible;
        }
    }
}