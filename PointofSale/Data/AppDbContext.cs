using Microsoft.EntityFrameworkCore;
using PointofSale.Models;
using System.IO;

namespace PointofSale.Data
{
    public class AppDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PointofSale");

            Directory.CreateDirectory(folder);

            var dbPath = Path.Combine(folder, "pos.db");

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        public DbSet<PointofSale.Models.Product> Products { get; set; }
        public DbSet<PointofSale.Models.AppUser> Users { get; set; }
        public DbSet<PointofSale.Models.Sale> Sales { get; set; }
        public DbSet<PointofSale.Models.SaleItem> SaleItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().Property(p => p.Price).HasConversion<double>();
            modelBuilder.Entity<Sale>().Property(s => s.Total).HasConversion<double>();
            modelBuilder.Entity<SaleItem>().Property(si => si.UnitPrice).HasConversion<double>();
            modelBuilder.Entity<SaleItem>().Property(si => si.LineTotal).HasConversion<double>();
        }
    }
}
