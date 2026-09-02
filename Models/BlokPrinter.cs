using Caupo.Data;
using Caupo.Properties;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using static Caupo.Data.DatabaseTables;
using static Caupo.ViewModels.KitchenDisplayViewModel;

namespace Caupo.Models
{
    public class BlokPrinter
    {
        private readonly List<RacunStavka> _stavke;
        private readonly string? _konobar = Globals.ulogovaniKorisnik.Radnik;
        private int _brojBloka;
        private readonly string _firma = Settings.Default.Firma;
        private readonly string _adresa = Settings.Default.Adresa;
        private readonly string _grad = Settings.Default.Mjesto;
        private readonly string _logoPath = Settings.Default.LogoUrl;
        private readonly string _printerSank = Settings.Default.SankPrinter;
        private readonly string _printerKuhinja = Settings.Default.KuhinjaPrinter;
        private Image? _logo;
        private readonly string _vrstaBloka;
        private readonly string _sto;
        private readonly string _imestola;

        public BlokPrinter(List<RacunStavka> stavke, string vrstaBloka, string sto, string imestola)
        {
            _stavke = stavke;
            _vrstaBloka = vrstaBloka;
            _sto = sto;
            _imestola = imestola;

            LoadLogo();
        }

        private void LoadLogo()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_logoPath) || !File.Exists(_logoPath))
                    return;

                using var source = Image.FromFile(_logoPath);
                _logo = new Bitmap(source);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BLOK] Logo nije moguće učitati: {ex.Message}");
                _logo = null;
            }
        }

        public async Task<int> InsertKuhinja()
        {
            await using var db = new AppDbContext();

            var novaKuhinja = new TblKuhinja
            {
                Sto = _sto,
                Datum = DateTime.Now,
                Radnik = Globals.ulogovaniKorisnik.Radnik,
                NazivStola = _imestola
            };

            db.Kuhinja.Add(novaKuhinja);
            await db.SaveChangesAsync();

            foreach (var item in _stavke)
            {
                var kuhinjaStavka = new TblKuhinjaStavke
                {
                    Artikl = item.Name,
                    Sifra = item.Sifra,
                    Note = item.Note,
                    Kolicina = item.Quantity ?? 0m,
                    Cijena = item.UnitPrice ?? 0m,
                    Zavrseno = "NE",
                    IdKuhinje = novaKuhinja.IdKuhinje
                };

                db.KuhinjaStavke.Add(kuhinjaStavka);
            }

            await db.SaveChangesAsync();

            return novaKuhinja.IdKuhinje;
        }

        public async Task InsertSank(int brojBloka)
        {
            await using var db = new AppDbContext();

            var noviSank = new TblBrojBlokaSank
            {
                BrojBloka = brojBloka
            };

            db.BrojBloka.Add(noviSank);
            await db.SaveChangesAsync();
        }

        public async Task<int> BrojBlokaSankAsync()
        {
            await using var db = new AppDbContext();

            return await db.BrojBloka
                .OrderByDescending(k => k.BrojBloka)
                .Select(k => k.BrojBloka)
                .FirstOrDefaultAsync();
        }

        public async Task Print()
        {
            try
            {
                string? printer = _vrstaBloka == "Kuhinja" ? _printerKuhinja : _printerSank;

                // =====================================================
                // KUHINJA
                // Baza i Kitchen Display rade NEZAVISNO od printera.
                // =====================================================
                if (_vrstaBloka == "Kuhinja")
                {
                    _brojBloka = await InsertKuhinja();

                    var newOrder = new DisplayOrder
                    {
                        Number = _brojBloka,
                        Waiter = _konobar,
                        TableName = _imestola,
                        OrderTime = DateTime.Now,
                        Elapsed = TimeSpan.Zero,
                        Items = new ObservableCollection<OrderItem>(
                            _stavke.Select(s => new OrderItem
                            {
                                Name = s.Name,
                                Note = s.Note,
                                Quantity = s.Quantity
                            }))
                    };

                    await System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        App.GlobalKitchenVM.Orders.Add(newOrder);
                    }));

                    Debug.WriteLine($"[BLOK] Kuhinja spremljena i poslana na display. Broj bloka={_brojBloka}");

                    if (string.IsNullOrWhiteSpace(printer))
                    {
                        Debug.WriteLine("[BLOK] Kuhinjski printer nije definisan. Fizička štampa preskočena.");
                        return;
                    }
                }
                else
                {
                    // =====================================================
                    // ŠANK
                    // Ako nema printera, nema potrebe kreirati fizički blok.
                    // =====================================================
                    if (string.IsNullOrWhiteSpace(printer))
                    {
                        Debug.WriteLine("[BLOK] Printer za šank nije definisan. Štampa preskočena.");
                        return;
                    }
                }

                // =====================================================
                // PROVJERA WINDOWS PRINTERA
                // =====================================================
                bool printerPostoji = PrinterSettings.InstalledPrinters.Cast<string>().Any(p => p.Equals(printer, StringComparison.OrdinalIgnoreCase));

                if (!printerPostoji)
                {
                    Debug.WriteLine($"[BLOK] Printer '{printer}' nije pronađen među instaliranim printerima.");
                    return;
                }

                // =====================================================
                // BROJ BLOKA ZA ŠANK
                // =====================================================
                if (_vrstaBloka != "Kuhinja")
                {
                    _brojBloka = await BrojBlokaSankAsync() + 1;
                    await InsertSank(_brojBloka);
                }

                // =====================================================
                // PRINT DOCUMENT
                // Širinu papira određuje Windows printer driver.
                // =====================================================
                using var printDoc = new PrintDocument();

                printDoc.PrinterSettings.PrinterName = printer;
                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                printDoc.PrintPage += OnPrintPage;

                DebugPrinterSettings(printDoc);

                int brojKopija = GetBrojKopijaBloka();

                Debug.WriteLine($"[BLOK] Printer='{printer}'");
                Debug.WriteLine($"[BLOK] Vrsta='{_vrstaBloka}'");
                Debug.WriteLine($"[BLOK] Broj bloka={_brojBloka}");
                Debug.WriteLine($"[BLOK] Broj kopija={brojKopija}");

                for (int i = 0; i < brojKopija; i++)
                    printDoc.Print();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BLOK] GREŠKA: {ex}");

                System.Windows.MessageBox.Show(
                    $"Greška prilikom štampe: {ex.Message}",
                    "Greška",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                _logo?.Dispose();
                _logo = null;
            }
        }

        private static void DebugPrinterSettings(PrintDocument printDoc)
        {
            try
            {
                var settings = printDoc.DefaultPageSettings;

                Debug.WriteLine("=======================================================");
                Debug.WriteLine($"[BLOK PRINT] Printer: {printDoc.PrinterSettings.PrinterName}");
                Debug.WriteLine($"[BLOK PRINT] Default Paper: {settings.PaperSize.PaperName}");
                Debug.WriteLine($"[BLOK PRINT] Default Paper Width: {settings.PaperSize.Width}");
                Debug.WriteLine($"[BLOK PRINT] Default Paper Height: {settings.PaperSize.Height}");
                Debug.WriteLine($"[BLOK PRINT] PrintableArea: {settings.PrintableArea}");
                Debug.WriteLine($"[BLOK PRINT] HardMarginX: {settings.HardMarginX}");
                Debug.WriteLine($"[BLOK PRINT] HardMarginY: {settings.HardMarginY}");
                Debug.WriteLine("=======================================================");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BLOK PRINT] Debug printer settings greška: {ex.Message}");
            }
        }

        private int GetBrojKopijaBloka()
        {
            if (!int.TryParse(Settings.Default.BlokKopija, out int kopije))
                return 1;

            return Math.Clamp(kopije, 0, 5);
        }

        private void OnPrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics? g = e.Graphics;

            if (g == null)
                return;

            using var font = new Font("Consolas", 9f, FontStyle.Regular);
            using var bold = new Font("Consolas", 10f, FontStyle.Bold);

            // =====================================================
            // GEOMETRIJA IZ PRINTER DRIVERA
            // Isto kao na fiskalnom računu.
            // =====================================================
            g.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);

            float hardLeft = e.PageSettings.PrintableArea.Left;
            float hardRight = e.PageBounds.Width - e.PageSettings.PrintableArea.Right;

            int sideMargin = (int)Math.Ceiling(Math.Max(hardLeft, hardRight)) + 4;
            int left = sideMargin;
            int width = Math.Max(100, e.PageBounds.Width - sideMargin * 2);
            int right = left + width;
            int y = (int)Math.Ceiling(e.PageSettings.PrintableArea.Top) + 4;

            int lineHeight = (int)Math.Ceiling(font.GetHeight(g)) + 2;

            Debug.WriteLine("=======================================================");
            Debug.WriteLine($"[BLOK PRINT PAGE] PageBounds: {e.PageBounds}");
            Debug.WriteLine($"[BLOK PRINT PAGE] MarginBounds: {e.MarginBounds}");
            Debug.WriteLine($"[BLOK PRINT PAGE] PrintableArea: {e.PageSettings.PrintableArea}");
            Debug.WriteLine($"[BLOK PRINT PAGE] HardMarginX: {e.PageSettings.HardMarginX}");
            Debug.WriteLine($"[BLOK PRINT PAGE] HardMarginY: {e.PageSettings.HardMarginY}");
            Debug.WriteLine($"[BLOK PRINT LAYOUT] Left={left}");
            Debug.WriteLine($"[BLOK PRINT LAYOUT] Width={width}");
            Debug.WriteLine($"[BLOK PRINT LAYOUT] Right={right}");
            Debug.WriteLine($"[BLOK PRINT LAYOUT] Y={y}");
            Debug.WriteLine("=======================================================");

            // =====================================================
            // LOGO
            // =====================================================
            if (_logo != null)
            {
                int maxLogoHeight = 60;
                float scale = Math.Min(1f, (float)maxLogoHeight / _logo.Height);

                int scaledWidth = Math.Max(1, (int)(_logo.Width * scale));
                int scaledHeight = Math.Max(1, (int)(_logo.Height * scale));

                if (scaledWidth > width)
                {
                    scale = (float)width / _logo.Width;
                    scaledWidth = Math.Max(1, (int)(_logo.Width * scale));
                    scaledHeight = Math.Max(1, (int)(_logo.Height * scale));
                }

                int logoX = left + (width - scaledWidth) / 2;

                try
                {
                    g.DrawImage(_logo, new Rectangle(logoX, y, scaledWidth, scaledHeight));
                    y += scaledHeight + 5;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[BLOK PRINT] Logo greška: {ex.Message}");
                }
            }

            // =====================================================
            // HEADER
            // =====================================================
            DrawCentered(g, _firma, bold, left, width, ref y, lineHeight);
            DrawCentered(g, _adresa, font, left, width, ref y, lineHeight);
            DrawCentered(g, _grad, font, left, width, ref y, lineHeight);
            DrawCentered(g, $"Blok: {_brojBloka}", font, left, width, ref y, lineHeight);
            DrawCentered(g, $"Datum: {DateTime.Now:dd.MM.yyyy HH:mm}", font, left, width, ref y, lineHeight);
            DrawCentered(g, $"Konobar: {_konobar}", font, left, width, ref y, lineHeight);

            y += 5;

            DrawLine(g, left, width, ref y);

            // =====================================================
            // KOLONE
            // =====================================================
            int colArtikl = left;
            int colKolicinaRight = left + (int)(width * 0.28f);
            int colCijenaCenter = left + (int)(width * 0.68f);
            int colIznosRight = right;

            g.DrawString("Artikal", bold, Brushes.Black, colArtikl, y);
            y += lineHeight;

            DrawRightAligned(g, "Kol.", bold, colKolicinaRight, y);
            DrawCenteredAt(g, "Cij.", bold, colCijenaCenter, y);
            DrawRightAligned(g, "Iznos", bold, colIznosRight, y);

            y += lineHeight;

            DrawLine(g, left, width, ref y);

            // =====================================================
            // STAVKE
            // =====================================================
            foreach (var item in _stavke)
            {
                string naziv = item.Name ?? string.Empty;
                string kolicina = item.Quantity?.ToString("0.##") ?? string.Empty;
                string cijena = item.UnitPrice?.ToString("0.00") ?? string.Empty;
                string iznos = item.TotalAmount?.ToString("0.00") ?? string.Empty;

                DrawWrappedText(g, naziv, font, left, width, ref y, lineHeight);

                DrawRightAligned(g, kolicina, font, colKolicinaRight, y);
                DrawCenteredAt(g, cijena, font, colCijenaCenter, y);
                DrawRightAligned(g, iznos, font, colIznosRight, y);

                y += lineHeight;

                if (!string.IsNullOrWhiteSpace(item.Note))
                {
                    string note = $"( {item.Note} )";

                    using var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        FormatFlags = StringFormatFlags.LineLimit,
                        Trimming = StringTrimming.Word
                    };

                    SizeF noteSize = g.MeasureString(note, font, width);
                    var noteRect = new RectangleF(left, y, width, Math.Max(noteSize.Height + 4, lineHeight));

                    g.DrawString(note, font, Brushes.Black, noteRect, sf);

                    y += (int)Math.Ceiling(noteSize.Height) + 3;
                }

                using var separatorPen = new Pen(Color.Gray);
                g.DrawLine(separatorPen, left, y, right, y);
                y += lineHeight;
            }

            // =====================================================
            // TOTAL
            // =====================================================
            y += 5;

            DrawLine(g, left, width, ref y);

            decimal total = _stavke.Sum(s => (s.UnitPrice ?? 0m) * (s.Quantity ?? 0m));
            string totalStr = $"Ukupno: {total:0.00} KM";

            DrawRightAligned(g, totalStr, bold, right, y);

            y += lineHeight + 5;

            e.HasMorePages = false;
        }

        private static void DrawCentered(Graphics g, string? text, Font font, int left, int width, ref int y, int lineHeight)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            SizeF size = g.MeasureString(text, font);
            float x = left + (width - size.Width) / 2f;

            g.DrawString(text, font, Brushes.Black, x, y);
            y += lineHeight;
        }

        private static void DrawCenteredAt(Graphics g, string text, Font font, float centerX, float y)
        {
            SizeF size = g.MeasureString(text, font);
            g.DrawString(text, font, Brushes.Black, centerX - size.Width / 2f, y);
        }

        private static void DrawRightAligned(Graphics g, string text, Font font, float right, float y)
        {
            SizeF size = g.MeasureString(text, font);
            g.DrawString(text, font, Brushes.Black, right - size.Width, y);
        }

        private static void DrawWrappedText(Graphics g, string text, Font font, int left, int width, ref int y, int lineHeight)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            SizeF size = g.MeasureString(text, font, width);

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.LineLimit,
                Trimming = StringTrimming.Word
            };

            var rect = new RectangleF(left, y, width, Math.Max(size.Height + 2, lineHeight));

            g.DrawString(text, font, Brushes.Black, rect, sf);

            y += Math.Max(lineHeight, (int)Math.Ceiling(size.Height));
        }

        private static void DrawLine(Graphics g, int left, int width, ref int y)
        {
            g.DrawLine(Pens.Black, left, y, left + width, y);
            y += 4;
        }
    }
}