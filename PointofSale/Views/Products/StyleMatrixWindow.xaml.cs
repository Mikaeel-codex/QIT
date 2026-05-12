using PointofSale.Data;
using PointofSale.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PointofSale.Views
{
    public partial class StyleMatrixWindow : Window
    {
        // ── Dimensions ────────────────────────────────────────────────────
        private const double SizeColW = 100;
        private const double AttrColW = 110;
        private const double TotalColW = 90;
        private const double RowH = 34;

        // ── Colours ───────────────────────────────────────────────────────
        private readonly SolidColorBrush _headerBg = new(Color.FromRgb(0x37, 0x37, 0x37));
        private readonly SolidColorBrush _evenRowBg = new(Color.FromRgb(0x26, 0x26, 0x26));
        private readonly SolidColorBrush _oddRowBg = new(Color.FromRgb(0x20, 0x20, 0x20));
        private readonly SolidColorBrush _totalBg = new(Color.FromRgb(0x2A, 0x2A, 0x2A));
        private readonly SolidColorBrush _gridLine = new(Color.FromRgb(0x44, 0x44, 0x44));
        private readonly SolidColorBrush _addBg = new(Color.FromRgb(0x1E, 0x1E, 0x1E));
        private readonly SolidColorBrush _mutedFg = new(Color.FromRgb(0x55, 0x55, 0x55));
        private readonly SolidColorBrush _whiteFg = Brushes.White;
        private readonly SolidColorBrush _goldFg = new(Color.FromRgb(0xF4, 0xC5, 0x42));
        private readonly SolidColorBrush _inputBg = new(Color.FromRgb(0x1A, 0x2A, 0x1A));

        // ── Data ─────────────────────────────────────────────────────────
        private readonly List<string> _attributes = new();
        private readonly List<string> _sizes = new();

        // qty[row, col] — indexed by size index × attribute index
        private int[,] _qty = new int[0, 0];

        // TextBox references so we can read values on save
        private readonly List<TextBox> _attrBoxes = new();
        private readonly List<TextBox> _sizeBoxes = new();

        // qty TextBoxes indexed [sizeIdx, attrIdx]
        private TextBox[,] _qtyBoxes = new TextBox[0, 0];

        // Total label TextBlocks — row totals and column totals
        private readonly List<TextBlock> _rowTotalLabels = new();
        private readonly List<TextBlock> _colTotalLabels = new();
        private TextBlock? _grandTotalLabel;

        // Pre-fill passed from ProductEditWindow
        public string PresetName { get; set; } = "";
        public string PresetDepartment { get; set; } = "";
        public decimal PresetPrice { get; set; } = 0;
        public string PresetSKU { get; set; } = "";

        public bool Saved { get; private set; } = false;

        public StyleMatrixWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ItemNameBox.Text = PresetName;

            using var db = new AppDbContext();
            DepartmentBox.ItemsSource = db.Departments
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => d.Name)
                .ToList();
            DepartmentBox.Text = PresetDepartment;

            _attributes.Add("");
            _sizes.Add("");
            ResizeQtyGrid();
            RebuildMatrix();
        }

        // ── Qty grid resize (preserve existing values) ────────────────────
        private void ResizeQtyGrid()
        {
            int rows = _sizes.Count;
            int cols = _attributes.Count;
            var newQty = new int[rows, cols];

            int oldRows = _qty.GetLength(0);
            int oldCols = _qty.GetLength(1);

            for (int r = 0; r < Math.Min(rows, oldRows); r++)
                for (int c = 0; c < Math.Min(cols, oldCols); c++)
                    newQty[r, c] = _qty[r, c];

            _qty = newQty;
        }

        // ── Matrix builder ────────────────────────────────────────────────
        private void RebuildMatrix()
        {
            MatrixContainer.Children.Clear();
            MatrixContainer.RowDefinitions.Clear();
            MatrixContainer.ColumnDefinitions.Clear();
            _attrBoxes.Clear();
            _sizeBoxes.Clear();
            _rowTotalLabels.Clear();
            _colTotalLabels.Clear();
            _grandTotalLabel = null;

            int attrCount = _attributes.Count;
            int sizeCount = _sizes.Count;

            _qtyBoxes = new TextBox[sizeCount, attrCount];

            // ── Column definitions ────────────────────────────────────────
            MatrixContainer.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(SizeColW) });
            for (int c = 0; c < attrCount; c++)
                MatrixContainer.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(AttrColW) });
            MatrixContainer.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(AttrColW) }); // add col
            MatrixContainer.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(TotalColW) }); // totals col

            // ── Row definitions ───────────────────────────────────────────
            MatrixContainer.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(RowH) }); // header
            for (int r = 0; r < sizeCount; r++)
                MatrixContainer.RowDefinitions.Add(
                    new RowDefinition { Height = new GridLength(RowH) });
            MatrixContainer.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(RowH) }); // add row
            MatrixContainer.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(RowH) }); // totals row

            // ── HEADER ROW ────────────────────────────────────────────────
            PlaceCell(MakeHeaderCell("Sizes ↓  Attr →", 10, muted: true), 0, 0);

            for (int c = 0; c < attrCount; c++)
            {
                var idx = c;
                var tb = new TextBox
                {
                    Text = _attributes[c],
                    Background = Brushes.Transparent,
                    Foreground = _whiteFg,
                    BorderThickness = new Thickness(0),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(4, 0, 4, 0),
                    CaretBrush = Brushes.White,
                    SelectionBrush = new SolidColorBrush(Color.FromRgb(0xF4, 0xC5, 0x42)),
                    ToolTip = "Attribute / colour name  •  Right-click to delete",
                };
                tb.TextChanged += (s, _) => _attributes[idx] = tb.Text;
                _attrBoxes.Add(tb);

                var cell = MakeBorderCell(_headerBg);
                var cm = MakeDeleteMenu(
                    () => _attrBoxes.ElementAtOrDefault(idx)?.Text ?? _attributes[idx],
                    () => DeleteAttribute(idx));
                cell.ContextMenu = cm;
                tb.ContextMenu = cm;
                cell.Child = tb;
                PlaceCell(cell, 0, c + 1);
            }

            PlaceCell(MakeClickCell("Click to add", AddAttribute), 0, attrCount + 1);
            PlaceCell(MakeHeaderCell("Totals", 12, bold: true), 0, attrCount + 2);

            // ── SIZE ROWS ─────────────────────────────────────────────────
            for (int r = 0; r < sizeCount; r++)
            {
                var rowBg = r % 2 == 0 ? _evenRowBg : _oddRowBg;
                var rowIdx = r;

                // Size label
                var sizeTb = new TextBox
                {
                    Text = _sizes[r],
                    Background = Brushes.Transparent,
                    Foreground = _whiteFg,
                    BorderThickness = new Thickness(0),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(4, 0, 4, 0),
                    CaretBrush = Brushes.White,
                    ToolTip = "Size value  •  Right-click to delete",
                };
                sizeTb.TextChanged += (s, _) => _sizes[rowIdx] = sizeTb.Text;
                _sizeBoxes.Add(sizeTb);

                var sizeCell = MakeBorderCell(_headerBg);
                var sizeCm = MakeDeleteMenu(
                    () => _sizeBoxes.ElementAtOrDefault(rowIdx)?.Text ?? _sizes[rowIdx],
                    () => DeleteSize(rowIdx));
                sizeCell.ContextMenu = sizeCm;
                sizeTb.ContextMenu = sizeCm;
                sizeCell.Child = sizeTb;
                PlaceCell(sizeCell, r + 1, 0);

                // Qty cells
                for (int c = 0; c < attrCount; c++)
                {
                    var ri = r;
                    var ci = c;

                    var qtyTb = new TextBox
                    {
                        Text = _qty[r, c] == 0 ? "" : _qty[r, c].ToString(),
                        Background = Brushes.Transparent,
                        Foreground = _whiteFg,
                        BorderThickness = new Thickness(0),
                        FontSize = 13,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(4, 0, 4, 0),
                        CaretBrush = Brushes.White,
                        SelectionBrush = new SolidColorBrush(Color.FromRgb(0xF4, 0xC5, 0x42)),
                        ToolTip = "Stock quantity",
                    };

                    // Only allow numbers
                    qtyTb.PreviewTextInput += (s, ev) =>
                    {
                        ev.Handled = !int.TryParse(ev.Text, out _);
                    };

                    qtyTb.TextChanged += (s, _) =>
                    {
                        int.TryParse(qtyTb.Text, out var val);
                        _qty[ri, ci] = val;
                        RefreshTotals();
                    };

                    qtyTb.GotFocus += (s, _) => qtyTb.Background = _inputBg;
                    qtyTb.LostFocus += (s, _) => qtyTb.Background = Brushes.Transparent;

                    _qtyBoxes[r, c] = qtyTb;

                    var cell = MakeBorderCell(rowBg);
                    cell.Child = qtyTb;
                    PlaceCell(cell, r + 1, c + 1);
                }

                // Empty add-col cell
                PlaceCell(MakeBorderCell(_addBg), r + 1, attrCount + 1);

                // Row total label
                var rowTotalLbl = new TextBlock
                {
                    Foreground = _goldFg,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                _rowTotalLabels.Add(rowTotalLbl);
                var rowTotalCell = MakeBorderCell(_totalBg);
                rowTotalCell.Child = rowTotalLbl;
                PlaceCell(rowTotalCell, r + 1, attrCount + 2);
            }

            // ── ADD SIZE ROW ──────────────────────────────────────────────
            int addRow = sizeCount + 1;
            PlaceCell(MakeClickCell("Click to add", AddSize), addRow, 0);
            for (int c = 1; c < attrCount + 3; c++)
                PlaceCell(MakeBorderCell(_addBg), addRow, c);

            // ── TOTALS ROW ────────────────────────────────────────────────
            int totRow = sizeCount + 2;
            PlaceCell(MakeHeaderCell("Totals", 12, bold: true), totRow, 0);

            for (int c = 0; c < attrCount; c++)
            {
                var colTotalLbl = new TextBlock
                {
                    Foreground = _goldFg,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                _colTotalLabels.Add(colTotalLbl);
                var colTotalCell = MakeBorderCell(_totalBg);
                colTotalCell.Child = colTotalLbl;
                PlaceCell(colTotalCell, totRow, c + 1);
            }

            PlaceCell(MakeBorderCell(_addBg), totRow, attrCount + 1);

            _grandTotalLabel = new TextBlock
            {
                Foreground = _goldFg,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var grandCell = MakeBorderCell(_totalBg);
            grandCell.Child = _grandTotalLabel;
            PlaceCell(grandCell, totRow, attrCount + 2);

            RefreshTotals();
        }

        // ── Totals calculator ─────────────────────────────────────────────
        private void RefreshTotals()
        {
            int sizeCount = _sizes.Count;
            int attrCount = _attributes.Count;

            if (_qty.GetLength(0) != sizeCount || _qty.GetLength(1) != attrCount) return;

            int grandTotal = 0;

            // Row totals
            for (int r = 0; r < sizeCount && r < _rowTotalLabels.Count; r++)
            {
                int rowSum = 0;
                for (int c = 0; c < attrCount; c++)
                    rowSum += _qty[r, c];
                _rowTotalLabels[r].Text = rowSum.ToString();
                grandTotal += rowSum;
            }

            // Column totals
            for (int c = 0; c < attrCount && c < _colTotalLabels.Count; c++)
            {
                int colSum = 0;
                for (int r = 0; r < sizeCount; r++)
                    colSum += _qty[r, c];
                _colTotalLabels[c].Text = colSum.ToString();
            }

            if (_grandTotalLabel != null)
                _grandTotalLabel.Text = grandTotal.ToString();
        }

        // ── Add row / column ──────────────────────────────────────────────
        private void AddAttribute()
        {
            // Flush current attr box values first
            for (int i = 0; i < _attrBoxes.Count && i < _attributes.Count; i++)
                _attributes[i] = _attrBoxes[i].Text;

            _attributes.Add("");
            ResizeQtyGrid();
            RebuildMatrix();

            Dispatcher.InvokeAsync(() =>
            {
                _attrBoxes.LastOrDefault()?.Focus();
            });
        }

        private void AddSize()
        {
            for (int i = 0; i < _sizeBoxes.Count && i < _sizes.Count; i++)
                _sizes[i] = _sizeBoxes[i].Text;

            _sizes.Add("");
            ResizeQtyGrid();
            RebuildMatrix();

            Dispatcher.InvokeAsync(() =>
            {
                _sizeBoxes.LastOrDefault()?.Focus();
            });
        }

        // ── Save ──────────────────────────────────────────────────────────
        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            var itemName = ItemNameBox.Text.Trim();
            var department = DepartmentBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(itemName))
            { ShowStatus("Item Name is required."); return; }

            if (string.IsNullOrWhiteSpace(department))
            { ShowStatus("Department is required."); return; }

            // Flush TextBox values
            for (int i = 0; i < _attrBoxes.Count && i < _attributes.Count; i++)
                _attributes[i] = _attrBoxes[i].Text;
            for (int i = 0; i < _sizeBoxes.Count && i < _sizes.Count; i++)
                _sizes[i] = _sizeBoxes[i].Text;

            var validSizes = _sizes.Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            var validAttrs = _attributes.Select(a => a.Trim()).Where(a => a.Length > 0).ToList();

            if (validSizes.Count == 0)
            { ShowStatus("Add at least one size."); return; }
            if (validAttrs.Count == 0)
            { ShowStatus("Add at least one attribute."); return; }

            try
            {
                using var db = new AppDbContext();
                int created = 0, updated = 0;

                for (int r = 0; r < validSizes.Count; r++)
                {
                    for (int c = 0; c < validAttrs.Count; c++)
                    {
                        var size = validSizes[r];
                        var attr = validAttrs[c];
                        var qty = (r < _qty.GetLength(0) && c < _qty.GetLength(1))
                                   ? _qty[r, c] : 0;

                        var existing = db.Products.FirstOrDefault(p =>
                            p.Name == itemName &&
                            p.Size == size &&
                            p.Attribute == attr);

                        if (existing != null)
                        {
                            // Update qty if changed
                            existing.StockQty = qty;
                            updated++;
                        }
                        else
                        {
                            db.Products.Add(new Product
                            {
                                Name = itemName,
                                Department = department,
                                Size = size,
                                Attribute = attr,
                                Price = PresetPrice,
                                CostPrice = 0,
                                SKU = PresetSKU,
                                StockQty = qty,
                                ProductType = "Inventory",
                            });
                            created++;
                        }
                    }
                }

                db.SaveChanges();
                Saved = true;

                var msg = "";
                if (created > 0) msg += $"{created} variant(s) created. ";
                if (updated > 0) msg += $"{updated} updated.";

                MessageBox.Show(msg.Trim(), "Saved",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                ShowStatus($"Error: {ex.Message}");
            }
        }

        // ── Delete row / column ───────────────────────────────────────────
        private void DeleteSize(int rowIdx)
        {
            if (_sizes.Count <= 1)
            { ShowStatus("Cannot delete the last row."); return; }

            for (int i = 0; i < _sizeBoxes.Count && i < _sizes.Count; i++)
                _sizes[i] = _sizeBoxes[i].Text;
            for (int i = 0; i < _attrBoxes.Count && i < _attributes.Count; i++)
                _attributes[i] = _attrBoxes[i].Text;

            _sizes.RemoveAt(rowIdx);

            var oldQty = _qty;
            int newRows = _sizes.Count;
            int cols = _attributes.Count;
            var newQty = new int[newRows, cols];

            int destRow = 0;
            for (int r = 0; r < oldQty.GetLength(0); r++)
            {
                if (r == rowIdx) continue;
                for (int c = 0; c < cols; c++)
                    newQty[destRow, c] = oldQty[r, c];
                destRow++;
            }
            _qty = newQty;
            RebuildMatrix();
        }

        private void DeleteAttribute(int colIdx)
        {
            if (_attributes.Count <= 1)
            { ShowStatus("Cannot delete the last column."); return; }

            for (int i = 0; i < _sizeBoxes.Count && i < _sizes.Count; i++)
                _sizes[i] = _sizeBoxes[i].Text;
            for (int i = 0; i < _attrBoxes.Count && i < _attributes.Count; i++)
                _attributes[i] = _attrBoxes[i].Text;

            _attributes.RemoveAt(colIdx);

            var oldQty = _qty;
            int rows = _sizes.Count;
            int newCols = _attributes.Count;
            var newQty = new int[rows, newCols];

            for (int r = 0; r < rows; r++)
            {
                int destCol = 0;
                for (int c = 0; c < oldQty.GetLength(1); c++)
                {
                    if (c == colIdx) continue;
                    newQty[r, destCol] = oldQty[r, c];
                    destCol++;
                }
            }
            _qty = newQty;
            RebuildMatrix();
        }

        private ContextMenu MakeDeleteMenu(Func<string> getLabel, Action onDelete)
        {
            var menu = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(0x1B, 0x1B, 0x1B)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
            };

            var item = new MenuItem
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
                Background = Brushes.Transparent,
                FontSize = 12,
                Icon = null,
            };

            // Set header dynamically when menu opens so it shows current typed text
            menu.Opened += (_, _) =>
            {
                item.Header = $"🗑  Delete  \"{getLabel()}\"";
            };

            item.Click += (_, _) => onDelete();
            menu.Items.Add(item);
            return menu;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e) => Close();

        private void ShowStatus(string msg)
        {
            StatusTxt.Text = msg;
            StatusTxt.Visibility = Visibility.Visible;
        }

        // ── Cell factories ────────────────────────────────────────────────
        private Border MakeBorderCell(SolidColorBrush bg) => new()
        {
            Background = bg,
            BorderBrush = _gridLine,
            BorderThickness = new Thickness(0, 0, 1, 1),
        };

        private Border MakeHeaderCell(string text, double fontSize = 12,
            bool bold = false, bool muted = false)
        {
            var cell = MakeBorderCell(_headerBg);
            cell.Child = new TextBlock
            {
                Text = text,
                Foreground = muted ? _mutedFg : _whiteFg,
                FontSize = fontSize,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(4, 0, 4, 0),
            };
            return cell;
        }

        private Border MakeClickCell(string label, Action onClick)
        {
            var cell = MakeBorderCell(_addBg);
            var tb = new TextBlock
            {
                Text = label,
                Foreground = _mutedFg,
                FontStyle = FontStyles.Italic,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
            };
            tb.MouseLeftButtonDown += (_, _) => onClick();
            tb.MouseEnter += (_, _) => tb.Foreground = _goldFg;
            tb.MouseLeave += (_, _) => tb.Foreground = _mutedFg;
            cell.Child = tb;
            return cell;
        }

        private void PlaceCell(UIElement element, int row, int col)
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, col);
            MatrixContainer.Children.Add(element);
        }
    }
}