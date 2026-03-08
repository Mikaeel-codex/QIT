using System.ComponentModel;

namespace PointofSale.Models
{
    public class CartLine : INotifyPropertyChanged
    {
        private int _qty;
        private decimal _unitPrice;

        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string SKU { get; set; } = "";
        public string Attribute { get; set; } = "";
        public string Size { get; set; } = "";
        public string TaxCode { get; set; } = "";

        // Stored as a percentage e.g. 15 means 15%, 0 means no tax
        public decimal TaxRate { get; set; } = 0m;

        public int Qty
        {
            get => _qty;
            set { _qty = value; OnPropertyChanged(nameof(Qty)); OnPropertyChanged(nameof(LineTotal)); }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set { _unitPrice = value; OnPropertyChanged(nameof(UnitPrice)); OnPropertyChanged(nameof(LineTotal)); }
        }

        public decimal LineTotal => System.Math.Round(Qty * UnitPrice, 2);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}