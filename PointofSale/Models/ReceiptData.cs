using System;
using System.Collections.Generic;

namespace PointofSale.Models
{
    /// <summary>
    /// Snapshot of a completed sale — passed to ReceiptPdfService to generate the PDF.
    /// </summary>
    public class ReceiptData
    {
        // ── Store info ────────────────────────────────────────────────────
        public string StoreName { get; set; } = "My Store";
        public string StoreAddress { get; set; } = "";
        public string StorePhone { get; set; } = "";
        public string StoreEmail { get; set; } = "";

        // ── Receipt meta ──────────────────────────────────────────────────
        public string ReceiptNumber { get; set; } = "";
        public DateTime SaleDate { get; set; } = DateTime.Now;
        public string Cashier { get; set; } = "";

        // ── Customer ──────────────────────────────────────────────────────
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerEmail { get; set; } = "";

        // ── Line items ────────────────────────────────────────────────────
        public List<ReceiptLineItem> Lines { get; set; } = new();

        // ── Totals ────────────────────────────────────────────────────────
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        // ── Payment splits ────────────────────────────────────────────────
        public List<ReceiptPaymentLine> Payments { get; set; } = new();

        public decimal AmountDue { get; set; }
        public decimal CashChange { get; set; }
    }

    public class ReceiptLineItem
    {
        public string SKU { get; set; } = "";
        public string Name { get; set; } = "";
        public string Attribute { get; set; } = "";
        public string Size { get; set; } = "";
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string TaxCode { get; set; } = "";
        public decimal DiscountPct { get; set; }
    }

    public class ReceiptPaymentLine
    {
        public string Label { get; set; } = "";
        public decimal Amount { get; set; }
    }
}