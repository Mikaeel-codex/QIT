using Microsoft.EntityFrameworkCore;
using PointofSale.Data;
using PointofSale.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PointofSale.Views
{
    public partial class HeldReceiptsWindow : Window
    {
        private List<HeldReceipt> _allReceipts = new();
        private bool _detailOpen = false;
        private const double DetailWidth = 680;

        public Action<HeldReceipt>? OnUnhold { get; set; }

        // Tracks when a double-click opened the panel so MouseDown doesn't immediately close it
        private bool _justOpenedByDoubleClick = false;

        public HeldReceiptsWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => LoadReceipts();

            // Close detail when clicking the left list area — but NOT right after a double-click open
            MouseDown += (s, e) =>
            {
                if (!_detailOpen) return;
                if (_justOpenedByDoubleClick) { _justOpenedByDoubleClick = false; return; }
                if (!IsClickInsideFrame(e.GetPosition(this)))
                    CloseDetail_Click(this, new System.Windows.RoutedEventArgs());
            };
        }

        private bool IsClickInsideFrame(System.Windows.Point p)
        {
            var framePt = DetailFrame.TranslatePoint(new System.Windows.Point(0, 0), this);
            var frameRect = new System.Windows.Rect(framePt.X, framePt.Y,
                DetailFrame.ActualWidth, DetailFrame.ActualHeight);
            return frameRect.Contains(p);
        }

        // ── Data Loading ─────────────────────────────────────────────────

        private void LoadReceipts()
        {
            try
            {
                using var db = new AppDbContext();
                var cutoff = GetCutoff();

                _allReceipts = db.HeldReceipts
                    .Where(h => cutoff == null || h.HeldAt >= cutoff)
                    .OrderByDescending(h => h.HeldAt)
                    .ToList();

                ApplySearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load held receipts: {ex.Message}", "Error");
            }
        }

        private void ApplySearch()
        {
            if (SearchBox == null || HeldGrid == null || StatusTxt == null) return;

            var q = SearchBox.Text.Trim().ToLower();
            var filtered = string.IsNullOrEmpty(q)
                ? _allReceipts
                : _allReceipts.Where(h =>
                    h.Cashier.ToLower().Contains(q) ||
                    h.CustomerName.ToLower().Contains(q) ||
                    h.ItemsSummary.ToLower().Contains(q)).ToList();

            HeldGrid.ItemsSource = filtered;
            StatusTxt.Text = $"{filtered.Count} record(s)  |  {_allReceipts.Count} total held";
        }

        private DateTime? GetCutoff() =>
            (PeriodBox.SelectedItem as ComboBoxItem)?.Content?.ToString() switch
            {
                "Today" => DateTime.Today,
                "Last 7 Days" => DateTime.Today.AddDays(-7),
                "Last 30 Days" => DateTime.Today.AddDays(-30),
                _ => (DateTime?)null
            };

        // ── Filters ───────────────────────────────────────────────────────

        private void Period_Changed(object sender, SelectionChangedEventArgs e) => LoadReceipts();
        private void Search_Changed(object sender, TextChangedEventArgs e) => ApplySearch();

        // ── Selection / Double-Click → Show Detail ────────────────────────

        private void HeldGrid_DoubleClick(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            if (HeldGrid.SelectedItem is HeldReceipt hr)
            {
                _justOpenedByDoubleClick = true;  // prevent MouseDown from closing immediately
                ShowDetail(hr);
            }
        }

        private void HeldGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If detail already open, keep it in sync when user clicks a different row
            if (_detailOpen && HeldGrid.SelectedItem is HeldReceipt hr)
                ShowDetail(hr);
        }

        private void ShowDetails_Click(object sender, RoutedEventArgs e)
        {
            if (HeldGrid.SelectedItem is not HeldReceipt hr)
            {
                MessageBox.Show("Select a held receipt first.", "No Selection");
                return;
            }
            ShowDetail(hr);
        }

        private void ShowDetail(HeldReceipt hr)
        {
            try
            {
                using var db = new AppDbContext();
                var items = db.HeldReceiptItems
                    .Where(i => i.HeldReceiptId == hr.Id)
                    .ToList();

                // Create a fresh Page and load the receipt into it
                var page = new HeldReceiptDetailPage();
                page.OnCloseRequested = () => CloseDetail_Click(this, new RoutedEventArgs());
                page.Load(hr, items);
                DetailFrame.Navigate(page);

                // Slide open if not already open
                if (!_detailOpen)
                {
                    _detailOpen = true;
                    if (ShowDetailsBtn != null) ShowDetailsBtn.Content = "Hide Details";
                    AnimateDetailPanel(0, DetailWidth);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load receipt detail: {ex.Message}", "Error");
            }
        }

        private void CloseDetail_Click(object sender, RoutedEventArgs e)
        {
            if (!_detailOpen) return;
            _detailOpen = false;
            if (ShowDetailsBtn != null) ShowDetailsBtn.Content = "Show Details";
            DetailFrame.Content = null;
            AnimateDetailPanel(DetailWidth, 0);
        }

        private void AnimateDetailPanel(double from, double to)
        {
            // Animate via a DispatcherTimer — ColumnDefinition is not a UIElement
            // so BeginAnimation does not work on it directly.
            const int steps = 12;
            int step = 0;
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(14)
            };
            timer.Tick += (s, e) =>
            {
                step++;
                double t = (double)step / steps;
                // Ease out cubic
                t = 1 - Math.Pow(1 - t, 3);
                double val = from + (to - from) * t;
                DetailCol.Width = new GridLength(Math.Max(0, val));
                if (step >= steps)
                {
                    DetailCol.Width = new GridLength(to);
                    timer.Stop();
                }
            };
            timer.Start();
        }

        // ── Unhold ────────────────────────────────────────────────────────

        private void Unhold_Click(object sender, RoutedEventArgs e)
        {
            if (HeldGrid.SelectedItem is not HeldReceipt hr)
            {
                MessageBox.Show("Select a held receipt first.", "No Selection");
                return;
            }

            var preview = string.IsNullOrWhiteSpace(hr.ItemsSummary) ? "" :
                $"\n\nItems: {hr.ItemsSummary}";

            var confirm = MessageBox.Show(
                $"Restore this held receipt to the sales screen?{preview}" +
                $"\n\nDate: {hr.HeldAt:MM/dd/yyyy HH:mm}   Total: {hr.Total:N2}",
                "Unhold Receipt", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var db = new AppDbContext();
                var full = db.HeldReceipts
                    .Include(h => h.Items)
                    .First(h => h.Id == hr.Id);

                db.HeldReceiptItems.RemoveRange(full.Items);
                db.HeldReceipts.Remove(full);
                db.SaveChanges();

                OnUnhold?.Invoke(full);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to unhold: {ex.Message}", "Error");
            }
        }

        // ── Delete ────────────────────────────────────────────────────────

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (HeldGrid.SelectedItem is not HeldReceipt hr)
            {
                MessageBox.Show("Select a held receipt first.", "No Selection");
                return;
            }

            var preview = string.IsNullOrWhiteSpace(hr.ItemsSummary) ? "" :
                $"\nItems: {hr.ItemsSummary}";

            var confirm = MessageBox.Show(
                $"Permanently delete this held receipt?{preview}" +
                $"\n\nDate: {hr.HeldAt:MM/dd/yyyy HH:mm}   Total: {hr.Total:N2}",
                "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var db = new AppDbContext();
                var items = db.HeldReceiptItems
                    .Where(i => i.HeldReceiptId == hr.Id).ToList();
                db.HeldReceiptItems.RemoveRange(items);
                db.HeldReceipts.Remove(db.HeldReceipts.First(h => h.Id == hr.Id));
                db.SaveChanges();

                _allReceipts.Remove(hr);
                ApplySearch();

                if (_detailOpen)
                {
                    _detailOpen = false;
                    DetailFrame.Content = null;
                    DetailCol.Width = new GridLength(0);
                    if (ShowDetailsBtn != null) ShowDetailsBtn.Content = "Show Details";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete: {ex.Message}", "Error");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}