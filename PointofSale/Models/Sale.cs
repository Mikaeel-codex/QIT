using System;
using System.Collections.Generic;

namespace PointofSale.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public DateTime saleDate { get; set; } = DateTime.Now;
        public string CashierUsername { get; set; } = "";
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public List<SaleItem> Items { get; set; } = new();
    }
}
