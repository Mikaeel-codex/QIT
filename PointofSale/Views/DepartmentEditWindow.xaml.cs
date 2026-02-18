using PointofSale.Data;
using PointofSale.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace PointofSale.Views
{
    public partial class DepartmentEditWindow : Window
    {
        private readonly int? _id;

        public DepartmentEditWindow(int? departmentId = null)
        {
            InitializeComponent();
            _id = departmentId;

            if (_id != null)
            {
                TitleText.Text = "Edit Department";
                LoadDepartment(_id.Value);
            }
        }

        private void LoadDepartment(int id)
        {
            using var db = new AppDbContext();
            var d = db.Departments.FirstOrDefault(x => x.Id == id);
            if (d == null) return;

            NameBox.Text = d.Name;
            CodeBox.Text = d.Code;
            TaxCodeBox.Text = d.TaxCode;
            MarginBox.Text = d.MarginPercent.ToString(CultureInfo.InvariantCulture);
            MarkupBox.Text = d.MarkupPercent.ToString(CultureInfo.InvariantCulture);
            ActiveBox.IsChecked = d.IsActive;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var name = (NameBox.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Department name is required.");
                return;
            }

            if (!decimal.TryParse(MarginBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var margin))
                margin = 0;

            if (!decimal.TryParse(MarkupBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var markup))
                markup = 0;

            using var db = new AppDbContext();

            var exists = db.Departments.Any(x => x.Name.ToLower() == name.ToLower() && x.Id != (_id ?? 0));
            if (exists)
            {
                MessageBox.Show("That department already exists.");
                return;
            }

            Department entity;
            if (_id == null)
            {
                entity = new Department();
                db.Departments.Add(entity);
            }
            else
            {
                entity = db.Departments.First(x => x.Id == _id.Value);
            }

            entity.Name = name;
            entity.Code = string.IsNullOrWhiteSpace(CodeBox.Text) ? null : CodeBox.Text.Trim();
            entity.TaxCode = string.IsNullOrWhiteSpace(TaxCodeBox.Text) ? null : TaxCodeBox.Text.Trim();
            entity.MarginPercent = margin;
            entity.MarkupPercent = markup;
            entity.IsActive = ActiveBox.IsChecked == true;

            db.SaveChanges();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}
