using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PointofSale.Models
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        [Required]
        public string PasswordSalt { get; set; } = "";

        // ── Identity ──────────────────────────────────────────────────────
        [MaxLength(100)]
        public string FullName { get; set; } = "";

        [MaxLength(20)]
        public string Role { get; set; } = "Cashier";
        // Roles: Admin | Co-Admin | Manager | Supervisor | Stock Controller | Cashier

        public bool IsActive { get; set; } = true;

        // ── Contact ───────────────────────────────────────────────────────
        [MaxLength(20)]
        public string Phone { get; set; } = "";

        [MaxLength(100)]
        public string Email { get; set; } = "";

        // ── Permissions (stored as comma-separated flags) ─────────────────
        // e.g. "MakeSales,ViewSalesHistory,ManageCustomers"
        // Admin and Co-Admin always get all permissions regardless of this field.
        public string Permissions { get; set; } = "";

        // ── Computed helpers (not mapped to DB) ───────────────────────────
        [NotMapped]
        public string DisplayName =>
            string.IsNullOrWhiteSpace(FullName) ? Username : FullName;

        [NotMapped]
        public bool IsAdmin => Role == "Admin" || Role == "Co-Admin";

        // Individual permission checks
        [NotMapped] public bool CanMakeSales => IsAdmin || HasPerm("MakeSales");
        [NotMapped] public bool CanViewSalesHistory => IsAdmin || HasPerm("ViewSalesHistory");
        [NotMapped] public bool CanVoidSales => IsAdmin || HasPerm("VoidSales");
        [NotMapped] public bool CanManageInventory => IsAdmin || HasPerm("ManageInventory");
        [NotMapped] public bool CanViewInventory => IsAdmin || HasPerm("ViewInventory") || HasPerm("ManageInventory");
        [NotMapped] public bool CanManageCustomers => IsAdmin || HasPerm("ManageCustomers");
        [NotMapped] public bool CanManageSuppliers => IsAdmin || HasPerm("ManageSuppliers");
        [NotMapped] public bool CanViewReports => IsAdmin || HasPerm("ViewReports");
        [NotMapped] public bool CanAccessSettings => IsAdmin || HasPerm("AccessSettings");
        [NotMapped] public bool CanManageEmployees => IsAdmin || HasPerm("ManageEmployees");

        private bool HasPerm(string perm) =>
            !string.IsNullOrWhiteSpace(Permissions) &&
            Permissions.Split(',').Any(p => p.Trim() == perm);

        public void SetPermissions(IEnumerable<string> perms)
            => Permissions = string.Join(",", perms);

        public IEnumerable<string> GetPermissions()
            => string.IsNullOrWhiteSpace(Permissions)
               ? Enumerable.Empty<string>()
               : Permissions.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0);
    }
}