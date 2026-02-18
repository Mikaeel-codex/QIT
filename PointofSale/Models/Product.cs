using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace PointofSale.Models
{
    public class Product
    {
        public int Id { get; set; }


        public string Name { get; set; } = "";
        public string SKU { get; set; } = "";
        public decimal Price { get; set; }
        public int StockQty { get; set; }

        public string? Department { get; set; }
        public string? Description { get; set; }
        public string? Size { get; set; }
        public decimal AvgUnitCost { get; set; }
        public string? Tax { get; set; }

        public string? ALU { get; set; }

        public string? Supplier { get; set; }
        public decimal OrderCost { get; set; }
        public int ReorderPoint { get; set; }
        public string? UnitOfMeasure { get; set; }
        public string? Manufacturer { get; set; }
        public string? Comments { get; set; }
    }
}