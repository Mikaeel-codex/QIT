namespace PointofSale.Models
{
    /// <summary>Key-value store for all configurable settings (store info, email credentials, etc.).</summary>
    public class StoreSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }
}