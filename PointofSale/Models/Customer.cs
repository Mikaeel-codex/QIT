using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PointofSale.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        // ── Basic info ────────────────────────────────────────────────────
        [Required]
        public string FirstName { get; set; } = ""; 
        [Required]
        public string LastName { get; set; } = "";
        [Required]
        public string Phone { get; set; } = "";
        [Required]
        public string Email { get; set; } = "";

        // ── Address ───────────────────────────────────────────────────────
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }

        // ── Account / credit ──────────────────────────────────────────────
        public decimal AccountBalance { get; set; } = 0m;
        public decimal CreditLimit { get; set; } = 0m;

        // ── Notes ─────────────────────────────────────────────────────────
        public string? Notes { get; set; }

        // ── Meta ──────────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // ── Computed (not mapped) ─────────────────────────────────────────
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        [NotMapped]
        public string DisplayLine => $"{FullName} | {Phone}";

        [NotMapped]
        public decimal AvailableCredit => CreditLimit - AccountBalance;
    }
}