using PointofSale.Data;
using PointofSale.Models;
using PointofSale.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace PointofSale.ViewModels
{
    public class ProductsViewModel : INotifyPropertyChanged
    {
        private string _searchText = "";
        private Product? _selectedProduct;

        public ObservableCollection<Product> Items { get; } = new();
        public ICollectionView View { get; }

        public bool CanManage => Session.CurrentUser?.CanManageInventory ?? false;

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); View.Refresh(); }
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set { _selectedProduct = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusMessage)); }
        }

        public string StatusMessage => _selectedProduct == null
            ? "Ready."
            : $"Selected: {_selectedProduct.Name}  |  SKU: {_selectedProduct.SKU}  |  Stock: {_selectedProduct.StockQty}";

        public ProductsViewModel()
        {
            View = CollectionViewSource.GetDefaultView(Items);
            View.Filter = FilterItem;
            LoadProducts();
        }

        public void LoadProducts()
        {
            using var db = new AppDbContext();
            var list = db.Products.OrderBy(p => p.Name).ToList();
            Items.Clear();
            foreach (var p in list) Items.Add(p);
        }

        private bool FilterItem(object obj)
        {
            if (obj is not Product p) return false;
            var term = _searchText.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(term)) return true;
            return (p.Name        ?? "").ToLower().Contains(term)
                || (p.SKU         ?? "").ToLower().Contains(term)
                || (p.Department  ?? "").ToLower().Contains(term)
                || (p.Description ?? "").ToLower().Contains(term)
                || (p.ALU         ?? "").ToLower().Contains(term)
                || p.Id.ToString().Contains(term);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
