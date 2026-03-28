using PointofSale.Data;
using PointofSale.Models;
using System.Security.Cryptography;

namespace PointofSale.Services
{
    public class AuthService
    {
        public AppUser? Login(string username, string password)
        {
            using var db = new AppDbContext();
            var user = db.Users.FirstOrDefault(u => u.Username == username && u.IsActive);
            if (user == null) return null;
            var ok = VerifyPassword(password, user.PasswordHash, user.PasswordSalt);
            return ok ? user : null;
        }

        public void EnsureSeedUsers()
        {
            using var db = new AppDbContext();
            if (db.Users.Any()) return;

            CreateUser(db, "admin", "admin123", "Admin");
            CreateUser(db, "cashier", "cashier123", "Cashier");
            db.SaveChanges();
        }

        public void CreateUser(AppDbContext db, string username, string password, string role)
        {
            var u = new AppUser
            {
                Username = username,
                Role = role,
                IsActive = true
            };
            SetPassword(u, password);
            db.Users.Add(u);
        }

        /// <summary>
        /// Hashes the given password and writes the hash + salt onto the user object.
        /// Call db.SaveChanges() after this if the user is already tracked.
        /// </summary>
        public void SetPassword(AppUser user, string password)
        {
            var (salt, hash) = HashPassword(password);
            user.PasswordSalt = salt;
            user.PasswordHash = hash;
        }

        private (string salt, string hash) HashPassword(string password)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, saltBytes, 100_000, HashAlgorithmName.SHA256);
            byte[] hashBytes = pbkdf2.GetBytes(32);
            return (Convert.ToBase64String(saltBytes), Convert.ToBase64String(hashBytes));
        }

        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] storedHashBytes = Convert.FromBase64String(storedHash);
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, saltBytes, 100_000, HashAlgorithmName.SHA256);
            byte[] computedHashBytes = pbkdf2.GetBytes(32);
            return storedHashBytes.SequenceEqual(computedHashBytes);
        }
    }
}