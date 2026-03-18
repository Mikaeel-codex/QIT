using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PointofSale.Views.Settings
{
    /// <summary>
    /// Placeholder page shown for settings sections not yet built.
    /// Just pass in the message to display.
    /// </summary>
    public class ComingSoonPage : Page
    {
        public ComingSoonPage(string message = "Coming soon.")
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111111"));

            var panel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            panel.Children.Add(new TextBlock
            {
                Text = "🚧",
                FontSize = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16),
            });

            panel.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666")),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
            });

            Content = panel;
        }
    }
}