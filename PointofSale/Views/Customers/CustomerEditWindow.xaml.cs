using PointofSale.Data;
using PointofSale.Models;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PointofSale.Views
{
    public partial class CustomerEditWindow : Window
    {
        private readonly int? _editId;
        public bool Saved { get; private set; } = false;

        public CustomerEditWindow(int? editCustomerId = null)
        {
            InitializeComponent();
            _editId = editCustomerId;
            Title = _editId == null ? "Add Customer" : "Edit Customer";

            // Recalculate available credit whenever balance or limit changes
            AccountBalanceBox.TextChanged += RecalcAvailable;
            CreditLimitBox.TextChanged += RecalcAvailable;

            if (_editId != null)
                LoadForEdit();
        }

        private void LoadForEdit()
        {
            using var db = new AppDbContext();
            var c = db.Customers.FirstOrDefault(x => x.Id == _editId!.Value);
            if (c == null) { MessageBox.Show("Customer not found."); Close(); return; }

            FirstNameBox.Text = c.FirstName;
            LastNameBox.Text = c.LastName;
            PhoneBox.Text = c.Phone;
            EmailBox.Text = c.Email ?? "";
            AddressBox.Text = c.Address ?? "";
            CityBox.Text = c.City ?? "";
            ProvinceBox.Text = c.Province ?? "";
            PostalCodeBox.Text = c.PostalCode ?? "";
            AccountBalanceBox.Text = c.AccountBalance.ToString("0.00");
            CreditLimitBox.Text = c.CreditLimit.ToString("0.00");
            NotesBox.Text = c.Notes ?? "";
            InactiveCheck.IsChecked = !c.IsActive;

            RecalcAvailable(null, null);
        }

        private void RecalcAvailable(object? sender, TextChangedEventArgs? e)
        {
            decimal.TryParse(AccountBalanceBox.Text, out var bal);
            decimal.TryParse(CreditLimitBox.Text, out var limit);
            AvailableCreditBox.Text = (limit - bal).ToString("0.00");
        }

        private bool ValidateInputs(out string error)
        {
            error = "";

            if (string.IsNullOrWhiteSpace(FirstNameBox.Text))
            { error = "First Name is required."; return false; }

            if (string.IsNullOrWhiteSpace(LastNameBox.Text))
            { error = "Last Name is required."; return false; }

            if (!string.IsNullOrWhiteSpace(AccountBalanceBox.Text) &&
                !decimal.TryParse(AccountBalanceBox.Text.Trim(), out _))
            { error = "Account Balance must be a valid number."; return false; }

            if (!string.IsNullOrWhiteSpace(CreditLimitBox.Text) &&
                !decimal.TryParse(CreditLimitBox.Text.Trim(), out _))
            { error = "Credit Limit must be a valid number."; return false; }

            return true;
        }

        private void SaveInternal()
        {
            using var db = new AppDbContext();

            Customer c;
            if (_editId == null)
            {
                c = new Customer();
                db.Customers.Add(c);
            }
            else
            {
                c = db.Customers.First(x => x.Id == _editId.Value);
            }

            c.FirstName = FirstNameBox.Text.Trim();
            c.LastName = LastNameBox.Text.Trim();
            c.Phone = PhoneBox.Text.Trim();
            c.Email = EmailBox.Text.Trim();
            c.Address = AddressBox.Text.Trim();
            c.City = CityBox.Text.Trim();
            c.Province = ProvinceBox.Text.Trim();
            c.PostalCode = PostalCodeBox.Text.Trim();
            c.Notes = NotesBox.Text.Trim();
            c.IsActive = !(InactiveCheck.IsChecked ?? false);

            c.AccountBalance = string.IsNullOrWhiteSpace(AccountBalanceBox.Text)
                               ? 0 : decimal.Parse(AccountBalanceBox.Text.Trim());
            c.CreditLimit = string.IsNullOrWhiteSpace(CreditLimitBox.Text)
                               ? 0 : decimal.Parse(CreditLimitBox.Text.Trim());

            db.SaveChanges();
            Saved = true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out var error))
            {
                StatusTxt.Text = error;
                StatusTxt.Visibility = Visibility.Visible;
                return;
            }
            StatusTxt.Visibility = Visibility.Collapsed;
            SaveInternal();
            if (Saved) Close();
        }

        private void SaveAndNew_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out var error))
            {
                StatusTxt.Text = error;
                StatusTxt.Visibility = Visibility.Visible;
                return;
            }
            StatusTxt.Visibility = Visibility.Collapsed;
            SaveInternal();
            if (Saved) ClearFormForNew();
        }

        private void ClearFormForNew()
        {
            FirstNameBox.Text = "";
            LastNameBox.Text = "";
            PhoneBox.Text = "";
            EmailBox.Text = "";
            AddressBox.Text = "";
            CityBox.Text = "";
            ProvinceBox.Text = "";
            PostalCodeBox.Text = "";
            AccountBalanceBox.Text = "0.00";
            CreditLimitBox.Text = "0.00";
            AvailableCreditBox.Text = "0.00";
            NotesBox.Text = "";
            InactiveCheck.IsChecked = false;
            Saved = false;
            Title = "Add Customer";
            FirstNameBox.Focus();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Saved = false;
            Close();
        }
    }
}