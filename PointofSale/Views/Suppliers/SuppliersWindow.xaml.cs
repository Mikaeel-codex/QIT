using PointofSale.Data;
using PointofSale.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace PointofSale.Views
{
    public partial class SuppliersWindow : Window
    {
        private readonly ObservableCollection<Supplier> _items = new();

        private Supplier? Selected => Grid.SelectedItem as Supplier;

        public SuppliersWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData(string? search = null)
        {
            using var db = new AppDbContext();

            var q = db.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                q = q.Where(s => s.Name.ToLower().Contains(search)
                              || (s.Code != null && s.Code.ToLower().Contains(search)));
            }

            var list = q.OrderBy(s => s.Name).ToList();

            _items.Clear();
            foreach (var s in list) _items.Add(s);

            Grid.ItemsSource = _items;
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
            => LoadData(SearchBox.Text);

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var win = new SupplierEditWindow();
            win.Owner = this;
            if (win.ShowDialog() == true) LoadData(SearchBox.Text);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null) { MessageBox.Show("Select a supplier first."); return; }

            var win = new SupplierEditWindow(Selected.Id);
            win.Owner = this;
            if (win.ShowDialog() == true) LoadData(SearchBox.Text);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null) { MessageBox.Show("Select a supplier first."); return; }

            if (MessageBox.Show($"Delete '{Selected.Name}'?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            using var db = new AppDbContext();
            var entity = db.Suppliers.FirstOrDefault(x => x.Id == Selected.Id);
            if (entity == null) return;

            db.Suppliers.Remove(entity);
            db.SaveChanges();
            LoadData(SearchBox.Text);
        }

        private void Grid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => Edit_Click(sender, e);
    }
}
