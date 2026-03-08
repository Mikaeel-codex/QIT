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
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<HeldReceipt> HeldReceipts { get; set; }
        public DbSet<HeldReceiptItem> HeldReceiptItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().Property(p => p.Price).HasConversion<double>();
            modelBuilder.Entity<Sale>().Property(s => s.Total).HasConversion<double>();
            modelBuilder.Entity<SaleItem>().Property(si => si.UnitPrice).HasConversion<double>();
            modelBuilder.Entity<SaleItem>().Property(si => si.LineTotal).HasConversion<double>();
            modelBuilder.Entity<Supplier>().HasIndex(s => s.Name).IsUnique();
            modelBuilder.Entity<Department>().HasIndex(d => d.Name).IsUnique();

            // HeldReceipt conversions
            modelBuilder.Entity<HeldReceipt>().Property(h => h.Subtotal).HasConversion<double>();
            modelBuilder.Entity<HeldReceipt>().Property(h => h.Tax).HasConversion<double>();
            modelBuilder.Entity<HeldReceipt>().Property(h => h.Total).HasConversion<double>();
            modelBuilder.Entity<HeldReceiptItem>().Property(i => i.UnitPrice).HasConversion<double>();
            modelBuilder.Entity<HeldReceiptItem>().Property(i => i.LineTotal).HasConversion<double>();
            modelBuilder.Entity<HeldReceiptItem>().Property(i => i.TaxRate).HasConversion<double>();

            modelBuilder.Entity<HeldReceiptItem>()
                .HasOne(i => i.HeldReceipt)
                .WithMany(h => h.Items)
                .HasForeignKey(i => i.HeldReceiptId);
        }
    }
}
