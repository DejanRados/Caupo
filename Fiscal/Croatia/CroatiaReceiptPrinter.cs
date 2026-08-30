using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Models;
using Caupo.Models;
using QRCoder;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;

namespace Caupo.Fiscal.Croatia
{
    public sealed class CroatiaReceiptPrinter
    {
        private readonly CroatiaFiscalSettings _settings;

        public CroatiaReceiptPrinter(
            CroatiaFiscalSettings settings)
        {
            _settings = settings;
        }

        public Task<bool> PrintAsync(
            FiscalRequest request,
            CroatiaBuiltInvoice builtInvoice,
            CroatiaFiscalizationResponse fiscalization,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if(string.IsNullOrWhiteSpace(
                _settings.PosPrinter))
            {
                return Task.FromResult(false);
            }

            try
            {
                using var document =
                    new PrintDocument();

                document.PrinterSettings.PrinterName =
                    _settings.PosPrinter;

                if(!document.PrinterSettings.IsValid)
                    return Task.FromResult(false);

                int paperWidth =
                    _settings.PaperWidthMm >= 80
                        ? 300
                        : 200;

                document.DefaultPageSettings.PaperSize =
                    new PaperSize(
                        "POS",
                        paperWidth,
                        2000);

                document.DefaultPageSettings.Margins =
                    new Margins(5, 5, 5, 5);

                document.PrintPage +=
                    (_, e) =>
                        DrawReceipt(
                            e,
                            request,
                            builtInvoice,
                            fiscalization);

                document.Print();

                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private void DrawReceipt(
            PrintPageEventArgs e,
            FiscalRequest request,
            CroatiaBuiltInvoice builtInvoice,
            CroatiaFiscalizationResponse fiscalization)
        {
            Graphics? g = e.Graphics;
            if(g == null)
                return;

            using var normal =
                new Font("Consolas", 9);

            using var bold =
                new Font(
                    "Consolas",
                    9,
                    FontStyle.Bold);

            using var small =
                new Font("Consolas", 7);

            int left = 0;
            int width =
                Math.Max(
                    100,
                    e.MarginBounds.Width - 5);

            int y =
                e.MarginBounds.Top;

            int lineHeight =
                (int)normal.GetHeight(g) + 2;

            DrawLogo(
                g,
                left,
                width,
                ref y);

            DrawCentered(
                g,
                _settings.CompanyName,
                bold,
                left,
                width,
                ref y,
                lineHeight);

            DrawCentered(
                g,
                _settings.Address,
                normal,
                left,
                width,
                ref y,
                lineHeight);

            DrawCentered(
                g,
                _settings.City,
                normal,
                left,
                width,
                ref y,
                lineHeight);

            DrawCentered(
                g,
                $"OIB: {_settings.Oib}",
                normal,
                left,
                width,
                ref y,
                lineHeight);

            DrawCentered(
                g,
                $"Broj računa: {builtInvoice.ReceiptNumberHr}",
                normal,
                left,
                width,
                ref y,
                lineHeight);

            DrawCentered(
                g,
                $"Datum: {builtInvoice.IssueDateTime:dd.MM.yyyy HH:mm}",
                normal,
                left,
                width,
                ref y,
                lineHeight);

            DrawCentered(
                g,
                $"Operater: {request.Cashier.Name}",
                normal,
                left,
                width,
                ref y,
                lineHeight);

            DrawCentered(
                g,
                $"Plaćanje: {PaymentName(request.PaymentType)}",
                normal,
                left,
                width,
                ref y,
                lineHeight);

            DrawLine(
                g,
                left,
                width,
                ref y);

            foreach(RacunStavka item in request.Items)
            {
                string name =
                    item.Name ?? string.Empty;

                g.DrawString(
                    name,
                    normal,
                    Brushes.Black,
                    left,
                    y);

                y += lineHeight;

                string detail =
                    $"{item.Quantity ?? 0m:0.##} x " +
                    $"{item.UnitPrice ?? 0m:0.00} = " +
                    $"{item.TotalAmount ?? 0m:0.00} EUR";

                g.DrawString(
                    detail,
                    normal,
                    Brushes.Black,
                    left,
                    y);

                y += lineHeight;

                if(!string.IsNullOrWhiteSpace(
                    item.Note))
                {
                    g.DrawString(
                        $"  {item.Note}",
                        small,
                        Brushes.Black,
                        left,
                        y);

                    y += lineHeight;
                }
            }

            DrawLine(
                g,
                left,
                width,
                ref y);

            foreach(var tax in builtInvoice.Taxes)
            {
                string prefix =
                    tax.IsConsumptionTax
                        ? "PNP"
                        : "PDV";

                string line =
                    $"{prefix} {tax.Rate:0.##}%  " +
                    $"osn. {tax.BaseAmount:0.00}  " +
                    $"porez {tax.TaxAmount:0.00}";

                g.DrawString(
                    line,
                    small,
                    Brushes.Black,
                    left,
                    y);

                y += lineHeight;
            }

            DrawLine(
                g,
                left,
                width,
                ref y);

            string total =
                $"UKUPNO: {request.TotalAmount:0.00} EUR";

            SizeF totalSize =
                g.MeasureString(
                    total,
                    bold);

            g.DrawString(
                total,
                bold,
                Brushes.Black,
                left + width - totalSize.Width,
                y);

            y += lineHeight * 2;

            g.DrawString(
                $"JIR: {fiscalization.Jir ?? "-"}",
                small,
                Brushes.Black,
                left,
                y);

            y += lineHeight;

            g.DrawString(
                $"ZKI: {fiscalization.Zki ?? "-"}",
                small,
                Brushes.Black,
                left,
                y);

            y += lineHeight * 2;

            if(fiscalization.Fiscalized &&
               !string.IsNullOrWhiteSpace(
                   fiscalization.Jir))
            {
                DrawQr(
                    g,
                    fiscalization.Jir,
                    builtInvoice,
                    request.TotalAmount,
                    left,
                    width,
                    ref y);
            }

            if(!fiscalization.Fiscalized)
            {
                g.DrawString(
                    "RAČUN NIJE FISKALIZOVAN",
                    bold,
                    Brushes.Black,
                    left,
                    y);

                y += lineHeight;

                g.DrawString(
                    "Potrebna je naknadna dostava računa.",
                    small,
                    Brushes.Black,
                    left,
                    y);
            }
        }

        private void DrawLogo(
            Graphics g,
            int left,
            int width,
            ref int y)
        {
            if(string.IsNullOrWhiteSpace(
                _settings.LogoPath) ||
               !File.Exists(
                   _settings.LogoPath))
            {
                return;
            }

            using Image logo =
                Image.FromFile(
                    _settings.LogoPath);

            int maxHeight = 60;

            float scale =
                Math.Min(
                    (float)maxHeight / logo.Height,
                    (float)width / logo.Width);

            int scaledWidth =
                Math.Max(
                    1,
                    (int)(logo.Width * scale));

            int scaledHeight =
                Math.Max(
                    1,
                    (int)(logo.Height * scale));

            int x =
                left +
                (width - scaledWidth) / 2;

            g.DrawImage(
                logo,
                new Rectangle(
                    x,
                    y,
                    scaledWidth,
                    scaledHeight));

            y +=
                scaledHeight + 5;
        }

        private static void DrawQr(
            Graphics g,
            string jir,
            CroatiaBuiltInvoice builtInvoice,
            decimal totalAmount,
            int left,
            int width,
            ref int y)
        {
            string datv =
                builtInvoice.IssueDateTime
                    .ToString(
                        "yyyyMMdd_HHmm",
                        CultureInfo.InvariantCulture);

            long amountInCents =
                (long)Math.Round(
                    totalAmount * 100m,
                    0,
                    MidpointRounding.AwayFromZero);

            string qrText =
                "https://porezna.gov.hr/rn" +
                $"?jir={Uri.EscapeDataString(jir)}" +
                $"&datv={datv}" +
                $"&izn={amountInCents}";

            using var generator =
                new QRCodeGenerator();

            using QRCodeData qrData =
                generator.CreateQrCode(
                    qrText,
                    QRCodeGenerator.ECCLevel.Q);

            using var qr =
                new QRCode(qrData);

            using Bitmap bitmap =
                qr.GetGraphic(10);

            int qrSize =
                Math.Min(
                    width,
                    150);

            int x =
                left +
                (width - qrSize) / 2;

            g.DrawImage(
                bitmap,
                new Rectangle(
                    x,
                    y,
                    qrSize,
                    qrSize));

            y += qrSize + 5;
        }

        private static void DrawCentered(
            Graphics g,
            string? text,
            Font font,
            int left,
            int width,
            ref int y,
            int lineHeight)
        {
            if(string.IsNullOrWhiteSpace(text))
                return;

            SizeF size =
                g.MeasureString(
                    text,
                    font);

            float x =
                left +
                (width - size.Width) / 2;

            g.DrawString(
                text,
                font,
                Brushes.Black,
                x,
                y);

            y += lineHeight;
        }

        private static void DrawLine(
            Graphics g,
            int left,
            int width,
            ref int y)
        {
            g.DrawLine(
                Pens.Black,
                left,
                y,
                left + width,
                y);

            y += 5;
        }

        private static string PaymentName(
            FiscalPaymentType paymentType)
        {
            return paymentType switch
            {
                FiscalPaymentType.Cash =>
                    "GOTOVINA",

                FiscalPaymentType.Card =>
                    "KARTICA",

                FiscalPaymentType.Check =>
                    "ČEK",

                FiscalPaymentType.WireTransfer =>
                    "TRANSAKCIJSKI RAČUN",

                FiscalPaymentType.Other =>
                    "OSTALO",

                _ =>
                    "NEPOZNATO"
            };
        }
    }
}
