using PointofSale.Data;
using PointofSale.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PointofSale.Views
{
    public partial class OrderListWindow : Window
    {
        private List<QuoteRowVm> _open    = new();
        private List<QuoteRowVm> _pending = new();
        private List<QuoteRowVm> _closed  = new();

        public OrderListWindow()
        {
            InitializeComponent();
            LoadOrders();
        }

        // ── Data ──────────────────────────────────────────────────────────
        private void LoadOrders()
        {
            using var db = new AppDbContext();

            var all = db.Quotes
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => new QuoteRowVm
                {
                    Id           = q.Id,
                    QuoteNumber  = q.QuoteNumber,
                    CustomerName = q.CustomerName == "" ? "(No customer)" : q.CustomerName,
                    CreatedAt    = q.CreatedAt,
                    ExpiresAt    = q.ExpiresAt,
                    Total        = q.Total,
                    CreatedBy    = q.CreatedBy,
                    Status       = q.Status,
                    ItemCount    = db.QuoteItems.Count(i => i.QuoteId == q.Id)
                })
                .ToList();

            _open    = all.Where(q => IsOpen(q.Status)).ToList();
            _pending = all.Where(q => IsPending(q.Status)).ToList();
            _closed  = all.Where(q => IsClosed(q.Status)).ToList();

            OpenTabTxt.Text    = $"Open  ({_open.Count})";
            PendingTabTxt.Text = $"Pending  ({_pending.Count})";
            ClosedTabTxt.Text  = $"Closed  ({_closed.Count})";

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            if (OrderGrid == null) return;
            var list = CurrentList();
            OrderGrid.ItemsSource  = list;
            OrderGrid.SelectedItem = null;
            StatusTxt.Text         = $"{list.Count} order(s)";
            OpenOrderBtn.Visibility = Visibility.Collapsed;
        }

        private List<QuoteRowVm> CurrentList()
            => OpenTab.IsChecked    == true ? _open
             : PendingTab.IsChecked == true ? _pending
             : _closed;

        private static bool IsOpen(string s)    => s is "Open" or "Draft";
        private static bool IsPending(string s) => s is "Pending";
        private static bool IsClosed(string s)  => !IsOpen(s) && !IsPending(s);

        // ── Event handlers ────────────────────────────────────────────────
        private void Tab_Checked(object sender, RoutedEventArgs e) => RefreshGrid();

        private void RefreshBtn_Click(object sender, RoutedEventArgs e) => LoadOrders();

        private void NewOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            var win = new SalesOrderWindow(Session.CurrentUser!) { Owner = this };
            win.ShowDialog();
            LoadOrders();
        }

        private void OrderGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrderGrid.SelectedItem is QuoteRowVm row)
            {
                var customer = string.IsNullOrWhiteSpace(row.CustomerName)
                    ? "(No customer)" : row.CustomerName;
                StatusTxt.Text = $"Selected: {row.QuoteNumber}  |  Customer: {customer}  |  Total: R {row.Total:N2}  |  Items: {row.ItemCount}";
                OpenOrderBtn.Visibility = Visibility.Visible;
            }
            else
            {
                StatusTxt.Text = $"{CurrentList().Count} order(s)";
                OpenOrderBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void OrderGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            => OpenSelected();

        private void OpenOrderBtn_Click(object sender, RoutedEventArgs e)
            => OpenSelected();

        private void OpenSelected()
        {
            if (OrderGrid.SelectedItem is not QuoteRowVm row) return;
            var win = new SalesOrderWindow(Session.CurrentUser!, row.Id) { Owner = this };
            win.ShowDialog();
            LoadOrders();
        }
    }

    public class QuoteRowVm
    {
        public int      Id           { get; set; }
        public string   QuoteNumber  { get; set; } = "";
        public string   CustomerName { get; set; } = "";
        public DateTime CreatedAt    { get; set; }
        public DateTime ExpiresAt    { get; set; }
        public int      ItemCount    { get; set; }
        public decimal  Total        { get; set; }
        public string   CreatedBy    { get; set; } = "";
        public string   Status       { get; set; } = "";
    }
}
