using PointofSale.Helpers;
using System.Security.AccessControl;

namespace PointofSale.Models
{
    public class CartLine : ObservableObject
    {
        public int ProductId { get; set; }

        private string _name = "";
        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        private string _sku = "";
        public string SKU
        {
            get => _sku;
            set => Set(ref _sku, value);
        }

        private decimal _unitPrice;
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (Set(ref _unitPrice, value))
                    OnPropertyChanged(nameof(LineTotal));

            }
        }

        private int _qty;
        public int Qty
        {
            get => _qty;
            set
            {
                if (Set(ref _qty, value))
                    OnPropertyChanged(nameof(LineTotal));
            }
        }

        public decimal LineTotal => UnitPrice * Qty;
    }
}
