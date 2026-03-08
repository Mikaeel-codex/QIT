using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PointofSale.Models
{
    public class HeldReceipt
    {
        [Key]
        public int Id { get; set; }
        public DateTime HeldAt { get; set; } = DateTime.Now;
        public string Cashier { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
        public int TotalQty { get; set; }

        /// Comma-separated item names for quick preview in the list
        /// e.g. "Pepsi x1, Lays x2"
        public string ItemsSummary { get; set; } = "";

        public List<HeldReceiptItem> Items { get; set; } = new();
    }

    public class HeldReceiptItem
    {
        [Key]
        public int Id { get; set; }
        public int HeldReceiptId { get; set; }
        public int ProductId { get; set; }
        public string SKU { get; set; } = "";
        public string Name { get; set; } = "";
        public string Attribute { get; set; } = "";
        public string Size { get; set; } = "";
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string TaxCode { get; set; } = "";
        public decimal TaxRate { get; set; }

        public HeldReceipt? HeldReceipt { get; set; }
    }
}