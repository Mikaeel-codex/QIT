using PointofSale.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace PointofSale.Services
{
    /// <summary>
    /// Generates a receipt PDF by calling the bundled Python script.
    /// The script (generate_receipt.py) must be in the same folder as the .exe,
    /// or in a subfolder called "Scripts".
    /// </summary>
    public static class ReceiptPdfService
    {
        public static string Generate(ReceiptData data)
        {
            // ── Locate the Python script ──────────────────────────────────
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var scriptPath = Path.Combine(baseDir, "Scripts", "generate_receipt.py");
            if (!File.Exists(scriptPath))
                scriptPath = Path.Combine(baseDir, "generate_receipt.py");
            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("generate_receipt.py not found. Place it in the app folder or a Scripts subfolder.");

            // ── Write JSON data to temp file ──────────────────────────────
            var tempJson = Path.Combine(Path.GetTempPath(), $"receipt_{Guid.NewGuid():N}.json");
            var outPdf = Path.Combine(Path.GetTempPath(), $"receipt_{data.ReceiptNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(tempJson, json);

            // ── Run Python ────────────────────────────────────────────────
            var psi = new ProcessStartInfo
            {
                FileName = FindPython(),
                Arguments = $"\"{scriptPath}\" \"{tempJson}\" \"{outPdf}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi) ?? throw new Exception("Failed to start Python process.");
            proc.WaitForExit(15_000);

            var stderr = proc.StandardError.ReadToEnd();
            if (proc.ExitCode != 0 || !File.Exists(outPdf))
                throw new Exception($"PDF generation failed.\n{stderr}");

            // Clean up temp JSON
            try { File.Delete(tempJson); } catch { }

            return outPdf;
        }

        private static string FindPython()
        {
            // Try common names in order
            foreach (var name in new[] { "python3", "python", "py" })
            {
                try
                {
                    var p = Process.Start(new ProcessStartInfo
                    {
                        FileName = name,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    });
                    p?.WaitForExit(3000);
                    if (p?.ExitCode == 0) return name;
                }
                catch { }
            }
            throw new Exception("Python not found. Please install Python 3 and ensure it is on your PATH.");
        }
    }
}