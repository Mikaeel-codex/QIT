using System;
using System.Collections.Generic;

namespace PointofSale.Models
{
    /// <summary>Snapshot of a quote passed to QuotePrinter and SendQuoteWindow.</summary>
    public class QuoteData
    {
        public string QuoteNumber { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ExpiresAt { get; set; }
        public string CreatedBy { get; set; } = "";

        // Store info
        public string StoreName { get; set; } = "";
        public string StoreAddress { get; set; } = "";
        public string StorePhone { get; set; } = "";

        // Customer
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerEmail { get; set; } = "";

        // Totals
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        // Notes
        public string Notes { get; set; } = "";

        // Lines
        public List<QuoteLineData> Lines { get; set; } = new();
    }

    public class QuoteLineData
    {
        public string Name { get; set; } = "";
        public string SKU { get; set; } = "";
        public string Size { get; set; } = "";
        public string Attribute { get; set; } = "";
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public decimal DiscountPct { get; set; }
        public string TaxCode { get; set; } = "";
    }
}