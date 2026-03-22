namespace PointofSale.Models
{
    /// <summary>
    /// Returned by ScanSkuAndAddToCart when the product stock
    /// will fall to or below its reorder point after this sale.
    /// </summary>
    public class LowStockWarning
    {
        public string ProductName { get; set; } = "";
        public int CurrentStock { get; set; }
        public int CartQty { get; set; }
        public int ReorderPoint { get; set; }
        public int Remaining { get; set; }
    }
}