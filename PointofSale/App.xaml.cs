using Microsoft.EntityFrameworkCore;
using PointofSale.Data;
using PointofSale.Services;
using System;
using System.Windows;
using System.Windows.Threading;

namespace PointofSale
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            ThemeService.ApplySaved();

            Task.Run(() =>
            {
                using var db = new AppDbContext();
                db.Database.Migrate();
                new AuthService().EnsureSeedUsers();
            }).GetAwaiter().GetResult();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            ShowError(e.Exception);
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                ShowError(ex);
        }

        private static void ShowError(Exception ex)
        {
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}