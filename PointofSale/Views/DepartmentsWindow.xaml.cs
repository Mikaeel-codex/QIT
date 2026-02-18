using PointofSale.Data;
using PointofSale.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace PointofSale.Views
{
    public partial class DepartmentsWindow : Window
    {
        private readonly ObservableCollection<Department> _items = new();
        private Department? Selected => Grid.SelectedItem as Department;

        public DepartmentsWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData(string? search = null)
        {
            using var db = new AppDbContext();
            var q = db.Departments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                q = q.Where(d => d.Name.ToLower().Contains(search)
                              || (d.Code != null && d.Code.ToLower().Contains(search)));
            }

            var list = q.OrderBy(d => d.Name).ToList();

            _items.Clear();
            foreach (var d in list) _items.Add(d);

            Grid.ItemsSource = _items;
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
            => LoadData(SearchBox.Text);

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var win = new DepartmentEditWindow();
            win.Owner = this;
            if (win.ShowDialog() == true) LoadData(SearchBox.Text);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null) { MessageBox.Show("Select a department first."); return; }

            var win = new DepartmentEditWindow(Selected.Id);
            win.Owner = this;
            if (win.ShowDialog() == true) LoadData(SearchBox.Text);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Selected == null) { MessageBox.Show("Select a department first."); return; }

            if (MessageBox.Show($"Delete '{Selected.Name}'?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            using var db = new AppDbContext();
            var entity = db.Departments.FirstOrDefault(x => x.Id == Selected.Id);
            if (entity == null) return;

            db.Departments.Remove(entity);
            db.SaveChanges();
            LoadData(SearchBox.Text);
        }

        private void Grid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => Edit_Click(sender, e);
    }
}
