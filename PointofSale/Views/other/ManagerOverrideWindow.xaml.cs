using PointofSale.Services;
using System.Windows;
using System.Windows.Input;

namespace PointofSale.Views
{
    /// <summary>
    /// Prompts for admin/manager credentials to authorize a restricted action.
    /// Set <see cref="Authorized"/> to true when credentials are verified.
    /// </summary>
    public partial class ManagerOverrideWindow : Window
    {
        /// <summary>True if a qualifying manager/admin successfully authenticated.</summary>
        public bool Authorized { get; private set; } = false;

        private readonly string _requiredPermission;

        /// <param name="reason">One-line description shown to the user, e.g. "Void Sale — Receipt #1042"</param>
        /// <param name="requiredPermission">
        ///   Permission key the authorizing user must hold (e.g. "VoidSales").
        ///   Pass null to require Admin/Co-Admin only.
        /// </param>
        public ManagerOverrideWindow(string reason, string? requiredPermission = null)
        {
            InitializeComponent();
            ReasonTxt.Text = $"A manager or admin must authorize this action:\n{reason}";
            _requiredPermission = requiredPermission ?? "";
        }

        private void Authorize_Click(object sender, RoutedEventArgs e) => TryAuthorize();
        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Field_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TryAuthorize();
        }

        private void TryAuthorize()
        {
            ErrorTxt.Visibility = Visibility.Collapsed;

            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter your username and password.");
                return;
            }

            var auth = new AuthService();
            var user = auth.Login(username, password);

            if (user == null)
            {
                ShowError("Invalid username or password.");
                PasswordBox.Clear();
                PasswordBox.Focus();
                return;
            }

            // Must be Admin/Co-Admin, OR hold the specific permission
            bool allowed = user.IsAdmin ||
                           (!string.IsNullOrEmpty(_requiredPermission) && user.GetPermissions()
                               .Any(p => p == _requiredPermission));

            if (!allowed)
            {
                ShowError($"{user.Username} does not have permission to authorize this action.");
                PasswordBox.Clear();
                PasswordBox.Focus();
                return;
            }

            Authorized = true;
            Close();
        }

        private void ShowError(string msg)
        {
            ErrorTxt.Text = msg;
            ErrorTxt.Visibility = Visibility.Visible;
        }
    }
}
