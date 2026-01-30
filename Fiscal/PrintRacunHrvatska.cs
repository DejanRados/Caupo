using Caupo.Properties;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using QRCoder;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Security.Cryptography;
using Tring.Fiscal.Driver;

namespace Caupo.Fiscal;
public static class PrintRacunHrvatska
{
    // ===== MODELI =====

    public class RacunModel
    {
        public string Firma { get; set; }
        public string Adresa { get; set; }
        public string Grad { get; set; }
        public string Oib { get; set; }

        public string BrojRacuna { get; set; }
        public DateTime DatumVrijeme { get; set; }
        public string Konobar { get; set; }
        public int NacinPlacanja { get; set; }
        public string NacinPlacanjaName => NacinPlacanja switch
        {
            0 => "GOTOVINA (Novčanice)",
            1 => "KARTICA",
            2 => "ČEK",
            3 => "Transakcijski račun",
            4 => "OSTALO",
            5 => "Reprezentacija",
            _ => "Nepoznat način plaćanja"
        };

        public ObservableCollection<FiskalniRacun.Item> Stavke { get; set; } = new ();
        public decimal? Ukupno { get; set; }

        public string JIR { get; set; }
        public string ZIK { get; set; }

        public Image Logo { get; set; }
        public string QR { get; set; }
        public Image QRimage()
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator ();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode (QR, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode (qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic (20);
            Image img = (Image)qrCodeImage;
            return img;

        }
        public decimal? PDV13 { get; internal set; }
        public decimal? PDV25 { get; internal set; }
        public decimal? PNP { get; internal set; }
        public decimal? PDV13Osnovica { get; internal set; }
        public decimal? PDV25Osnovica { get; internal set; }
        public string PNPPostotak { get; internal set; }
        public decimal? PNPOsnovica { get; internal set; }
    }

    public class Stavka
    {
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
        public string Note { get; set; }
    }

    // ===== PRINT API =====

    private static RacunModel _m;
  


    public static void Print(RacunModel model)
    {
        _m = model;

        PrintDocument doc = new PrintDocument ();
        doc.PrinterSettings.PrinterName = Settings.Default.POSPrinter;

        int sirina;
        if(!int.TryParse (Settings.Default.SirinaTrake, out sirina))
        {
            sirina = 80; 
        }

        int paperWidth = sirina == 80 ? 300 : 200;

        doc.DefaultPageSettings.PaperSize =
            new PaperSize ("POS", paperWidth, 2000);

        doc.DefaultPageSettings.Margins = new Margins (5, 5, 5, 5);
        doc.PrintPage += OnPrintPage;

        doc.Print ();
    }

    // ===== PRINT LOGIKA =====

    private static void OnPrintPage(object sender, PrintPageEventArgs e)
    {
        Graphics g = e.Graphics;
        if(g == null)
            return;

        Font font = new Font ("Consolas", 10);
        Font bold = new Font ("Consolas", 10, FontStyle.Bold);
        Font small = new Font ("Consolas",8);

        // Margin i širina papira
        int left = 0;//e.MarginBounds.Left;
        int width = e.MarginBounds.Width-5;

        int y = e.MarginBounds.Top;
        int lineHeight = (int)font.GetHeight (g) + 2;

        // ===== LOGO =====
        if(_m.Logo != null)
        {
            int maxLogoHeight = 60;
            float scale = (float)maxLogoHeight / _m.Logo.Height;
            int scaledWidth = (int)(_m.Logo.Width * scale);
            int scaledHeight = (int)(_m.Logo.Height * scale);
            int xLogo = left + (width - scaledWidth) / 2;

            g.DrawImage (_m.Logo, new Rectangle (xLogo, y, scaledWidth, scaledHeight));
            y += scaledHeight + 5;
        }

        // ===== HEADER (blokovi centrirani po liniji) =====
        string[] headerLines = new[]
        {
        _m.Firma,
        _m.Adresa,
        _m.Grad,
        $"OIB: {_m.Oib}",
        $"Broj računa: {_m.BrojRacuna}",
        $"Datum: {_m.DatumVrijeme:dd.MM.yyyy HH:mm}",
        $"Operater: {_m.Konobar}",
        $"Plaćanje: {_m.NacinPlacanjaName}"
    };

        foreach(var line in headerLines)
        {
            Font currentFont = line == _m.Firma ? bold : font;
            SizeF textSize = g.MeasureString (line, currentFont);
            float x = left + (width - textSize.Width) / 2;
            g.DrawString (line, currentFont, Brushes.Black, x, y);
            y += lineHeight;
        }

        DrawLine (g, left, width, ref y);

        // ===== KOLONE STAVKI =====
        int col1 = left;
        int col2 = left + width - (width/2);
        int col3 = left + width - 60;
        int widthCol2 = 60;
        int widthCol3 = 60;

        g.DrawString ("Artikal", bold, Brushes.Black, col1, y);

        string col1Title = "Kol.";
        string col2Title = "Cij.";
        string col3Title = "Iznos";

        SizeF sizeCol1 = g.MeasureString (col1Title, bold);
        SizeF sizeCol2 = g.MeasureString (col2Title, bold);
        SizeF sizeCol3 = g.MeasureString (col3Title, bold);

        g.DrawString (col1Title, bold, Brushes.Black, col1 + 60 - sizeCol1.Width, y + lineHeight);
        g.DrawString (col2Title, bold, Brushes.Black, col2 + widthCol2 / 2 - sizeCol2.Width / 2, y + lineHeight);
        g.DrawString (col3Title, bold, Brushes.Black, col3 + widthCol3 / 2 - sizeCol3.Width / 2, y + lineHeight);
      
        y += lineHeight * 2;
        g.DrawLine (Pens.Black, left, y, left + width, y);
        y += 3;

        // ===== STAVKE =====
        foreach(var item in _m.Stavke)
        {
            string naziv = item.Name ?? "";
            string kolicina = item.Quantity?.ToString ("0.##") +"X" ?? "";
            string cijena = item.UnitPrice?.ToString ("0.00") + " €" ?? "";
            string iznos = item.TotalAmount?.ToString ("0.00") + " €" ?? "";

            g.DrawString (naziv, font, Brushes.Black, col1, y);
            y += lineHeight;

            SizeF sizeKolicina = g.MeasureString (kolicina, font);
            SizeF sizeCijena = g.MeasureString (cijena, font);
            SizeF sizeIznos = g.MeasureString (iznos, font);

            g.DrawString (kolicina, font, Brushes.Black, col1 + 60 - sizeKolicina.Width, y);
            g.DrawString (cijena, font, Brushes.Black, col2 + widthCol2 / 2 - sizeCijena.Width / 2, y);
            g.DrawString (iznos, font, Brushes.Black, col3 + widthCol3 / 2 - sizeIznos.Width / 2, y);

            y += lineHeight;

        }
        y += lineHeight;
        DrawLine (g, left, width, ref y);
        // ===== PDV / PNP =====
        DrawTaxLine (g, "Porez", "%", "Osnovica", "iznos", bold, left, width, ref y);
        DrawLine (g, left, width, ref y);
        DrawTaxLine (g, "PDV", "13%", (_m.PDV13Osnovica ?? 0).ToString ("0.000"),( _m.PDV13 ?? 0).ToString ("0.000"), font, left, width, ref y);
        DrawTaxLine (g, "PDV", "25%", (_m.PDV25Osnovica ?? 0).ToString ("0.000"), (_m.PDV25 ?? 0).ToString ("0.000"), font, left, width, ref y);
        DrawTaxLine (g, "PNP", _m.PNPPostotak + "%", (_m.PNPOsnovica ?? 0).ToString ("0.000"), (_m.PNP ?? 0).ToString ("0.000"), font, left, width, ref y);

        DrawLine (g, left, width, ref y);

        string label = "UKUPNO:";
        string value = (_m.Ukupno ?? 0).ToString ("0.00") + " €";

        // Širina kolona može biti fleksibilna:
        float labelWidth = g.MeasureString (label, bold).Width;
        float valueWidth = g.MeasureString (value, bold).Width;

        // x pozicija da budu uz desnu marginu:
        float xValue = left + width - valueWidth;       // vrijednost stoji na krajnjoj desnoj poziciji
        float xLabel = xValue - labelWidth - 5;        // labela malo lijevo od vrijednosti, 5px razmak

        g.DrawString (label, bold, Brushes.Black, xLabel, y);
        g.DrawString (value, bold, Brushes.Black, xValue, y);

        y += lineHeight;
        DrawLine (g, left, width, ref y);

        // ===== QR kod (50% širine, centrirano) =====
        if(_m.QR != null)
        {
            int qrSize = width / 2;
            int xQR = left + (width - qrSize) / 2;
            g.DrawImage (_m.QRimage (), xQR, y, qrSize, qrSize);
            y += qrSize + 5;
        }
        DrawLine (g, left, width, ref y);
        // ===== JIR / ZIK =====
        string[] jirZik = { "JIR: " + _m.JIR, "ZIK: " + _m.ZIK };
        foreach(var line in jirZik)
        {
            SizeF size = g.MeasureString (line, small);
            float x = left + (width - size.Width) / 2;
            g.DrawString (line, small, Brushes.Black, x, y);
            y += lineHeight;
            DrawLine (g, left, width, ref y);
        }
     
        // ===== FOOTER =====

        string path = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "Images", "logodsoft.png");
        Image footerLogo = null;
        if(File.Exists (path))
        {
            footerLogo = Image.FromFile (path);
        }
        else
        {
            Debug.WriteLine ("################################# NEMA SLIKE #################################");
        }

        if(footerLogo != null)
        {
            int maxLogoHeight = 40; // manji logo za footer
            float scale = (float)maxLogoHeight / footerLogo.Height;
            int bmpWidth = (int)(footerLogo.Width * scale);
            int bmpHeight = (int)(footerLogo.Height * scale);

            int xLogo = left + (width - bmpWidth) / 2;
            g.DrawImage (footerLogo, new Rectangle (xLogo, y, bmpWidth, bmpHeight));
            y += bmpHeight + 5;

            DrawLine (g, left, width, ref y);
        }

    }

    // ===== HELPERS =====
    private static void DrawTaxLine(Graphics g, string naziv, string stopa, string osnovica, string iznos, Font font, int left, int width, ref int y)
    {
        // Podjela width na 4 kolone
        int colNazivW = width / 4;
        int colStopaW = width / 4;
        int colOsnovicaW = width / 4;
        int colIznosW = width / 4;

        int xNaziv = left;
        int xStopa = xNaziv + colNazivW;
        int xOsnovica = xStopa + colStopaW;
        int xIznos = xOsnovica + colOsnovicaW;

        // Centriranje teksta unutar kolona
        SizeF sizeNaziv = g.MeasureString (naziv, font);
        g.DrawString (naziv, font, Brushes.Black, xNaziv + (colNazivW - sizeNaziv.Width) / 2, y);

        SizeF sizeStopa = g.MeasureString (stopa, font);
        g.DrawString (stopa, font, Brushes.Black, xStopa + (colStopaW - sizeStopa.Width) / 2, y);

     
        SizeF sizeOsnovica = g.MeasureString (osnovica, font);
        g.DrawString (osnovica, font, Brushes.Black, xOsnovica + (colOsnovicaW - sizeOsnovica.Width) / 2, y);

        SizeF sizeIznos = g.MeasureString (iznos, font);
        g.DrawString (iznos, font, Brushes.Black, xIznos + (colIznosW - sizeIznos.Width) / 2, y);

        // Pomakni y ispod linije
        y += (int)font.GetHeight (g) + 5;
    }



    private static void DrawRight(Graphics g, string text, Font f, int x, int y, int w)
    {
        SizeF size = g.MeasureString (text, f);
        g.DrawString (text, f, Brushes.Black, x + w - size.Width, y);
    }

    private static void DrawLine(Graphics g, int left, int width, ref int y)
    {
        g.DrawLine (Pens.Black, left, y, left + width, y);
        y += 5;
    }


   
}
