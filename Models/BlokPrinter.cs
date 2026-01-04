using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Properties;
using Caupo.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;

using static Caupo.Data.DatabaseTables;
using static Caupo.ViewModels.KitchenDisplayViewModel;



namespace Caupo.Models
{
  
    public class BlokPrinter
    {
        private List<FiskalniRacun.Item> _stavke;
        private string? _konobar = Globals.ulogovaniKorisnik.Radnik;
        private int _paperWidthMm;
        private int _brojBloka;
        private string _firma = Settings.Default.Firma;
        private string _adresa = Settings.Default.Adresa;
        private string _grad = Settings.Default.Mjesto;
        private string logoPath = Settings.Default.LogoUrl;
        private string printerSank = Settings.Default.SankPrinter;
        private string printerKuhinja = Settings.Default.KuhinjaPrinter;
        private Image? _logo;
        private string _vrstaBloka;
        private string _sto;
        private string _imestola;


        public BlokPrinter(List<FiskalniRacun.Item> stavke, string vrstaBloka, string sto, string imestola)
        {
            _stavke = stavke;
            _paperWidthMm = Convert.ToInt32(Settings.Default.SirinaTrake);
            _vrstaBloka = vrstaBloka;
            _sto = sto;
            _imestola = imestola;
      
           
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                _logo = Image.FromFile(logoPath);
            }
        }


        public async Task InsertKuhinja()
        {
            using (var db = new AppDbContext())
            {
                var novaKuhinja = new TblKuhinja
                {
                    Sto = _sto,
                    Datum = DateTime.Now,
                    Radnik = Globals.ulogovaniKorisnik.Radnik,
                    NazivStola =  _imestola
                };
                db.Kuhinja.Add(novaKuhinja);

                foreach (var item in _stavke)
                {
                    var kuhinjaStavke = new TblKuhinjaStavke
                    {
                        Artikl = item.Name,
                        Sifra = item.Sifra,
                        Kolicina = Convert.ToDecimal(item.Quantity),
                        Cijena = Convert.ToDecimal(item.UnitPrice),
                        Zavrseno = "NE",
                        IdKuhinje = await BrojBlokaKuhinjaAsync() +1
                    };
                    db.KuhinjaStavke.Add(kuhinjaStavke);
                }
                await db.SaveChangesAsync();
            }

        }

        public async Task InsertSank(int brojbloka)
        {
            using (var db = new AppDbContext())
            {
                var noviSank = new TblBrojBlokaSank
                {
                    BrojBloka = brojbloka
                };
                db.BrojBloka.Add(noviSank);

             
                await db.SaveChangesAsync();
            }

        }

        public async Task<int> BrojBlokaKuhinjaAsync()
        {
            using (var db = new AppDbContext())
            {

                var lastIdKuhinje = await db.Kuhinja
                                                  .OrderByDescending(k => k.IdKuhinje)
                                                  .Select(k => k.IdKuhinje)
                                                  .FirstOrDefaultAsync();

                return lastIdKuhinje;
            }
        }

        public async Task<int> BrojBlokaSankAsync()
        {
            using (var db = new AppDbContext())
            {

                var lastIdSanka = await db.BrojBloka
                                                  .OrderByDescending(k => k.BrojBloka)
                                                  .Select(k => k.BrojBloka)
                                                  .FirstOrDefaultAsync();

                return lastIdSanka;
            }
        }

        public async Task Print()
        {
            await Task.Delay (1); // samo da ne blokira UI thread

            try
            {
                // odredi printer na osnovu vrste bloka
                string? printer = _vrstaBloka == "Kuhinja" ? printerKuhinja : printerSank;

                // provjeri da li je printer definisan
                if (string.IsNullOrWhiteSpace (printer))
                {
                    return; // prekini štampu
                }

                if (!PrinterSettings.InstalledPrinters.Cast<string> ()
                        .Any (p => p.Equals (printer, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                if (_vrstaBloka == "Kuhinja")
                {
                    _brojBloka = await BrojBlokaKuhinjaAsync () + 1;
                    await InsertKuhinja ();

                    var newOrder = new DisplayOrder ();
                    newOrder.Number = BrojBlokaKuhinjaAsync ().Result;
                    newOrder.Waiter = _konobar;
                    newOrder.TableName = _imestola;
                    newOrder.OrderTime = DateTime.Now;   // <--- ovo je ključno
                    newOrder.Elapsed = TimeSpan.Zero;

                    await System.Windows.Application.Current.Dispatcher.BeginInvoke (new Action (() =>
                    {
                        var orderItems = new ObservableCollection<OrderItem> (
                            _stavke.Select (s => new OrderItem { Name = s.Name, Quantity = s.Quantity })
                        );
                        newOrder.Items = orderItems;
                        App.GlobalKitchenVM.Orders.Add (newOrder);
                    }));

                }
                else
                {
                    _brojBloka = await BrojBlokaSankAsync () + 1;
                    await InsertSank (_brojBloka);
                }


                // kreiraj dokument
                PrintDocument printDoc = new PrintDocument
                {
                    PrinterSettings = { PrinterName = printer }
                };

                printDoc.PrintPage += OnPrintPage;

                // postavi papir i margine
                int widthHundredthsInch = (int)(_paperWidthMm / 25.4 * 100);
                PaperSize ps = new PaperSize ("Custom", widthHundredthsInch - 15, 0);
                printDoc.DefaultPageSettings.PaperSize = ps;
                printDoc.DefaultPageSettings.Margins = new Margins (5, 5, 5, 5);

                // štampaj
                printDoc.Print ();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show (
                    $"Greška prilikom štampe: {ex.Message}",
                    "Greška",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }


        private void OnPrintPage(object sender, PrintPageEventArgs e)
        {
        
            if (e.Graphics != null)
            {
                Graphics g = e.Graphics;

                Font font = new Font("Consolas", 9);
                Font bold = new Font("Consolas", 10, FontStyle.Bold);
                int y = 0;
                int lineHeight = (int)font.GetHeight(g) + 2;
                int pageWidth = e.MarginBounds.Width;

                // LOGO
                if (_logo != null)
                {
                    int maxLogoHeight = 60;

                    // Izvorišna veličina slike
                    int originalWidth = _logo.Width;
                    int originalHeight = _logo.Height;

                    // Skaliraj sliku proporcionalno na max visinu
                    float scale = (float)maxLogoHeight / originalHeight;
                    int scaledWidth = (int)(originalWidth * scale);
                    int scaledHeight = (int)(originalHeight * scale);

                    // Centriraj sliku po širini papira
                    int x = (pageWidth - scaledWidth) / 2;

                    System.Windows.Application.Current.Dispatcher.Invoke (() =>
                    {

                        try
                        {
                            g.DrawImage (_logo, new Rectangle (x, y, scaledWidth, scaledHeight));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine ($"Exception type: {ex.GetType ()}");
                            Debug.WriteLine ($"Message: {ex.Message}");
                            Debug.WriteLine ($"_logo: {_logo}");
                            Debug.WriteLine ($"Dimensions: {scaledWidth}x{scaledHeight}");
                            throw;
                        }

                    });
                    y += scaledHeight + 5;
                }

                // Zaglavlje
                // g.DrawString(_firma, bold, Brushes.Black, 0, y); y += lineHeight;
                // g.DrawString(_adresa, font, Brushes.Black, 0, y); y += lineHeight;
                // g.DrawString(_grad, font, Brushes.Black, 0, y); y += lineHeight;
                // g.DrawString($"Blok: {_brojBloka}", font, Brushes.Black, 0, y); y += lineHeight;
                // g.DrawString($"Datum: {DateTime.Now:dd.MM.yyyy HH:mm}", font, Brushes.Black, 0, y); y += lineHeight;
                // g.DrawString($"Konobar: {_konobar}", font, Brushes.Black, 0, y); y += lineHeight + 5;


                string[] headerLines = new[]
                        {
                        _firma,
                        _adresa,
                        _grad,
                        $"Blok: {_brojBloka}",
                        $"Datum: {DateTime.Now:dd.MM.yyyy HH:mm}",
                        $"Konobar: {_konobar}"
                    };

                foreach (var line in headerLines)
                {
                    // Koristi bold za naziv firme, ostalo obični font
                    var currentFont = line == _firma ? bold : font;

                    // Izračunavanje širine teksta i centriranje
                    var textSize = g.MeasureString(line, currentFont);
                    float x = (pageWidth - textSize.Width) / 2;

                    g.DrawString(line, currentFont, Brushes.Black, x, y);
                    y += lineHeight;
                }

                y += 5;

                g.DrawLine(Pens.Black, 0, y, pageWidth, y); y += 3;

                // Kolone
                int col1 = 0;
                int col2 = pageWidth - 120;
                int col3 = pageWidth - 60;
                int widthCol2 = 60;
                int widthCol3 = 60;

                g.DrawString("Artikal", bold, Brushes.Black, col1, y);

                // Centrirani naslovi kolona
                string col2Title = "Cij.";
                string col3Title = "Iznos";
                string col1Title = "Kol.";

                SizeF sizeCol1Title = g.MeasureString(col1Title, bold);
                SizeF sizeCol2Title = g.MeasureString(col2Title, bold);
                SizeF sizeCol3Title = g.MeasureString(col3Title, bold);

                g.DrawString(col1Title, bold, Brushes.Black, col1 + 60 - sizeCol1Title.Width, y + lineHeight);
                g.DrawString(col2Title, bold, Brushes.Black, col2 + widthCol2 / 2 - sizeCol2Title.Width / 2, y + lineHeight);
                g.DrawString(col3Title, bold, Brushes.Black, col3 + widthCol3 / 2 - sizeCol3Title.Width / 2, y + lineHeight);

                y += lineHeight * 2;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y); y += 3;

                // Stavke
                foreach (var item in _stavke)
                {
                    string naziv = item.Name ?? "";
                    string kolicina = item.Quantity?.ToString("0.##") ?? "";
                    string cijena = item.UnitPrice?.ToString("0.00") ?? "";
                    string iznos = item.TotalAmount?.ToString("0.00") ?? "";

                    g.DrawString(naziv, font, Brushes.Black, col1, y); y += lineHeight;

                    // Desno poravnanje vrijednosti
                    SizeF sizeKolicina = g.MeasureString(kolicina, font);
                    SizeF sizeCijena = g.MeasureString(cijena, font);
                    SizeF sizeIznos = g.MeasureString(iznos, font);

                    g.DrawString(kolicina, font, Brushes.Black, col1 + 60 - sizeKolicina.Width, y);
                    g.DrawString(cijena, font, Brushes.Black, col2 + widthCol2 / 2 - sizeCijena.Width / 2, y);
                    g.DrawString(iznos, font, Brushes.Black, col3 + widthCol3 / 2 - sizeIznos.Width / 2, y);

                    y += lineHeight;
                }

                // Footer
                y += 5;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                // TOTAL suma
                decimal total = _stavke.Sum(s => (s.UnitPrice ?? 0) * (s.Quantity ?? 0));

                string totalStr = $"Ukupno: {total:0.00} KM";
                SizeF sizeTotal = g.MeasureString(totalStr, bold);
                g.DrawString(totalStr, bold, Brushes.Black, pageWidth - sizeTotal.Width - 5, y);

            }
        }

    }

}
