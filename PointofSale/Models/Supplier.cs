using System.ComponentModel.DataAnnotations;

namespace PointofSale.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = "";

        [MaxLength(60)]
        public string? Code { get; set; }

        [MaxLength(25)]
        public string? Phone { get; set; }

        [MaxLength(120)]
        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
