using System;
using System.ComponentModel.DataAnnotations;

namespace PointofSale.Models
{
    public class GiftCard
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CardNumber { get; set; } = "";

        public decimal Balance { get; set; } = 0m;
        public decimal IssuedValue { get; set; } = 0m;   // original amount when purchased

        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public DateTime? LastUsedAt { get; set; }

        // "Active" | "Depleted" | "Voided"
        public string Status { get; set; } = "Active";

        public string IssuedBy { get; set; } = "";   // cashier who issued it
    }
}