using PointofSale.Services;
using System.Windows;
using System.Windows.Media;

namespace PointofSale.Services
{
    /// <summary>
    /// Manages colour theme switching at runtime by updating App.Current.Resources.
    /// The chosen theme key is persisted via StoreSettingsService.
    /// </summary>
    public static class ThemeService
    {
        public const string SettingKey = "AppTheme";
        public const string DefaultTheme = "Dark";

        // ── Theme definitions ─────────────────────────────────────────────
        // Each theme supplies the four core brushes used across the app.
        // Bg        = window background
        // Panel     = card / sidebar background
        // Panel2    = alternating row / secondary panel
        // Border    = border / divider lines
        // Accent    = primary action colour
        // Text      = main foreground text
        // TextMuted = secondary / muted text

        public static void Apply(string themeKey)
        {
            var (bg, panel, panel2, border, accent, text, muted) = themeKey switch
            {
                "Forest" => (
                    "#0D1F17",   // bg        — very dark green
                    "#163324",   // panel
                    "#1E4232",   // panel2
                    "#2A5C40",   // border
                    "#3A8C5C",   // accent
                    "#FFFFFF",   // text
                    "#88B89A"    // muted
                ),
                "Ocean" => (
                    "#1E1B4B",   // bg        — deep navy
                    "#252272",   // panel
                    "#3A7CA5",   // panel2
                    "#2D5F8A",   // border
                    "#4DB6AC",   // accent
                    "#FFFFFF",   // text
                    "#A0C4D8"    // muted
                ),
                "Blossom" => (
                    "#3D1A2E",   // bg        — deep plum
                    "#5C2A44",   // panel
                    "#E8A0BF",   // panel2
                    "#C06080",   // border
                    "#FF70A6",   // accent
                    "#FFFFFF",   // text
                    "#F0B8CC"    // muted
                ),
                "Candy" => (
                    "#2A1A3E",   // bg        — dark purple base
                    "#3A2255",   // panel
                    "#F9A8D4",   // panel2
                    "#7DD3FC",   // border
                    "#F472B6",   // accent
                    "#FFFFFF",   // text
                    "#BFDBFE"    // muted
                ),
                "Mocha" => (
                    "#1C1008",   // bg        — dark espresso
                    "#2E1A0E",   // panel
                    "#5C3D1E",   // panel2
                    "#7A4F2A",   // border
                    "#C68642",   // accent
                    "#FFF8F0",   // text
                    "#C4A882"    // muted
                ),
                _ => (           // "Dark" — default
                    "#111111",
                    "#1B1B1B",
                    "#222222",
                    "#2E2E2E",
                    "#2F66C8",
                    "#FFFFFF",
                    "#B8B8B8"
                )
            };

            var res = Application.Current.Resources;

            res["BgBrush"] = Brush(bg);
            res["PanelBrush"] = Brush(panel);
            res["Panel2Brush"] = Brush(panel2);
            res["BorderBrush"] = Brush(border);
            res["MutedBrush"] = Brush(muted);
            res["GoldBrush"] = Brush(accent);

            // Also update the raw Color keys so anything binding to them updates too
            res["Bg"] = Color(bg);
            res["Panel"] = Color(panel);
            res["Panel2"] = Color(panel2);
            res["Border"] = Color(border);
            res["TextMuted"] = Color(muted);
            res["Gold"] = Color(accent);

            StoreSettingsService.Set(SettingKey, themeKey);
        }

        /// <summary>Load and apply the saved theme on startup.</summary>
        public static void ApplySaved()
        {
            var saved = StoreSettingsService.Get(SettingKey, DefaultTheme);
            Apply(saved);
        }

        private static SolidColorBrush Brush(string hex) =>
            new((System.Windows.Media.Color)ColorConverter.ConvertFromString(hex));

        private static System.Windows.Media.Color Color(string hex) =>
            (System.Windows.Media.Color)ColorConverter.ConvertFromString(hex);
    }
}