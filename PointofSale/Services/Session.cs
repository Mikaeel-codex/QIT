using PointofSale.Models;


namespace PointofSale.Services
{
    public static class Session
    {
        public static AppUser? CurrentUser { get; private set; }

        public static bool IsAdmin => CurrentUser != null && CurrentUser.Role == "Admin";

        public static void SetUser(AppUser user) => CurrentUser = user;

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}