using PointofSale.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PointofSale.Views
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _auth = new();

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => UsernameBox.Focus();
        }

        // ── Secret dev panel trigger — Ctrl+Shift+D ───────────────────────
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.D &&
                Keyboard.IsKeyDown(Key.LeftCtrl) &&
                Keyboard.IsKeyDown(Key.LeftShift))
            {
                OpenDevPanel();
                e.Handled = true;
            }
        }

        private void OpenDevPanel()
        {
            // Prompt for dev secret code
            var code = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter developer access code:",
                "Developer Access", "");

            if (string.IsNullOrWhiteSpace(code)) return;

            if (!DevSettings.VerifyDevCode(code))
            {
                // Silently ignore wrong code — no error shown to client
                return;
            }

            new DevPanelWindow { Owner = this }.ShowDialog();
        }

        // ── Drag to move ──────────────────────────────────────────────────
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
            => Close();

        // ── Floating label animation ──────────────────────────────────────
        private void AnimateLabel(ScaleTransform scale, TranslateTransform translate,
                                  bool up, TextBlock lbl)
        {
            var dur = new Duration(TimeSpan.FromMilliseconds(180));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,
                new DoubleAnimation(up ? 0.82 : 1.0, dur));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,
                new DoubleAnimation(up ? 0.82 : 1.0, dur));
            translate.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(up ? -22 : 0, dur));
            lbl.Foreground = up
                ? new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x5E, 0x7E, 0xB6))
                : new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x9A, 0xAB, 0xB8));
        }

        private void Field_GotFocus(object sender, RoutedEventArgs e)
            => AnimateLabel(UsernameLblScale, UsernameLblTranslate, true, UsernameLbl);

        private void Field_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameBox.Text))
                AnimateLabel(UsernameLblScale, UsernameLblTranslate, false, UsernameLbl);
        }

        private void PassField_GotFocus(object sender, RoutedEventArgs e)
            => AnimateLabel(PasswordLblScale, PasswordLblTranslate, true, PasswordLbl);

        private void PassField_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
                AnimateLabel(PasswordLblScale, PasswordLblTranslate, false, PasswordLbl);
        }

        private void Field_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) AttemptLogin();
        }

        private void ForgotLink_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Please contact your system administrator to reset your password.",
                "Forgot Password", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
            => AttemptLogin();

        private void AttemptLogin()
        {
            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter your username and password.");
                return;
            }

            try
            {
                var user = _auth.Login(username, password);
                if (user == null)
                {
                    ShowError("Incorrect username or password.");
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                    return;
                }

                Session.SetUser(user);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError($"Login error: {ex.Message}");
            }
        }

        private void ShowError(string msg)
        {
            ErrorTxt.Text = msg;
            ErrorTxt.Visibility = Visibility.Visible;
        }
    }
}