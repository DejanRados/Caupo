using Caupo.Fiscal.Common;
using Caupo.Fiscal.RS.Models;
using Caupo.Properties;
using QRCoder;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Text;

namespace Caupo.Fiscal.RS
{
    public sealed class RsReceiptPrinter
    {
        private const int JournalWidth = 40;

        private readonly RsFiscalSettings _settings;

        private string _journalText = string.Empty;
        private Bitmap? _qrBitmap;

        public RsReceiptPrinter(RsFiscalSettings settings)
        {
            _settings = settings;
        }

        // ============================================================
        // NORMALNI ISPIS NAKON FISKALIZACIJE
        // ============================================================

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

            _journalText = response.Journal;

            Debug.WriteLine("=======================================================");
            Debug.WriteLine("[RS JOURNAL] START");
            Debug.WriteLine("=======================================================");
            Debug.WriteLine(_journalText);
            Debug.WriteLine("=======================================================");
            Debug.WriteLine("[RS JOURNAL] END");
            Debug.WriteLine("=======================================================");

            _qrBitmap?.Dispose();
            _qrBitmap = GenerateQrCode(response.VerificationUrl);

            if (_qrBitmap == null)
                throw new FiscalException("QR kod nije moguće generisati iz Verification URL-a.");

            return PrintDocument(_settings.PosPrinter);
        }

        // ============================================================
        // PONOVNI LOKALNI ISPIS
        // ============================================================

        public bool Reprint(RsReceiptReprintData data)
        {
            // Ako LPFR štampa račun, lokalni reprint nije moguć.
            if (!_settings.ExternalPrinter)
                return false;

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Items.Count == 0)
                return false;

            if (string.IsNullOrWhiteSpace(data.FiscalReceiptNumber))
                return false;

            if (string.IsNullOrWhiteSpace(data.TotalCounter))
                return false;

            if (string.IsNullOrWhiteSpace(data.VerificationUrl))
                return false;

            if (string.IsNullOrWhiteSpace(_settings.PosPrinter))
                return false;

            _journalText = BuildReprintJournal(data);

            Debug.WriteLine("=======================================================");
            Debug.WriteLine("[RS REPRINT JOURNAL] START");
            Debug.WriteLine("=======================================================");
            Debug.WriteLine(_journalText);
            Debug.WriteLine("=======================================================");
            Debug.WriteLine("[RS REPRINT JOURNAL] END");
            Debug.WriteLine("=======================================================");

            _qrBitmap?.Dispose();
            _qrBitmap = GenerateQrCode(data.VerificationUrl);

            if (_qrBitmap == null)
            {
                Debug.WriteLine("[RS REPRINT] QR nije moguće generisati iz Verification URL-a.");
                return false;
            }

            Debug.WriteLine($"[RS REPRINT] QR generisan lokalno. Size={_qrBitmap.Width}x{_qrBitmap.Height}");

            return PrintDocument(_settings.PosPrinter);
        }

        // ============================================================
        // PRINT DOCUMENT
        // ============================================================

        private bool PrintDocument(string printerName)
        {
            using var document = new PrintDocument();

            document.PrinterSettings.PrinterName = printerName;

            if (!document.PrinterSettings.IsValid)
            {
                _qrBitmap?.Dispose();
                _qrBitmap = null;

                Debug.WriteLine($"[RS PRINT] Printer '{printerName}' nije pronađen.");
                return false;
            }

            document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            document.PrintPage += OnPrintPage;

            DebugPrinterSettings(document);

            try
            {
                document.Print();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RS PRINT] Greška štampe: {ex}");
                return false;
            }
            finally
            {
                document.PrintPage -= OnPrintPage;

                _qrBitmap?.Dispose();
                _qrBitmap = null;
            }
        }

        // ============================================================
        // REKONSTRUKCIJA JOURNALA
        // ============================================================

        private static string BuildReprintJournal(RsReceiptReprintData data)
        {
            var sb = new StringBuilder();

            sb.AppendLine(Center("ФИСКАЛНИ РАЧУН", '='));

            AppendCompanyHeader(sb);

            sb.AppendLine(LeftRight("Касир:", data.Cashier));
            sb.AppendLine(LeftRight("ЕСИР број:", data.LocalReceiptNumber.ToString(CultureInfo.InvariantCulture)));
            sb.AppendLine(LeftRight("ЕСИР време:", FormatDateTime(data.EsirDateTime)));

            sb.AppendLine(Center("ПРОМЕТ ПРОДАЈА", '-'));
            sb.AppendLine("Артикли");
            sb.AppendLine(new string('=', JournalWidth));
            sb.AppendLine("Назив   Цена         Кол.         Укупно");

            foreach (var item in data.Items)
            {
                string itemName = item.Name;

                if (!string.IsNullOrWhiteSpace(item.TaxLabel))
                    itemName += $" ({item.TaxLabel})";

                foreach (string line in Wrap(itemName, JournalWidth))
                    sb.AppendLine(line);

                decimal total = Math.Round(item.Quantity * item.UnitPrice, 2, MidpointRounding.AwayFromZero);

                string price = FormatAmount(item.UnitPrice);
                string quantity = FormatQuantity(item.Quantity);
                string totalText = FormatAmount(total);

                sb.AppendLine(
                    price.PadLeft(13) +
                    quantity.PadLeft(11) +
                    totalText.PadLeft(16));
            }

            sb.AppendLine(new string('-', JournalWidth));
            sb.AppendLine(LeftRight("Укупан износ:", FormatAmount(data.TotalAmount)));
            sb.AppendLine(LeftRight(GetPaymentName(data.PaymentType) + ":", FormatAmount(data.TotalAmount)));
            sb.AppendLine(new string('=', JournalWidth));
            sb.AppendLine("Ознака       Име      Стопа        Порез");

            var taxes = data.Items
                .GroupBy(x => new
                {
                    Label = x.TaxLabel,
                    Name = x.TaxName,
                    Rate = x.TaxRate
                })
                .Select(group =>
                {
                    decimal tax = group.Sum(item =>
                    {
                        decimal gross = Math.Round(item.Quantity * item.UnitPrice, 2, MidpointRounding.AwayFromZero);

                        if (item.TaxRate <= 0m)
                            return 0m;

                        return gross - gross / (1m + item.TaxRate / 100m);
                    });

                    return new
                    {
                        group.Key.Label,
                        group.Key.Name,
                        group.Key.Rate,
                        Tax = Math.Round(tax, 2, MidpointRounding.AwayFromZero)
                    };
                })
                .OrderBy(x => x.Label)
                .ToList();

            foreach (var tax in taxes)
            {
                string label = Limit(tax.Label, 6).PadRight(6);
                string name = Limit(tax.Name, 10).PadLeft(10);
                string rate = (FormatAmount(tax.Rate) + "%").PadLeft(9);
                string amount = FormatAmount(tax.Tax).PadLeft(13);

                sb.AppendLine(label + name + rate + amount);
            }

            sb.AppendLine(new string('-', JournalWidth));

            decimal totalTax = taxes.Sum(x => x.Tax);

            sb.AppendLine(LeftRight("Укупан износ пореза:", FormatAmount(totalTax)));
            sb.AppendLine(new string('=', JournalWidth));
            sb.AppendLine(LeftRight("ПФР време:", FormatDateTime(data.PfrDateTime)));
            sb.AppendLine(LeftRight("ПФР број рачуна:", data.FiscalReceiptNumber));
            sb.AppendLine(LeftRight("Бројач рачуна:", data.TotalCounter));
            sb.AppendLine(new string('=', JournalWidth));
            sb.AppendLine(Center("КРАЈ ФИСКАЛНОГ РАЧУНА", '='));

            return sb.ToString().TrimEnd();
        }

        // ============================================================
        // ZAGLAVLJE
        // ============================================================

        private static void AppendCompanyHeader(StringBuilder sb)
        {
            string pib = Settings.Default.PDV ?? string.Empty;
            string firma = Settings.Default.Firma ?? string.Empty;
            string adresa = Settings.Default.Adresa ?? string.Empty;
            string mjesto = Settings.Default.Mjesto ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(pib))
                sb.AppendLine(Center(pib));

            if (!string.IsNullOrWhiteSpace(firma))
                sb.AppendLine(Center(firma));

            if (!string.IsNullOrWhiteSpace(firma))
                sb.AppendLine(Center(firma));

            if (!string.IsNullOrWhiteSpace(adresa))
                sb.AppendLine(Center(adresa));

            if (!string.IsNullOrWhiteSpace(mjesto))
                sb.AppendLine(Center(mjesto));
        }

        // ============================================================
        // FORMATIRANJE
        // ============================================================

        private static string GetPaymentName(FiscalPaymentType paymentType)
        {
            return paymentType switch
            {
                FiscalPaymentType.Cash => "Готовина",
                FiscalPaymentType.Card => "Картица",
                FiscalPaymentType.Check => "Чек",
                FiscalPaymentType.WireTransfer => "Пренос на рачун",
                _ => "Друго"
            };
        }

        private static string FormatDateTime(DateTime value)
        {
            return value.ToString("d.M.yyyy. HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static string FormatAmount(decimal value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');
        }

        private static string FormatQuantity(decimal value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', ',');
        }

        private static string LeftRight(string left, string right)
        {
            left ??= string.Empty;
            right ??= string.Empty;

            if (left.Length + right.Length >= JournalWidth)
            {
                int maxRight = Math.Max(1, JournalWidth - left.Length - 1);
                right = Limit(right, maxRight);
            }

            return left + new string(' ', Math.Max(1, JournalWidth - left.Length - right.Length)) + right;
        }

        private static string Center(string text, char fill = ' ')
        {
            text = text?.Trim() ?? string.Empty;

            if (fill != ' ')
                text = $" {text} ";

            if (text.Length >= JournalWidth)
                return Limit(text, JournalWidth);

            int remaining = JournalWidth - text.Length;
            int left = remaining / 2;
            int right = remaining - left;

            return new string(fill, left) + text + new string(fill, right);
        }

        private static IEnumerable<string> Wrap(string text, int width)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield return string.Empty;
                yield break;
            }

            int index = 0;

            while (index < text.Length)
            {
                int length = Math.Min(width, text.Length - index);

                yield return text.Substring(index, length);

                index += length;
            }
        }

        private static string Limit(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Length <= maxLength ? value : value[..maxLength];
        }

        // ============================================================
        // CRTANJE
        // ============================================================

        private void OnPrintPage(object? sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;

            g.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);

            float hardLeft = e.PageSettings.PrintableArea.Left;
            float hardRight = e.PageBounds.Width - e.PageSettings.PrintableArea.Right;

            int sideMargin = (int)Math.Ceiling(Math.Max(hardLeft, hardRight)) + 4;
            int left = sideMargin;
            int width = Math.Max(100, e.PageBounds.Width - sideMargin * 2);
            int y = (int)Math.Ceiling(e.PageSettings.PrintableArea.Top) + 4;

            using var font = CreateJournalFont(g, width);
            using var footerFont = new Font("Arial", 8f, FontStyle.Regular);
            using var webFont = new Font("Arial", 8f, FontStyle.Regular);

            string normalizedJournal = _journalText.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] journalLines = normalizedJournal.Split('\n');
            int lineHeight = (int)Math.Ceiling(font.GetHeight(g));

            string? endFiscalLine = null;

            foreach (string line in journalLines)
            {
                // Završna linija se namjerno ne štampa ovdje.
                // Štampa se tek nakon QR koda.
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
            // QR
            // =========================================================

            if (_qrBitmap != null)
            {
                int qrSize = width;
                int qrX = left;

                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(_qrBitmap, new Rectangle(qrX, y, qrSize, qrSize));

                y += qrSize + 8;
            }

            // =========================================================
            // KRAJ FISKALNOG RAČUNA - POSLIJE QR-a
            // =========================================================

            if (!string.IsNullOrWhiteSpace(endFiscalLine))
            {
                g.DrawString(endFiscalLine, font, Brushes.Black, left, y);
                y += lineHeight;
            }

            y += 10;

            // =========================================================
            // FOOTER
            // =========================================================

            string footer = Settings.Default.FooterRacuna ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(footer))
            {
                DrawCenteredMultilineText(g, footer, footerFont, left, width, ref y);
                y += 8;
            }

            // =========================================================
            // CAUPO
            // =========================================================

            DrawCaupoLogo(g, left, width, ref y);

            y += 3;
            DrawCenteredText(g, "www.caupo.app", webFont, left, width, y);

            e.HasMorePages = false;
        }

        // ============================================================
        // QR
        // ============================================================

        private static Bitmap? GenerateQrCode(string verificationUrl)
        {
            try
            {
                using var generator = new QRCodeGenerator();
                using QRCodeData data = generator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.M);
                using var qrCode = new QRCode(data);

                return qrCode.GetGraphic(20);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RS PRINT] Greška generisanja QR koda: {ex}");
                return null;
            }
        }

        // ============================================================
        // FONT
        // ============================================================

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

        // ============================================================
        // CENTRIRANI TEKST
        // ============================================================

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

        // ============================================================
        // CAUPO LOGO
        // ============================================================

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

        // ============================================================
        // DEBUG
        // ============================================================

        private static void DebugPrinterSettings(PrintDocument document)
        {
            try
            {
                PageSettings settings = document.DefaultPageSettings;

                Debug.WriteLine("=======================================================");
                Debug.WriteLine($"[RS PRINT] Printer: {document.PrinterSettings.PrinterName}");
                Debug.WriteLine($"[RS PRINT] Default Paper: {settings.PaperSize.PaperName}");
                Debug.WriteLine($"[RS PRINT] Default Paper Width: {settings.PaperSize.Width}");
                Debug.WriteLine($"[RS PRINT] Default Paper Height: {settings.PaperSize.Height}");
                Debug.WriteLine($"[RS PRINT] PrintableArea: {settings.PrintableArea}");
                Debug.WriteLine($"[RS PRINT] HardMarginX: {settings.HardMarginX}");
                Debug.WriteLine($"[RS PRINT] HardMarginY: {settings.HardMarginY}");
                Debug.WriteLine("=======================================================");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RS PRINT] Debug printer settings greška: {ex.Message}");
            }
        }
    }
}