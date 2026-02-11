using System;
using System.Collections.ObjectModel;

namespace PointofSale.ViewModels
{
    public class ReceiptItemVM
    {
        public string Name { get; set; } = "";
        public string DetailText { get; set; } = "";
        public decimal LineTotal { get; set; }
    }

    public class ReceiptViewModel
    {
        public int SaleId { get; }
        public string CashierUsername { get; }
        public DateTime ReceiptDate { get; }

        public ObservableCollection<ReceiptItemVM> Items { get; } = new();

        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }

        public string ReceiptDateText => ReceiptDate.ToString("yyyy-mm-dd HH:mm");
        public string SaleNumberText => $"Sale #{SaleId}";
        public string CashierText => $"Cashier: {CashierUsername}";

        public ReceiptViewModel(int saleId, string cashierUsername)
        {
            SaleId = saleId;
            CashierUsername = cashierUsername;
            ReceiptDate = DateTime.Now;
        }
    }
}
