using Caupo.Fiscal.Common;
using Caupo.Fiscal.Serbia.Models;
using Caupo.Properties;
using QRCoder;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Text;

namespace Caupo.Fiscal.Serbia
{
    public sealed class SerbiaReceiptPrinter
    {
        private const int JournalWidth = 40;

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

            return PrintDocumentAsync(printerName, cancellationToken);
        }

        public Task<bool> ReprintAsync(SerbiaReceiptReprintData data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(Settings.Default.ExterniPrinter, "DA", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(true);

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Items.Count == 0)
                return Task.FromResult(false);

            if (string.IsNullOrWhiteSpace(data.FiscalNumber))
                return Task.FromResult(false);

            if (string.IsNullOrWhiteSpace(data.InvoiceCounter))
                return Task.FromResult(false);

            if (string.IsNullOrWhiteSpace(data.VerificationUrl))
                return Task.FromResult(false);

            string printerName = Settings.Default.POSPrinter ?? string.Empty;

            if (string.IsNullOrWhiteSpace(printerName))
                return Task.FromResult(false);

            _journalText = BuildReprintJournal(data);

            Debug.WriteLine("=======================================================");
            Debug.WriteLine("[SR REPRINT JOURNAL] START");
            Debug.WriteLine("=======================================================");
            Debug.WriteLine(_journalText);
            Debug.WriteLine("=======================================================");
            Debug.WriteLine("[SR REPRINT JOURNAL] END");
            Debug.WriteLine("=======================================================");

            _qrBitmap?.Dispose();
            _qrBitmap = GenerateQrCode(data.VerificationUrl);

            if (_qrBitmap == null)
            {
                Debug.WriteLine("[SR REPRINT] QR nije moguće generisati iz Verification URL-a.");
                return Task.FromResult(false);
            }

            Debug.WriteLine($"[SR REPRINT] QR generisan lokalno. Size={_qrBitmap.Width}x{_qrBitmap.Height}");

            return PrintDocumentAsync(printerName, cancellationToken);
        }

        private Task<bool> PrintDocumentAsync(string printerName, CancellationToken cancellationToken)
        {
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

        private static string BuildReprintJournal(SerbiaReceiptReprintData data)
        {
            var sb = new StringBuilder();

            sb.AppendLine(Center("ФИСКАЛНИ РАЧУН", '='));

            AppendCompanyHeader(sb);

            sb.AppendLine(LeftRight("Касир:", data.Cashier));
            sb.AppendLine(LeftRight("ЕСИР број:", data.LocalReceiptNumber.ToString(CultureInfo.InvariantCulture)));
            sb.AppendLine(LeftRight("ЕСИР време:", FormatDateTime(data.EsirDateTime)));

            if (data.IsRefund)
            {
                if (!string.IsNullOrWhiteSpace(data.ReferentDocumentNumber))
                    sb.AppendLine(LeftRight("Реф. број:", data.ReferentDocumentNumber));

                if (data.ReferentDocumentDateTime.HasValue)
                    sb.AppendLine(LeftRight("Реф. време:", FormatDateTime(data.ReferentDocumentDateTime.Value)));
            }

            sb.AppendLine(Center(data.IsRefund ? "ПРОМЕТ РЕФУНДАЦИЈА" : "ПРОМЕТ ПРОДАЈА", '-'));
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

                if (data.IsRefund)
                    total = -Math.Abs(total);

                string price = FormatAmount(item.UnitPrice);
                string quantity = FormatQuantity(item.Quantity);
                string totalText = FormatAmount(total);

                sb.AppendLine(price.PadLeft(13) + quantity.PadLeft(11) + totalText.PadLeft(16));
            }

            sb.AppendLine(new string('-', JournalWidth));

            if (data.IsRefund)
            {
                sb.AppendLine(LeftRight("Укупна рефундација:", FormatAmount(data.TotalAmount)));
                sb.AppendLine(LeftRight(GetPaymentName(data.PaymentType) + ":", FormatAmount(data.TotalAmount)));
            }
            else
            {
                sb.AppendLine(LeftRight("Укупан износ:", FormatAmount(data.TotalAmount)));
                sb.AppendLine(LeftRight(GetPaymentName(data.PaymentType) + ":", FormatAmount(data.TotalAmount)));
            }

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
            sb.AppendLine(LeftRight("ПФР број рачуна:", data.FiscalNumber));
            sb.AppendLine(LeftRight("Бројач рачуна:", data.InvoiceCounter));
            sb.AppendLine(new string('=', JournalWidth));
            sb.AppendLine(Center("КРАЈ ФИСКАЛНОГ РАЧУНА", '='));

            return sb.ToString().TrimEnd();
        }

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

            if (_qrBitmap != null)
            {
                int qrSize = Math.Min((int)(width * 0.80f), 220);
                int qrX = left + (width - qrSize) / 2;

                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(_qrBitmap, new Rectangle(qrX, y, qrSize, qrSize));

                y += qrSize + 8;
            }

            if (!string.IsNullOrWhiteSpace(endFiscalLine))
            {
                g.DrawString(endFiscalLine, font, Brushes.Black, left, y);
                y += lineHeight;
            }

            y += 10;

            string footer = Settings.Default.FooterRacuna ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(footer))
            {
                DrawCenteredMultilineText(g, footer, footerFont, left, width, ref y);
                y += 8;
            }

            DrawCaupoLogo(g, left, width, ref y);

            y += 3;
            DrawCenteredText(g, "www.caupo.app", webFont, left, width, y);

            e.HasMorePages = false;
        }

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
                Debug.WriteLine($"[SR REPRINT] Greška generisanja QR koda: {ex}");
                return null;
            }
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