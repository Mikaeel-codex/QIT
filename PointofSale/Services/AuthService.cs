using PointofSale.Data;
using PointofSale.Models;
using System.Security.Cryptography;

namespace PointofSale.Services
{
    class AuthService
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
            
            //Admin
            CreateUser (db, "admin", "admin123", "Admin");

            //Cashier
            CreateUser (db, "cashier", "cashier123", "Cashier");

            db.SaveChanges();
        }

        public void CreateUser(AppDbContext db, string username, string password, string role)
        {
           var (salt, hash) = HashPassword(password);

            db.Users.Add(new AppUser
            {
                Username = username,
                PasswordSalt = salt,
                PasswordHash = hash,
                Role = role,
                IsActive = true
            });
        }

        private (string salt, string hash) HashPassword(string password)
        {
            byte[] saltbytes = RandomNumberGenerator.GetBytes(16);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltbytes, 100_000, HashAlgorithmName.SHA256);

            byte[] hashbytes = pbkdf2.GetBytes(32);

            return (Convert.ToBase64String(saltbytes), Convert.ToBase64String(hashbytes));
        }

        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            byte[] saltbytes = Convert.FromBase64String(storedSalt);
            byte[] storedHashBytes = Convert.FromBase64String(storedHash);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltbytes, 100_000, HashAlgorithmName.SHA256);

            byte[] computedHashBytes = pbkdf2.GetBytes(32);

            return storedHashBytes.SequenceEqual(computedHashBytes);
        }
    }
}
