using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PointofSale.Models
{
    public class Sale
    {
        [Key]
        public int Id { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.Now;
        public string ReceiptNumber { get; set; } = "";
        public string Cashier { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = "";  // e.g. "Cash" or "Cash + EFT"
        public string Status { get; set; } = "Completed";

        public List<SaleItem> Items { get; set; } = new();
    }
}