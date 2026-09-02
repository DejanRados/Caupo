using Caupo.Fiscal.Serbia.Models;
using Caupo.Properties;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;

namespace Caupo.Fiscal.Serbia
{
    public sealed class SerbiaReceiptPrinter
    {
        private string _journalText = string.Empty;
        private Bitmap? _qrBitmap;

        public Task<bool> PrintAsync(SerbiaInvoiceResponse response, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(Settings.Default.ExterniPrinter, "DA", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(true);

            if (string.IsNullOrWhiteSpace(response.Journal))
                return Task.FromResult(false);

            string printerName = Settings.Default.POSPrinter ?? string.Empty;

            if (string.IsNullOrWhiteSpace(printerName))
                return Task.FromResult(false);

            _journalText = response.Journal;

            Debug.WriteLine("=======================================================");
            Debug.WriteLine("[SR JOURNAL] START");
            Debug.WriteLine("=======================================================");
            Debug.WriteLine(_journalText);
            Debug.WriteLine("=======================================================");
            Debug.WriteLine("[SR JOURNAL] END");
            Debug.WriteLine("=======================================================");

            _qrBitmap?.Dispose();
            _qrBitmap = null;

            if (!string.IsNullOrWhiteSpace(response.VerificationQRCode))
            {
                _qrBitmap = LoadVerificationQrCode(response.VerificationQRCode);

                if (_qrBitmap != null)
                    Debug.WriteLine($"[SR PRINT] Originalni V-SDC QR učitan. Size={_qrBitmap.Width}x{_qrBitmap.Height}");
                else
                    Debug.WriteLine("[SR PRINT] Originalni V-SDC QR nije moguće učitati.");
            }
            else
            {
                Debug.WriteLine("[SR PRINT] V-SDC odgovor ne sadrži verificationQRCode.");
            }

            using var document = new PrintDocument();

            document.PrinterSettings.PrinterName = printerName;

            if (!document.PrinterSettings.IsValid)
            {
                _qrBitmap?.Dispose();
                _qrBitmap = null;

                Debug.WriteLine($"[SR PRINT] Printer '{printerName}' nije pronađen.");
                return Task.FromResult(false);
            }

            document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            document.PrintPage += OnPrintPage;

            DebugPrinterSettings(document);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                document.Print();

                return Task.FromResult(true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SR PRINT] Greška štampe: {ex}");
                return Task.FromResult(false);
            }
            finally
            {
                document.PrintPage -= OnPrintPage;

                _qrBitmap?.Dispose();
                _qrBitmap = null;
            }
        }

        private void OnPrintPage(object? sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;

            g.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);

            float hardLeft = e.PageSettings.PrintableArea.Left;
            float hardRight = e.PageBounds.Width - e.PageSettings.PrintableArea.Right;

            int sideMargin = (int)Math.Ceiling(Math.Max(hardLeft, hardRight)) + 4;
            int left = sideMargin;
            int width = Math.Max(100, e.PageBounds.Width - sideMargin * 2);
            int right = left + width;
            int y = (int)Math.Ceiling(e.PageSettings.PrintableArea.Top) + 4;

            using var font = CreateJournalFont(g, width);
            using var footerFont = new Font("Arial", 8f, FontStyle.Regular);
            using var webFont = new Font("Arial", 8f, FontStyle.Regular);

            Debug.WriteLine("=======================================================");
            Debug.WriteLine($"[SR PRINT PAGE] PageBounds: {e.PageBounds}");
            Debug.WriteLine($"[SR PRINT PAGE] MarginBounds: {e.MarginBounds}");
            Debug.WriteLine($"[SR PRINT PAGE] PrintableArea: {e.PageSettings.PrintableArea}");
            Debug.WriteLine($"[SR PRINT PAGE] HardMarginX: {e.PageSettings.HardMarginX}");
            Debug.WriteLine($"[SR PRINT PAGE] HardMarginY: {e.PageSettings.HardMarginY}");
            Debug.WriteLine($"[SR PRINT LAYOUT] Left={left}");
            Debug.WriteLine($"[SR PRINT LAYOUT] Width={width}");
            Debug.WriteLine($"[SR PRINT LAYOUT] Right={right}");
            Debug.WriteLine($"[SR PRINT LAYOUT] Y={y}");
            Debug.WriteLine($"[SR PRINT LAYOUT] JournalFont={font.Size:0.##}");
            Debug.WriteLine("=======================================================");

            string normalizedJournal = _journalText.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] journalLines = normalizedJournal.Split('\n');
            int lineHeight = (int)Math.Ceiling(font.GetHeight(g));

            string? endFiscalLine = null;

            foreach (string line in journalLines)
            {
                if (line.Contains("КРАЈ ФИСКАЛНОГ РАЧУНА", StringComparison.OrdinalIgnoreCase) ||
                   line.Contains("END OF FISCAL INVOICE", StringComparison.OrdinalIgnoreCase))
                {
                    endFiscalLine = line;
                    continue;
                }

                if (line.Length > 0)
                    g.DrawString(line, font, Brushes.Black, left, y);

                y += lineHeight;
            }

            y += 8;

            // =========================================================
            // ORIGINALNI V-SDC QR
            // =========================================================

            if (_qrBitmap != null)
            {
                int qrSize = Math.Min((int)(width * 0.80f), 220);
                int qrX = left + (width - qrSize) / 2;

                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(_qrBitmap, new Rectangle(qrX, y, qrSize, qrSize));

                Debug.WriteLine($"[SR PRINT QR] X={qrX}, Y={y}, Size={qrSize}");

                y += qrSize + 8;
            }

            // =========================================================
            // KRAJ FISKALNOG RAČUNA
            // =========================================================

            if (!string.IsNullOrWhiteSpace(endFiscalLine))
            {
                g.DrawString(endFiscalLine, font, Brushes.Black, left, y);
                y += lineHeight;
            }

            y += 10;

            // =========================================================
            // KLIJENTOV FOOTER
            // =========================================================

            string footer = Settings.Default.FooterRacuna ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(footer))
            {
                DrawCenteredMultilineText(g, footer, footerFont, left, width, ref y);
                y += 8;
            }

            // =========================================================
            // CAUPO LOGO
            // =========================================================

            DrawCaupoLogo(g, left, width, ref y);

            // =========================================================
            // WWW.CAUPO.APP
            // =========================================================

            y += 3;
            DrawCenteredText(g, "www.caupo.app", webFont, left, width, y);
            y += (int)Math.Ceiling(webFont.GetHeight(g)) + 8;

            e.HasMorePages = false;
        }

        private static Bitmap? LoadVerificationQrCode(string base64)
        {
            try
            {
                string value = base64.Trim();
                int commaIndex = value.IndexOf(',');

                if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
                    value = value[(commaIndex + 1)..];

                byte[] bytes = Convert.FromBase64String(value);

                using var stream = new MemoryStream(bytes);
                using Image image = Image.FromStream(stream);

                return new Bitmap(image);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SR PRINT] Greška učitavanja verificationQRCode: {ex}");
                return null;
            }
        }

        private static Font CreateJournalFont(Graphics g, int width)
        {
            const string testLine = "========================================";
            float fontSize = 9f;

            while (fontSize >= 6f)
            {
                var font = new Font("Consolas", fontSize, FontStyle.Regular);

                if (g.MeasureString(testLine, font).Width <= width)
                    return font;

                font.Dispose();
                fontSize -= 0.25f;
            }

            return new Font("Consolas", 6f, FontStyle.Regular);
        }

        private static void DrawCenteredText(Graphics g, string text, Font font, int left, int width, int y)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            SizeF size = g.MeasureString(text, font);
            float x = left + Math.Max(0, (width - size.Width) / 2f);

            g.DrawString(text, font, Brushes.Black, x, y);
        }

        private static void DrawCenteredMultilineText(Graphics g, string text, Font font, int left, int width, ref int y)
        {
            string normalizedText = text.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalizedText.Split('\n');
            int lineHeight = (int)Math.Ceiling(font.GetHeight(g));

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    y += lineHeight;
                    continue;
                }

                DrawCenteredText(g, line.Trim(), font, left, width, y);
                y += lineHeight;
            }
        }

        private static void DrawCaupoLogo(Graphics g, int left, int width, ref int y)
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Images/CaupoLogo.png", UriKind.Absolute);
                var resource = System.Windows.Application.GetResourceStream(uri);

                if (resource == null)
                {
                    Debug.WriteLine("[SR PRINT] CaupoLogo.png nije pronađen.");
                    return;
                }

                using Stream stream = resource.Stream;
                using Image logo = Image.FromStream(stream);

                int maxWidth = (int)(width * 0.39f);
                int maxHeight = 32;

                float scaleX = (float)maxWidth / logo.Width;
                float scaleY = (float)maxHeight / logo.Height;
                float scale = Math.Min(scaleX, scaleY);

                int logoWidth = Math.Max(1, (int)(logo.Width * scale));
                int logoHeight = Math.Max(1, (int)(logo.Height * scale));
                int logoX = left + (width - logoWidth) / 2;

                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(logo, new Rectangle(logoX, y, logoWidth, logoHeight));

                y += logoHeight;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SR PRINT] Caupo logo nije moguće ispisati: {ex.Message}");
            }
        }

        private static void DebugPrinterSettings(PrintDocument document)
        {
            try
            {
                PageSettings settings = document.DefaultPageSettings;

                Debug.WriteLine("=======================================================");
                Debug.WriteLine($"[SR PRINT] Printer: {document.PrinterSettings.PrinterName}");
                Debug.WriteLine($"[SR PRINT] Default Paper: {settings.PaperSize.PaperName}");
                Debug.WriteLine($"[SR PRINT] Default Paper Width: {settings.PaperSize.Width}");
                Debug.WriteLine($"[SR PRINT] Default Paper Height: {settings.PaperSize.Height}");
                Debug.WriteLine($"[SR PRINT] PrintableArea: {settings.PrintableArea}");
                Debug.WriteLine($"[SR PRINT] HardMarginX: {settings.HardMarginX}");
                Debug.WriteLine($"[SR PRINT] HardMarginY: {settings.HardMarginY}");
                Debug.WriteLine("=======================================================");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SR PRINT] Debug printer settings greška: {ex.Message}");
            }
        }
    }
}