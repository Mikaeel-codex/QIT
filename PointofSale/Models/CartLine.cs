using System.ComponentModel;

namespace PointofSale.Models
{
    public class CartLine : INotifyPropertyChanged
    {
        // ── backing fields ───────────────────────────────────────────────
        private int _qty;
        private decimal _unitPrice;
        private string _name = "";
        private string _attribute = "";
        private string _size = "";
        private string _taxCode = "";
        private decimal _taxRate = 0m;

        // ── read-only (no editing) ───────────────────────────────────────
        public int ProductId { get; set; }
        public string SKU { get; set; } = "";
        public decimal CostPrice { get; set; } = 0m;
        public int StockQty { get; set; } = 0;    // Stock on hand snapshot

        // ── return tracking ──────────────────────────────────────────────
        public bool IsReturn { get; set; } = false;
        public string ReturnReceiptNo { get; set; } = "";
        public string ReturnReason { get; set; } = "";

        // ── discount tracking ────────────────────────────────────────────
        private decimal _originalPrice = 0m;
        private decimal _discountPct = 0m;

        /// <summary>The price before any discount was applied. 0 means no discount set.</summary>
        public decimal OriginalPrice
        {
            get => _originalPrice;
            set { _originalPrice = value; OnPropertyChanged(nameof(OriginalPrice)); OnPropertyChanged(nameof(HasDiscount)); OnPropertyChanged(nameof(DiscountVisibility)); }
        }

        /// <summary>Discount percentage e.g. 15 means 15% off.</summary>
        public decimal DiscountPct
        {
            get => _discountPct;
            set { _discountPct = value; OnPropertyChanged(nameof(DiscountPct)); OnPropertyChanged(nameof(HasDiscount)); OnPropertyChanged(nameof(DiscountVisibility)); }
        }

        public bool HasDiscount => DiscountPct > 0 || (OriginalPrice > 0 && OriginalPrice != UnitPrice);

        public System.Windows.Visibility DiscountVisibility =>
            HasDiscount ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        // ── editable fields ──────────────────────────────────────────────
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Attribute
        {
            get => _attribute;
            set { _attribute = value; OnPropertyChanged(nameof(Attribute)); }
        }

        public string Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(nameof(Size)); }
        }

        public string TaxCode
        {
            get => _taxCode;
            set { _taxCode = value; OnPropertyChanged(nameof(TaxCode)); }
        }

        public decimal TaxRate
        {
            get => _taxRate;
            set { _taxRate = value; OnPropertyChanged(nameof(TaxRate)); OnPropertyChanged(nameof(LineTotal)); }
        }

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

        // ── INotifyPropertyChanged ───────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}