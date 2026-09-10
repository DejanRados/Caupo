using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Models;
using Caupo.Models;
using Caupo.Properties;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;

namespace Caupo.Fiscal.Croatia
{
    public sealed class CroatiaReceiptPrinter
    {
        private readonly CroatiaFiscalSettings _settings;

        public CroatiaReceiptPrinter(CroatiaFiscalSettings settings)
        {
            _settings = settings;
        }

        public async Task<bool> PrintAsync(FiscalRequest request, CroatiaBuiltInvoice builtInvoice, CroatiaFiscalizationResponse fiscalization, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(_settings.PosPrinter))
                return false;

            try
            {
                Dictionary<int, decimal?> taxRates = await LoadTaxRatesAsync(cancellationToken);

                using var document = new PrintDocument();

                document.PrinterSettings.PrinterName = _settings.PosPrinter;

                if (!document.PrinterSettings.IsValid)
                    return false;

                document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                Debug.WriteLine("=======================================================");
                Debug.WriteLine($"[HR PRINT] Printer: {document.PrinterSettings.PrinterName}");
                Debug.WriteLine($"[HR PRINT] Default Paper: {document.DefaultPageSettings.PaperSize.PaperName}");
                Debug.WriteLine($"[HR PRINT] Default Paper Width: {document.DefaultPageSettings.PaperSize.Width}");
                Debug.WriteLine($"[HR PRINT] Default Paper Height: {document.DefaultPageSettings.PaperSize.Height}");
                Debug.WriteLine($"[HR PRINT] PrintableArea: {document.DefaultPageSettings.PrintableArea}");
                Debug.WriteLine($"[HR PRINT] HardMarginX: {document.DefaultPageSettings.HardMarginX}");
                Debug.WriteLine($"[HR PRINT] HardMarginY: {document.DefaultPageSettings.HardMarginY}");
                Debug.WriteLine("=======================================================");

                document.PrintPage += (_, e) => DrawReceipt(e, request, builtInvoice, fiscalization, taxRates);

                document.Print();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("=======================================================");
                Debug.WriteLine("[HR PRINT] ERROR");
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("=======================================================");

                return false;
            }
        }

        public async Task<bool> PrintStornoAsync(FiscalRequest request, CroatiaBuiltInvoice builtInvoice, CroatiaFiscalizationResponse fiscalization, string originalReceiptNumber, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(_settings.PosPrinter))
                return false;

            if (string.IsNullOrWhiteSpace(originalReceiptNumber))
                return false;

            try
            {
                Dictionary<int, decimal?> taxRates = await LoadTaxRatesAsync(cancellationToken);

                using var document = new PrintDocument();

                document.PrinterSettings.PrinterName = _settings.PosPrinter;

                if (!document.PrinterSettings.IsValid)
                    return false;

                document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                Debug.WriteLine("=======================================================");
                Debug.WriteLine($"[HR STORNO PRINT] Printer: {document.PrinterSettings.PrinterName}");
                Debug.WriteLine($"[HR STORNO PRINT] Storno: {builtInvoice.ReceiptNumberHr}");
                Debug.WriteLine($"[HR STORNO PRINT] Original: {originalReceiptNumber}");
                Debug.WriteLine("=======================================================");

                document.PrintPage += (_, e) => DrawReceipt(e, request, builtInvoice, fiscalization, taxRates, originalReceiptNumber);

                document.Print();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("=======================================================");
                Debug.WriteLine("[HR STORNO PRINT] ERROR");
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("=======================================================");

                return false;
            }
        }

        public async Task<bool> ReprintAsync(FiscalRequest request, CroatiaBuiltInvoice builtInvoice, CroatiaFiscalizationResponse fiscalization, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Debug.WriteLine($"[HR REPRINT] Ponovna štampa računa {builtInvoice.ReceiptNumberHr}");

            return await PrintAsync(request, builtInvoice, fiscalization, cancellationToken);
        }

        private static async Task<Dictionary<int, decimal?>> LoadTaxRatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var db = new AppDbContext();

                return await db.PoreskeStope.AsNoTracking().ToDictionaryAsync(x => x.IdStope, x => x.Postotak, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HR PRINT] Greška učitavanja poreskih stopa: {ex}");
                return new Dictionary<int, decimal?>();
            }
        }

        private void DrawReceipt(PrintPageEventArgs e, FiscalRequest request, CroatiaBuiltInvoice builtInvoice, CroatiaFiscalizationResponse fiscalization, IReadOnlyDictionary<int, decimal?> taxRates, string? originalReceiptNumber = null)
        {
            Graphics? g = e.Graphics;

            if (g == null)
                return;

            Debug.WriteLine("=======================================================");
            Debug.WriteLine($"[HR PRINT PAGE] PageBounds: {e.PageBounds}");
            Debug.WriteLine($"[HR PRINT PAGE] MarginBounds: {e.MarginBounds}");
            Debug.WriteLine($"[HR PRINT PAGE] PrintableArea: {e.PageSettings.PrintableArea}");
            Debug.WriteLine($"[HR PRINT PAGE] HardMarginX: {e.PageSettings.HardMarginX}");
            Debug.WriteLine($"[HR PRINT PAGE] HardMarginY: {e.PageSettings.HardMarginY}");
            Debug.WriteLine("=======================================================");

            using var normal = new Font("Consolas", 8.5f);
            using var bold = new Font("Consolas", 9f, FontStyle.Bold);
            using var itemFont = new Font("Consolas", 9.5f, FontStyle.Regular);
            using var totalFont = new Font("Consolas", 12f, FontStyle.Bold);
            using var titleFont = new Font("Consolas", 10f, FontStyle.Bold);
            using var small = new Font("Consolas", 7f);
            using var caupoWebFont = new Font("Arial", 8.5f, FontStyle.Bold);

            g.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);

            float hardLeft = e.PageSettings.PrintableArea.Left;
            float hardRight = e.PageBounds.Width - e.PageSettings.PrintableArea.Right;
            int sideMargin = (int)Math.Ceiling(Math.Max(hardLeft, hardRight)) + 4;

            int left = sideMargin;
            int width = Math.Max(100, e.PageBounds.Width - sideMargin * 2);
            int y = (int)Math.Ceiling(e.PageSettings.PrintableArea.Top) + 4;

            Debug.WriteLine($"[HR PRINT LAYOUT] Left={left}");
            Debug.WriteLine($"[HR PRINT LAYOUT] Width={width}");
            Debug.WriteLine($"[HR PRINT LAYOUT] Right={left + width}");
            Debug.WriteLine($"[HR PRINT LAYOUT] Y={y}");

            int lineHeight = (int)Math.Ceiling(normal.GetHeight(g)) + 2;
            int itemLineHeight = (int)Math.Ceiling(itemFont.GetHeight(g)) + 2;
            int smallLineHeight = (int)Math.Ceiling(small.GetHeight(g)) + 2;

            DrawCompanyLogo(g, left, width, ref y);

            DrawCentered(g, _settings.CompanyName, titleFont, left, width, ref y, lineHeight + 2);
            DrawCentered(g, _settings.Address, normal, left, width, ref y, lineHeight);
            DrawCentered(g, _settings.City, normal, left, width, ref y, lineHeight);
            DrawCentered(g, $"OIB: {_settings.Oib}", normal, left, width, ref y, lineHeight);

            y += 3;

            DrawSeparator(g, left, width, ref y);

            if (!string.IsNullOrWhiteSpace(originalReceiptNumber))
            {
                DrawCentered(g, "STORNO RAČUN", titleFont, left, width, ref y, lineHeight + 2);
                DrawSeparator(g, left, width, ref y);
                DrawLeftRight(g, "Stornirani račun:", originalReceiptNumber, normal, bold, left, width, ref y, lineHeight);
            }

            DrawLeftRight(g, "Broj računa:", builtInvoice.ReceiptNumberHr, normal, bold, left, width, ref y, lineHeight);
            DrawLeftRight(g, "Datum izdavanja:", builtInvoice.IssueDateTime.ToString("dd.MM.yyyy.", CultureInfo.InvariantCulture), normal, normal, left, width, ref y, lineHeight);
            DrawLeftRight(g, "Vrijeme izdavanja:", builtInvoice.IssueDateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture), normal, normal, left, width, ref y, lineHeight);
            DrawLeftRight(g, "Izdao:", request.Cashier.Name ?? string.Empty, normal, normal, left, width, ref y, lineHeight);

            DrawSeparator(g, left, width, ref y);

            DrawItemsHeader(g, bold, left, width, ref y, lineHeight);

            DrawSeparator(g, left, width, ref y);

            foreach (RacunStavka item in request.Items)
                DrawItem(g, item, itemFont, small, left, width, ref y, itemLineHeight, smallLineHeight, taxRates);

            DrawSeparator(g, left, width, ref y);

            DrawTotal(g, request.TotalAmount, totalFont, left, width, ref y);

            DrawSeparator(g, left, width, ref y);

            DrawLeftRight(g, "NAČIN PLAĆANJA:", PaymentName(request.PaymentType), bold, bold, left, width, ref y, lineHeight);

            DrawSeparator(g, left, width, ref y);

            DrawTaxRecap(g, builtInvoice, bold, small, left, width, ref y, smallLineHeight);

            DrawSeparator(g, left, width, ref y);

            if (fiscalization.Fiscalized && !string.IsNullOrWhiteSpace(fiscalization.Jir))
                DrawQr(g, fiscalization.Jir, builtInvoice, request.TotalAmount, left, width, ref y);

            DrawSeparator(g, left, width, ref y);

            DrawWrappedLine(g, $"ZKI: {fiscalization.Zki ?? "-"}", small, left, width, ref y, smallLineHeight);
            DrawWrappedLine(g, $"JIR: {fiscalization.Jir ?? "-"}", small, left, width, ref y, smallLineHeight);

            DrawSeparator(g, left, width, ref y);

            string footer = GetFooterText();

            if (!string.IsNullOrWhiteSpace(footer))
            {
                foreach (string footerLine in footer.Replace("\r\n", "\n").Split('\n'))
                    DrawCentered(g, footerLine, normal, left, width, ref y, lineHeight);

                DrawSeparator(g, left, width, ref y);
            }

            if (!fiscalization.Fiscalized)
            {
                DrawCentered(g, "RAČUN NIJE FISKALIZOVAN", bold, left, width, ref y, lineHeight);
                DrawCentered(g, "Potrebna je naknadna dostava računa.", small, left, width, ref y, smallLineHeight);
                DrawSeparator(g, left, width, ref y);
            }

            DrawCaupoBranding(g, caupoWebFont, left, width, ref y);
        }

        private void DrawCompanyLogo(Graphics g, int left, int width, ref int y)
        {
            if (string.IsNullOrWhiteSpace(_settings.LogoPath) || !File.Exists(_settings.LogoPath))
                return;

            try
            {
                using Image logo = Image.FromFile(_settings.LogoPath);

                int maxWidth = (int)(width * 0.75f);
                int maxHeight = 70;
                float scale = Math.Min((float)maxWidth / logo.Width, (float)maxHeight / logo.Height);

                int scaledWidth = Math.Max(1, (int)(logo.Width * scale));
                int scaledHeight = Math.Max(1, (int)(logo.Height * scale));
                int x = left + (width - scaledWidth) / 2;

                g.DrawImage(logo, new Rectangle(x, y, scaledWidth, scaledHeight));

                y += scaledHeight + 6;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HR PRINT] Logo firme error: {ex.Message}");
            }
        }

        private static void DrawItemsHeader(Graphics g, Font font, int left, int width, ref int y, int lineHeight)
        {
            g.DrawString("Naziv", font, Brushes.Black, left, y);
            y += lineHeight;

            float quantityX = left + width * 0.18f;
            float multiplicationX = left + width * 0.23f;
            float priceX = left + width * 0.48f;
            float taxX = left + width * 0.68f;
            float amountX = left + width;

            DrawRightAligned(g, "Kol", font, quantityX, y);
            g.DrawString("x", font, Brushes.Black, multiplicationX, y);
            DrawRightAligned(g, "Cijena", font, priceX, y);
            DrawRightAligned(g, "Por", font, taxX, y);
            DrawRightAligned(g, "Iznos", font, amountX, y);

            y += lineHeight;
        }

        private static void DrawItem(Graphics g, RacunStavka item, Font itemFont, Font small, int left, int width, ref int y, int itemLineHeight, int smallLineHeight, IReadOnlyDictionary<int, decimal?> taxRates)
        {
            string name = item.Name ?? string.Empty;

            DrawWrappedLine(g, name, itemFont, left, width, ref y, itemLineHeight);

            decimal quantity = item.Quantity ?? 0m;
            decimal price = item.UnitPrice ?? 0m;
            decimal amount = item.TotalAmount ?? quantity * price;
            string taxLabel = GetItemTaxLabel(item, taxRates);

            float quantityX = left + width * 0.18f;
            float multiplicationX = left + width * 0.23f;
            float priceX = left + width * 0.48f;
            float taxX = left + width * 0.68f;
            float amountX = left + width;

            DrawRightAligned(g, quantity.ToString("0.###", CultureInfo.CurrentCulture), itemFont, quantityX, y);
            g.DrawString("x", itemFont, Brushes.Black, multiplicationX, y);
            DrawRightAligned(g, price.ToString("0.00", CultureInfo.CurrentCulture), itemFont, priceX, y);
            DrawRightAligned(g, taxLabel, itemFont, taxX, y);
            DrawRightAligned(g, amount.ToString("0.00", CultureInfo.CurrentCulture), itemFont, amountX, y);

            y += itemLineHeight;

            if (!string.IsNullOrWhiteSpace(item.Note))
                DrawWrappedLine(g, $"  {item.Note}", small, left, width, ref y, smallLineHeight);

            y += 2;
        }

        private static void DrawTotal(Graphics g, decimal totalAmount, Font font, int left, int width, ref int y)
        {
            string label = "UKUPNO";
            string amount = totalAmount.ToString("0.00", CultureInfo.CurrentCulture);

            g.DrawString(label, font, Brushes.Black, left, y);

            SizeF amountSize = g.MeasureString(amount, font);
            g.DrawString(amount, font, Brushes.Black, left + width - amountSize.Width, y);

            y += (int)Math.Ceiling(font.GetHeight(g)) + 5;
        }

        private static void DrawTaxRecap(Graphics g, CroatiaBuiltInvoice builtInvoice, Font bold, Font small, int left, int width, ref int y, int lineHeight)
        {
            g.DrawString("REKAPITULACIJA POREZA", bold, Brushes.Black, left, y);
            y += (int)Math.Ceiling(bold.GetHeight(g)) + 3;

            float rateX = left;
            float baseX = left + width * 0.48f;
            float taxX = left + width * 0.76f;
            float totalX = left + width;

            g.DrawString("Stopa", small, Brushes.Black, rateX, y);
            DrawRightAligned(g, "Osnovica", small, baseX, y);
            DrawRightAligned(g, "Porez", small, taxX, y);
            DrawRightAligned(g, "Ukupno", small, totalX, y);

            y += lineHeight;

            foreach (var tax in builtInvoice.Taxes.Where(x => !x.IsConsumptionTax))
            {
                decimal total = tax.BaseAmount + tax.TaxAmount;

                g.DrawString($"{tax.Rate:0.##}%", small, Brushes.Black, rateX, y);
                DrawRightAligned(g, tax.BaseAmount.ToString("0.00", CultureInfo.CurrentCulture), small, baseX, y);
                DrawRightAligned(g, tax.TaxAmount.ToString("0.00", CultureInfo.CurrentCulture), small, taxX, y);
                DrawRightAligned(g, total.ToString("0.00", CultureInfo.CurrentCulture), small, totalX, y);

                y += lineHeight;
            }

            var consumptionTaxes = builtInvoice.Taxes.Where(x => x.IsConsumptionTax).ToList();

            if (consumptionTaxes.Count > 0)
            {
                y += 2;

                g.DrawString("POREZ NA POTROŠNJU", bold, Brushes.Black, left, y);
                y += (int)Math.Ceiling(bold.GetHeight(g)) + 3;

                foreach (var tax in consumptionTaxes)
                {
                    g.DrawString($"{tax.Rate:0.##}%", small, Brushes.Black, rateX, y);
                    DrawRightAligned(g, tax.BaseAmount.ToString("0.00", CultureInfo.CurrentCulture), small, baseX, y);
                    DrawRightAligned(g, tax.TaxAmount.ToString("0.00", CultureInfo.CurrentCulture), small, taxX, y);

                    y += lineHeight;
                }
            }
        }

        private static void DrawQr(Graphics g, string jir, CroatiaBuiltInvoice builtInvoice, decimal totalAmount, int left, int width, ref int y)
        {
            string datv = builtInvoice.IssueDateTime.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
            long amountInCents = (long)Math.Round(totalAmount * 100m, 0, MidpointRounding.AwayFromZero);

            string qrText = "https://porezna.gov.hr/rn" +
                            $"?jir={Uri.EscapeDataString(jir)}" +
                            $"&datv={datv}" +
                            $"&izn={amountInCents}";

            using var generator = new QRCodeGenerator();
            using QRCodeData qrData = generator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            using var qr = new QRCode(qrData);
            using Bitmap bitmap = qr.GetGraphic(12);

            int qrSize = Math.Min((int)(width * 0.68f), 190);
            int x = left + (width - qrSize) / 2;

            g.DrawImage(bitmap, new Rectangle(x, y, qrSize, qrSize));

            y += qrSize + 6;
        }

        private static void DrawCaupoBranding(Graphics g, Font webFont, int left, int width, ref int y)
        {
            y += 4;

            try
            {
                using Bitmap? logo = LoadCaupoLogo();

                if (logo != null)
                {
                    int maxWidth = (int)(width * 0.49f);
                    int maxHeight = 40;
                    float scale = Math.Min((float)maxWidth / logo.Width, (float)maxHeight / logo.Height);

                    int logoWidth = Math.Max(1, (int)(logo.Width * scale));
                    int logoHeight = Math.Max(1, (int)(logo.Height * scale));
                    int logoX = left + (width - logoWidth) / 2;

                    g.DrawImage(logo, new Rectangle(logoX, y, logoWidth, logoHeight));

                    y += logoHeight + 3;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HR PRINT] Caupo logo error: {ex.Message}");
            }

            DrawCentered(g, "www.caupo.app", webFont, left, width, ref y, (int)Math.Ceiling(webFont.GetHeight(g)) + 2);

            y += 5;
        }

        private static Bitmap? LoadCaupoLogo()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Images/CaupoLogo.png", UriKind.Absolute);
                var resource = System.Windows.Application.GetResourceStream(uri);

                if (resource == null)
                    return null;

                using Stream stream = resource.Stream;
                using Image image = Image.FromStream(stream);

                return new Bitmap(image);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HR PRINT] Nije moguće učitati Caupo logo: {ex.Message}");
                return null;
            }
        }

        private static void DrawLeftRight(Graphics g, string leftText, string rightText, Font leftFont, Font rightFont, int left, int width, ref int y, int lineHeight)
        {
            g.DrawString(leftText, leftFont, Brushes.Black, left, y);

            SizeF rightSize = g.MeasureString(rightText, rightFont);
            g.DrawString(rightText, rightFont, Brushes.Black, left + width - rightSize.Width, y);

            y += lineHeight;
        }

        private static void DrawRightAligned(Graphics g, string text, Font font, float rightX, float y)
        {
            SizeF size = g.MeasureString(text, font);
            g.DrawString(text, font, Brushes.Black, rightX - size.Width, y);
        }

        private static void DrawCentered(Graphics g, string? text, Font font, int left, int width, ref int y, int lineHeight)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            SizeF size = g.MeasureString(text, font);
            float x = left + (width - size.Width) / 2;

            g.DrawString(text, font, Brushes.Black, x, y);

            y += lineHeight;
        }

        private static void DrawWrappedLine(Graphics g, string text, Font font, int left, int width, ref int y, int lineHeight)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string currentLine = string.Empty;

            foreach (string word in words)
            {
                string candidate = string.IsNullOrWhiteSpace(currentLine) ? word : $"{currentLine} {word}";

                if (g.MeasureString(candidate, font).Width <= width)
                {
                    currentLine = candidate;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(currentLine))
                {
                    g.DrawString(currentLine, font, Brushes.Black, left, y);
                    y += lineHeight;
                }

                currentLine = word;
            }

            if (!string.IsNullOrWhiteSpace(currentLine))
            {
                g.DrawString(currentLine, font, Brushes.Black, left, y);
                y += lineHeight;
            }
        }

        private static void DrawSeparator(Graphics g, int left, int width, ref int y)
        {
            y += 2;

            using var pen = new Pen(Color.Black, 1f);

            g.DrawLine(pen, left, y, left + width, y);

            y += 6;
        }

        private static string GetItemTaxLabel(RacunStavka item, IReadOnlyDictionary<int, decimal?> taxRates)
        {
            if (!item.PoreskaStopa.HasValue)
                return "-";

            if (!taxRates.TryGetValue(item.PoreskaStopa.Value, out decimal? rate) || !rate.HasValue)
                return "-";

            return $"{rate.Value.ToString("0.##", CultureInfo.CurrentCulture)}%";
        }

        private static string GetFooterText()
        {
            try
            {
                return Settings.Default.FooterRacuna?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string PaymentName(FiscalPaymentType paymentType)
        {
            return paymentType switch
            {
                FiscalPaymentType.Cash => "GOTOVINA",
                FiscalPaymentType.Card => "KARTICA",
                FiscalPaymentType.WireTransfer => "TRANSAKCIJSKI RAČUN",
                FiscalPaymentType.Other => "OSTALO",
                FiscalPaymentType.Check => "ČEK",
                _ => "NEPOZNATO"
            };
        }
    }
}