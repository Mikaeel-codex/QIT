// ═══════════════════════════════════════════════════════
//  SECTION 3 — VIEWMODEL PROPERTIES
//  Add these to your MainViewModel (or MainWindow DataContext)
// ═══════════════════════════════════════════════════════

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PointofSale.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // ── Backing fields ────────────────────────────────────────────────
        private string _loggedInUserName = "Guest";
        private string _loggedInUserRole = "";
        private string _loggedInUserInitials = "?";
        private string _loggedInUserFullName = "";
        private bool _isProfileOpen = false;

        // ── Bindable properties ───────────────────────────────────────────

        /// <summary>Display name shown in the navbar pill and dropdown header.</summary>
        public string LoggedInUserName
        {
            get => _loggedInUserName;
            set { _loggedInUserName = value; OnPropertyChanged(); }
        }

        /// <summary>Role label shown under the name (e.g. "Admin", "Cashier").</summary>
        public string LoggedInUserRole
        {
            get => _loggedInUserRole;
            set { _loggedInUserRole = value; OnPropertyChanged(); }
        }

        /// <summary>One or two initials shown in the avatar circle.</summary>
        public string LoggedInUserInitials
        {
            get => _loggedInUserInitials;
            set { _loggedInUserInitials = value; OnPropertyChanged(); }
        }

        /// <summary>Controls whether the profile popup is open.</summary>
        public bool IsProfileOpen
        {
            get => _isProfileOpen;
            set { _isProfileOpen = value; OnPropertyChanged(); }
        }

        // ── Helper: call this after a successful login ────────────────────

        /// <summary>
        /// Populates the profile dropdown from a logged-in AppUser.
        /// Call this from ApplyPermissions() after Session.CurrentUser is set.
        /// </summary>
        public void SetLoggedInUser(Models.AppUser user)
        {
            if (user == null)
            {
                LoggedInUserName = "Sign In";
                LoggedInUserRole = "";
                LoggedInUserInitials = "?";
                return;
            }

            // Use FullName if your AppUser model has it, else fall back to Username
            var displayName = user.Username;

            LoggedInUserName = displayName;
            LoggedInUserRole = user.Role;
            LoggedInUserInitials = BuildInitials(displayName);
        }

        public void ClearLoggedInUser()
        {
            LoggedInUserName = "Sign In";
            LoggedInUserRole = "";
            LoggedInUserInitials = "?";
            IsProfileOpen = false;
        }

        // ── Initials builder ──────────────────────────────────────────────

        private static string BuildInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";

            var parts = name.Trim().Split(' ',
                System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
                return parts[0][0].ToString().ToUpper();

            // First + last initial
            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}


// ═══════════════════════════════════════════════════════
//  SECTION 4 — HOW TO WIRE IT IN MainWindow.xaml.cs
//  Replace or update your ApplyPermissions() method
// ═══════════════════════════════════════════════════════

/*

private readonly MainViewModel _vm = new();

public MainWindow()
{
    InitializeComponent();
    DataContext = _vm;
    ApplyPermissions();
    StartClock();
}

private void ApplyPermissions()
{
    var user = Session.CurrentUser;

    if (user == null)
    {
        _vm.ClearLoggedInUser();
        SetEnabled(false, MakeSaleBtn, ...);
        UpdateButtonVisuals();
        return;
    }

    // ← This is the key call — populates the dropdown automatically
    _vm.SetLoggedInUser(user);

    bool isAdmin = user.Role == "Admin";
    SetEnabled(true,  MakeSaleBtn, HeldReceiptsBtn, EndOfDayBtn_Tile);
    SetEnabled(isAdmin, ProductsBtn_Tile, ReportsBtn_Tile, ...);
    UpdateButtonVisuals();
}

// Dropdown click handlers in MainWindow.xaml.cs:

private void ManageAccount_Click(object sender, RoutedEventArgs e)
{
    _vm.IsProfileOpen = false;
    // Open manage account window
    MessageBox.Show("Manage Account coming soon.");
}

private void MyAccount_Click(object sender, RoutedEventArgs e)
{
    _vm.IsProfileOpen = false;
    // Open my account / profile window
    MessageBox.Show("My Account coming soon.");
}

private void SignOut_Click(object sender, RoutedEventArgs e)
{
    _vm.IsProfileOpen = false;
    Session.Logout();
    _vm.ClearLoggedInUser();
    ApplyPermissions();
}

*/


// ═══════════════════════════════════════════════════════
//  SECTION 5 — AppUser model: add FullName if not present
//  Your AppUser model likely just has Username + Role.
//  Add FullName so the dropdown shows "James Davidson"
//  instead of just the login username.
// ═══════════════════════════════════════════════════════

/*

// In Models/AppUser.cs — add this property:
public string? FullName { get; set; }

// In your employee creation form — save the employee's
// full name to AppUser.FullName when the admin creates them.
// Then SetLoggedInUser() will automatically display it.

*/