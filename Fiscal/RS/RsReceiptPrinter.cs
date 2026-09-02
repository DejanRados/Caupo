using Caupo.Fiscal.Common;
using Caupo.Fiscal.RS.Models;
using QRCoder;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;

namespace Caupo.Fiscal.RS
{
    public sealed class RsReceiptPrinter
    {
        private readonly RsFiscalSettings _settings;

        public RsReceiptPrinter(RsFiscalSettings settings)
        {
            _settings = settings;
        }

        public bool Print(RsFiscalResponse response)
        {
            // Kada LPFR sam štampa račun, dodatni POS ispis nije potreban.
            if (!_settings.ExternalPrinter)
                return true;

            if (string.IsNullOrWhiteSpace(response.Journal))
                throw new FiscalException("Journal nije pronađen u LPFR odgovoru.");

            if (string.IsNullOrWhiteSpace(response.VerificationUrl))
                throw new FiscalException("Verification URL nije pronađen u LPFR odgovoru.");

            if (string.IsNullOrWhiteSpace(_settings.PosPrinter))
                throw new FiscalException("POS printer nije podešen.");

            using var qrGenerator = new QRCodeGenerator();
            using QRCodeData qrData = qrGenerator.CreateQrCode(response.VerificationUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrData);
            using Bitmap qrBitmap = qrCode.GetGraphic(20);

            using var document = new PrintDocument();

            document.PrinterSettings.PrinterName = _settings.PosPrinter;

            if (!document.PrinterSettings.IsValid)
                throw new FiscalException($"Printer '{_settings.PosPrinter}' nije dostupan.");

            document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            document.PrintPage += (_, e) => DrawReceipt(e, response.Journal, qrBitmap);

            document.Print();

            Debug.WriteLine("[RS/PRINT] Journal, QR, footer i Caupo oznaka poslani na POS printer.");

            return true;
        }

        private static void DrawReceipt(PrintPageEventArgs e, string journal, Bitmap qrBitmap)
        {
            Graphics g = e.Graphics;

            g.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);

            float hardLeft = e.PageSettings.PrintableArea.Left;
            float hardRight = e.PageBounds.Width - e.PageSettings.PrintableArea.Right;
            int sideMargin = (int)Math.Ceiling(Math.Max(hardLeft, hardRight)) + 4;

            int left = sideMargin;
            int width = Math.Max(100, e.PageBounds.Width - sideMargin * 2);
            int y = (int)Math.Ceiling(e.PageSettings.PrintableArea.Top) + 4;

            Debug.WriteLine($"[RS PRINT] Printer: {e.PageSettings.PrinterSettings.PrinterName}");
            Debug.WriteLine($"[RS PRINT] PageBounds: {e.PageBounds}");
            Debug.WriteLine($"[RS PRINT] PrintableArea: {e.PageSettings.PrintableArea}");
            Debug.WriteLine($"[RS PRINT] HardMarginX: {e.PageSettings.HardMarginX}");
            Debug.WriteLine($"[RS PRINT] HardMarginY: {e.PageSettings.HardMarginY}");
            Debug.WriteLine($"[RS PRINT LAYOUT] Left={left}");
            Debug.WriteLine($"[RS PRINT LAYOUT] Width={width}");
            Debug.WriteLine($"[RS PRINT LAYOUT] Right={left + width}");
            Debug.WriteLine($"[RS PRINT LAYOUT] Y={y}");

            using Font journalFont = CreateJournalFont(g, journal, width);
            using Font footerFont = new Font("Arial", 8f, FontStyle.Regular);
            using Font webFont = new Font("Arial", 8f, FontStyle.Regular);

            // =========================================================
            // LPFR JOURNAL
            // =========================================================

            string normalizedJournal = journal.Replace("\r\n", "\n").Replace('\r', '\n');

            string[] journalLines = normalizedJournal.Split('\n');

            float journalLineHeight = journalFont.GetHeight(g);

            foreach (string line in journalLines)
            {
                string text = line.TrimEnd();

                if (text.Length == 0)
                {
                    y += (int)Math.Ceiling(journalLineHeight);
                    continue;
                }

                g.DrawString(text, journalFont, Brushes.Black, left, y);
                y += (int)Math.Ceiling(journalLineHeight);
            }

            y += 8;

            // =========================================================
            // QR CODE - 100% RASPOLOŽIVE ŠIRINE
            // =========================================================

            int qrSize = width;
            int qrX = left;
            int qrY = y;

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(qrBitmap, new Rectangle(qrX, qrY, qrSize, qrSize));

            y += qrSize + 12;

            // =========================================================
            // KLIJENTOV FOOTER
            // =========================================================

            string footer = Properties.Settings.Default.FooterRacuna ?? string.Empty;

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

        private static Font CreateJournalFont(Graphics g, string journal, int width)
        {
            float fontSize = 9f;

            while (fontSize >= 6f)
            {
                var font = new Font("Consolas", fontSize, FontStyle.Regular);

                bool fits = true;

                string normalizedJournal = journal.Replace("\r\n", "\n").Replace('\r', '\n');
                string[] lines = normalizedJournal.Split('\n');

                foreach (string line in lines)
                {
                    if (g.MeasureString(line.TrimEnd(), font).Width > width)
                    {
                        fits = false;
                        break;
                    }
                }

                if (fits)
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
                    Debug.WriteLine("[RS PRINT] CaupoLogo.png nije pronađen.");
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
                Debug.WriteLine($"[RS PRINT] Caupo logo nije moguće ispisati: {ex.Message}");
            }
        }
    }
}