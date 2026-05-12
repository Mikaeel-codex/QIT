using PointofSale.Models;
using PointofSale.Services;
using PointofSale.ViewModels;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PointofSale.Views
{
    public partial class ProductsWindow : Window
    {
        private ProductsViewModel Vm => (ProductsViewModel)DataContext;
        private Product? SelectedProduct => Vm.SelectedProduct;

        public ProductsWindow()
        {
            InitializeComponent();
            DataContext = new ProductsViewModel();

            if (!(Session.CurrentUser?.CanManageInventory ?? false))
            {
                AddBtn.Visibility    = Visibility.Collapsed;
                EditBtn.Visibility   = Visibility.Collapsed;
                DeleteBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
            => Vm.LoadProducts();

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            var win = new ProductEditWindow(null) { Owner = this };
            win.ShowDialog();
            if (win.Saved) Vm.LoadProducts();
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            var p = SelectedProduct;
            if (p == null) { MessageBox.Show("Select a product first."); return; }
            var win = new ProductEditWindow(p.Id) { Owner = this };
            win.ShowDialog();
            if (win.Saved) Vm.LoadProducts();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var p = SelectedProduct;
            if (p == null) { MessageBox.Show("Select an item first."); return; }

            if (MessageBox.Show($"Delete '{p.Name}'?", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

            using var db = new Data.AppDbContext();
            var prod = db.Products.FirstOrDefault(x => x.Id == p.Id);
            if (prod == null) { MessageBox.Show("Item not found. Refresh and try again."); return; }

            db.Products.Remove(prod);
            db.SaveChanges();
            Vm.LoadProducts();
        }

        private void ProductsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedProduct != null && (Session.CurrentUser?.CanManageInventory ?? false))
                EditBtn_Click(sender, e);
        }
    }
}
