using System.ComponentModel.DataAnnotations;

namespace PointofSale.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string SKU { get; set; } = "";
        public decimal Price { get; set; }
        public int StockQty { get; set; }
    }
}