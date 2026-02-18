using System.ComponentModel.DataAnnotations;

namespace PointofSale.Models
{
    public class Department
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = "";

        [MaxLength(20)]
        public string? Code { get; set; }

        [MaxLength(20)]
        public string? TaxCode { get; set; }

        public decimal MarginPercent { get; set; }
        public decimal MarkupPercent { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
