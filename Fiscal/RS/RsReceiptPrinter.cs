using Caupo.Fiscal.RS.Models;
using QRCoder;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using Caupo.Fiscal.Common;

namespace Caupo.Fiscal.RS
{
    public sealed class RsReceiptPrinter
    {
        private readonly RsFiscalSettings _settings;

        public RsReceiptPrinter(
            RsFiscalSettings settings)
        {
            _settings = settings;
        }

        public bool Print(
            RsFiscalResponse response)
        {
            // Kada LPFR sam štampa račun, dodatni POS ispis nije potreban.
            if(!_settings.ExternalPrinter)
                return true;

            if(string.IsNullOrWhiteSpace(
                response.Journal))
            {
                throw new FiscalException(
                    "Journal nije pronađen u LPFR odgovoru.");
            }

            if(string.IsNullOrWhiteSpace(
                response.VerificationUrl))
            {
                throw new FiscalException(
                    "Verification URL nije pronađen u LPFR odgovoru.");
            }

            if(string.IsNullOrWhiteSpace(
                _settings.PosPrinter))
            {
                throw new FiscalException(
                    "POS printer nije podešen.");
            }

            using var qrGenerator =
                new QRCodeGenerator();

            using QRCodeData qrData =
                qrGenerator.CreateQrCode(
                    response.VerificationUrl,
                    QRCodeGenerator.ECCLevel.Q);

            using var qrCode =
                new QRCode(qrData);

            using Bitmap qrBitmap =
                qrCode.GetGraphic(20);

            using var document =
                new PrintDocument();

            document.PrinterSettings.PrinterName =
                _settings.PosPrinter;

            document.DefaultPageSettings.Margins =
                new Margins(
                    0,
                    0,
                    0,
                    0);

            document.PrintPage +=
                (_, e) =>
                {
                    using var font =
                        new Font(
                            "Consolas",
                            9);

                    string journal =
                        response.Journal;

                    int maxWidth =
                        e.MarginBounds.Width;

                    SizeF textSize =
                        e.Graphics.MeasureString(
                            journal,
                            font);

                    int qrWidth =
                        qrBitmap.Width;

                    int qrHeight =
                        qrBitmap.Height;

                    if(qrWidth > maxWidth)
                    {
                        float scale =
                            (float)maxWidth /
                            qrWidth;

                        qrWidth =
                            maxWidth;

                        qrHeight =
                            (int)(
                                qrHeight *
                                scale);
                    }

                    float textX =
                        Math.Max(
                            0,
                            (maxWidth -
                             textSize.Width) /
                            2f -
                            2f);

                    float textY =
                        e.MarginBounds.Top;

                    e.Graphics.DrawString(
                        journal,
                        font,
                        Brushes.Black,
                        new PointF(
                            textX,
                            textY));

                    int qrX =
                        Math.Max(
                            0,
                            (maxWidth -
                             qrWidth) /
                            2);

                    int qrY =
                        (int)(
                            textY +
                            textSize.Height +
                            10);

                    e.Graphics.InterpolationMode =
                        System.Drawing.Drawing2D
                            .InterpolationMode
                            .NearestNeighbor;

                    e.Graphics.DrawImage(
                        qrBitmap,
                        new Rectangle(
                            qrX,
                            qrY,
                            qrWidth,
                            qrHeight));
                };

            document.Print();

            Debug.WriteLine(
                "[RS/PRINT] Journal i QR poslani na POS printer.");

            return true;
        }
    }
}
