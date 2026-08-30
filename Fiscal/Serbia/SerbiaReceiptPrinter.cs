using Caupo.Fiscal.Serbia.Models;
using Caupo.Properties;
using QRCoder;
using System.Drawing;
using System.Drawing.Printing;

namespace Caupo.Fiscal.Serbia
{
    public sealed class SerbiaReceiptPrinter
    {
        private string _journalText =
            string.Empty;

        private Bitmap? _qrBitmap;

        public Task<bool> PrintAsync(
            SerbiaInvoiceResponse response,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if(string.IsNullOrWhiteSpace(
                response.Journal))
            {
                return Task.FromResult(false);
            }

            string printerName =
                Settings.Default.POSPrinter
                ?? string.Empty;

            if(string.IsNullOrWhiteSpace(
                printerName))
            {
                return Task.FromResult(false);
            }

            _journalText =
                response.Journal;

            _qrBitmap?.Dispose();
            _qrBitmap = null;

            if(!string.IsNullOrWhiteSpace(
                response.VerificationUrl))
            {
                using var generator =
                    new QRCodeGenerator();

                using QRCodeData data =
                    generator.CreateQrCode(
                        response.VerificationUrl,
                        QRCodeGenerator.ECCLevel.Q);

                using var qr =
                    new QRCode(data);

                _qrBitmap =
                    qr.GetGraphic(10);
            }

            using var document =
                new PrintDocument();

            document.PrinterSettings.PrinterName =
                printerName;

            if(!document.PrinterSettings.IsValid)
            {
                _qrBitmap?.Dispose();
                _qrBitmap = null;

                return Task.FromResult(false);
            }

            int widthMm =
                int.TryParse(
                    Settings.Default.SirinaTrake,
                    out int parsedWidth)
                    ? parsedWidth
                    : 80;

            int paperWidth =
                MmToHundredthsInch(
                    widthMm);

            document.DefaultPageSettings.PaperSize =
                new PaperSize(
                    "POS",
                    paperWidth,
                    4000);

            document.DefaultPageSettings.Margins =
                new Margins(
                    3,
                    3,
                    3,
                    3);

            document.PrintPage +=
                OnPrintPage;

            try
            {
                document.Print();

                return Task.FromResult(true);
            }
            finally
            {
                document.PrintPage -=
                    OnPrintPage;

                _qrBitmap?.Dispose();
                _qrBitmap = null;
            }
        }

        private void OnPrintPage(
            object? sender,
            PrintPageEventArgs e)
        {
            using var font =
                new Font(
                    FontFamily.GenericMonospace,
                    8.5f,
                    FontStyle.Regular);

            RectangleF bounds =
                new RectangleF(
                    e.MarginBounds.Left,
                    e.MarginBounds.Top,
                    e.MarginBounds.Width,
                    e.MarginBounds.Height);

            SizeF textSize =
                e.Graphics.MeasureString(
                    _journalText,
                    font,
                    (int)bounds.Width);

            e.Graphics.DrawString(
                _journalText,
                font,
                Brushes.Black,
                bounds);

            float y =
                bounds.Top +
                textSize.Height +
                8f;

            if(_qrBitmap != null)
            {
                int qrSize =
                    Math.Min(
                        160,
                        e.MarginBounds.Width - 10);

                float x =
                    e.MarginBounds.Left +
                    (e.MarginBounds.Width - qrSize) / 2f;

                e.Graphics.DrawImage(
                    _qrBitmap,
                    x,
                    y,
                    qrSize,
                    qrSize);
            }

            e.HasMorePages =
                false;
        }

        private static int MmToHundredthsInch(
            float mm)
        {
            return (int)Math.Round(
                mm / 25.4f * 100f);
        }
    }
}
