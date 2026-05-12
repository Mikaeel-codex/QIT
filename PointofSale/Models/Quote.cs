using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PointofSale.Models
{
    // ── Quote status values ───────────────────────────────────────────────
    // Draft     — created, not yet submitted for approval
    // Pending   — submitted, waiting for admin approval to print/send
    // Approved  — admin approved, can be printed/sent
    // Accepted  — customer accepted, cashier can process at POS
    // Rejected  — admin rejected
    // Expired   — past expiry date
    // Converted — converted to a sale

    public class Quote
    {
        [Key]
        public int Id { get; set; }

        public string QuoteNumber { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ExpiresAt { get; set; } = DateTime.Now.AddDays(30);

        // ── Status ────────────────────────────────────────────────────────
        public string Status { get; set; } = "Draft";
        // Draft | Pending | Approved | Accepted | Rejected | Expired | Converted

        // ── Staff ─────────────────────────────────────────────────────────
        public string CreatedBy { get; set; } = "";  // cashier username
        public string ApprovedBy { get; set; } = "";  // admin username
        public DateTime? ApprovedAt { get; set; }

        // ── Customer ──────────────────────────────────────────────────────
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerEmail { get; set; } = "";

        // ── Totals ────────────────────────────────────────────────────────
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        // ── Notes / terms ─────────────────────────────────────────────────
        public string Notes { get; set; } = "";

        // ── Print audit log ───────────────────────────────────────────────
        // Stored as pipe-separated entries: "2024-01-01 10:00|admin|Print"
        public string PrintLog { get; set; } = "";

        // ── Conversion ────────────────────────────────────────────────────
        public int? ConvertedToSaleId { get; set; }

        // ── Line items ────────────────────────────────────────────────────
        public List<QuoteItem> Items { get; set; } = new();

        // ── Computed ──────────────────────────────────────────────────────
        [NotMapped]
        public bool IsExpired => DateTime.Now > ExpiresAt && Status != "Converted";

        [NotMapped]
        public string StatusDisplay => IsExpired && Status == "Approved" ? "Expired" : Status;
    }

    public class QuoteItem
    {
        [Key]
        public int Id { get; set; }

        public int QuoteId { get; set; }
        public Quote Quote { get; set; } = null!;

        public int ProductId { get; set; }
        public string SKU { get; set; } = "";
        public string Name { get; set; } = "";
        public string Size { get; set; } = "";
        public string Attribute { get; set; } = "";
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string TaxCode { get; set; } = "";
        public decimal DiscountPct { get; set; }
    }
}