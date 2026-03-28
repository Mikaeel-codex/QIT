

namespace PointofSale.Models
{
    public class SaleItem
    {
        public int Id { get; set; }

        public int SaleId { get; set; }
        public Sale Sale { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";
        public string SKU { get; set; } = "";

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public decimal LineTotal { get; set; }
        public decimal DiscountPct { get; set; }

        /// <summary>Reason for return. Empty for normal sale lines.</summary>
        public string ReturnReason { get; set; } = "";

        /// <summary>True when this line is a return (negative quantity).</summary>
        public bool IsReturn => Quantity < 0;
    }
}
