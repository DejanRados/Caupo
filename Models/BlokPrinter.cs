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
        private const string Kuhinja = "Kuhinja";
        private const string Sank = "Sank";

        private readonly List<RacunStavka> _stavke;
        private readonly string? _konobar = Globals.ulogovaniKorisnik.Radnik;
        private readonly string _firma = Settings.Default.Firma;
        private readonly string _adresa = Settings.Default.Adresa;
        private readonly string _grad = Settings.Default.Mjesto;
        private readonly string _logoPath = Settings.Default.LogoUrl;
        private readonly string _printerSank = Settings.Default.SankPrinter;
        private readonly string _printerKuhinja = Settings.Default.KuhinjaPrinter;
        private readonly string _vrstaBloka;
        private readonly string _sto;
        private readonly string _imeStola;

        private Image? _logo;
        private int _brojBloka;
        private DateTime _datumBloka;

        public BlokPrinter(List<RacunStavka> stavke, string vrstaBloka, string sto, string imeStola)
        {
            _stavke = stavke ?? throw new ArgumentNullException(nameof(stavke));
            _vrstaBloka = vrstaBloka;
            _sto = sto;
            _imeStola = imeStola;

            if (_vrstaBloka != Kuhinja && _vrstaBloka != Sank)
                throw new ArgumentException($"Nepoznata vrsta bloka: {_vrstaBloka}", nameof(vrstaBloka));

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

        private async Task<int> InsertKuhinjaAsync()
        {
            await using var db = new AppDbContext();

            var novaKuhinja = new TblKuhinja
            {
                Sto = _sto,
                Datum = _datumBloka,
                Radnik = _konobar,
                NazivStola = _imeStola
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

        private async Task InsertSankAsync(int brojBloka)
        {
            await using var db = new AppDbContext();

            db.BrojBloka.Add(new TblBrojBlokaSank
            {
                BrojBloka = brojBloka
            });

            await db.SaveChangesAsync();
        }

        private async Task<int> GetLastSankBlockNumberAsync()
        {
            await using var db = new AppDbContext();

            return await db.BrojBloka
                .AsNoTracking()
                .MaxAsync(x => (int?)x.BrojBloka) ?? 0;
        }

        public async Task Print()
        {
            if (_stavke.Count == 0)
            {
                Debug.WriteLine("[BLOK] Nema stavki za obradu.");
                return;
            }

            _datumBloka = DateTime.Now;

            try
            {
                string? printer = _vrstaBloka == Kuhinja ? _printerKuhinja : _printerSank;

                if (_vrstaBloka == Kuhinja)
                    await ProcessKitchenAsync();

                if (string.IsNullOrWhiteSpace(printer))
                {
                    Debug.WriteLine(_vrstaBloka == Kuhinja
                        ? "[BLOK] Kuhinjski printer nije definisan. Fizička štampa preskočena."
                        : "[BLOK] Printer za šank nije definisan. Štampa preskočena.");

                    return;
                }

                if (!PrinterExists(printer))
                {
                    Debug.WriteLine($"[BLOK] Printer '{printer}' nije pronađen među instaliranim printerima.");
                    return;
                }

                if (_vrstaBloka == Sank)
                {
                    _brojBloka = await GetLastSankBlockNumberAsync() + 1;
                    await InsertSankAsync(_brojBloka);
                }

                int brojKopija = GetBrojKopijaBloka();

                if (brojKopija == 0)
                {
                    Debug.WriteLine($"[BLOK] Broj kopija za {_vrstaBloka} je 0. Fizička štampa preskočena.");
                    return;
                }

                PrintDocument(printer, brojKopija);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BLOK] GREŠKA: {ex}");
                throw;
            }
            finally
            {
                _logo?.Dispose();
                _logo = null;
            }
        }

        private async Task ProcessKitchenAsync()
        {
            _brojBloka = await InsertKuhinjaAsync();

            var newOrder = new DisplayOrder
            {
                Number = _brojBloka,
                Waiter = _konobar,
                TableName = _imeStola,
                OrderTime = _datumBloka,
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
        }

        private static bool PrinterExists(string printer)
        {
            return PrinterSettings.InstalledPrinters
                .Cast<string>()
                .Any(p => p.Equals(printer, StringComparison.OrdinalIgnoreCase));
        }

        private void PrintDocument(string printer, int brojKopija)
        {
            using var printDoc = new PrintDocument();

            printDoc.PrinterSettings.PrinterName = printer;
            printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
            printDoc.PrintPage += OnPrintPage;

            DebugPrinterSettings(printDoc);

            Debug.WriteLine($"[BLOK] Printer='{printer}'");
            Debug.WriteLine($"[BLOK] Vrsta='{_vrstaBloka}'");
            Debug.WriteLine($"[BLOK] Broj bloka={_brojBloka}");
            Debug.WriteLine($"[BLOK] Broj kopija={brojKopija}");

            for (int i = 0; i < brojKopija; i++)
                printDoc.Print();
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

            DrawLogo(g, left, width, ref y);

            DrawCentered(g, _firma, bold, left, width, ref y, lineHeight);
            DrawCentered(g, _adresa, font, left, width, ref y, lineHeight);
            DrawCentered(g, _grad, font, left, width, ref y, lineHeight);
            DrawCentered(g, $"Blok: {_brojBloka}", font, left, width, ref y, lineHeight);
            DrawCentered(g, $"Datum: {_datumBloka:dd.MM.yyyy HH:mm}", font, left, width, ref y, lineHeight);
            DrawCentered(g, $"Konobar: {_konobar}", font, left, width, ref y, lineHeight);

            y += 5;

            DrawLine(g, left, width, ref y);

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

            foreach (var item in _stavke)
            {
                string naziv = item.Name ?? string.Empty;
                string kolicina = item.Quantity?.ToString("0.##") ?? string.Empty;
                string cijena = item.UnitPrice?.ToString("0.00") ?? string.Empty;
                string iznos = ((item.UnitPrice ?? 0m) * (item.Quantity ?? 0m)).ToString("0.00");

                DrawWrappedText(g, naziv, font, left, width, ref y, lineHeight);

                DrawRightAligned(g, kolicina, font, colKolicinaRight, y);
                DrawCenteredAt(g, cijena, font, colCijenaCenter, y);
                DrawRightAligned(g, iznos, font, colIznosRight, y);

                y += lineHeight;

                if (!string.IsNullOrWhiteSpace(item.Note))
                    DrawNote(g, item.Note, font, left, width, ref y, lineHeight);

                using var separatorPen = new Pen(Color.Gray);
                g.DrawLine(separatorPen, left, y, right, y);
                y += lineHeight;
            }

            y += 5;

            DrawLine(g, left, width, ref y);

            decimal total = _stavke.Sum(s => (s.UnitPrice ?? 0m) * (s.Quantity ?? 0m));
            DrawRightAligned(g, $"Ukupno: {total:0.00} KM", bold, right, y);

            e.HasMorePages = false;
        }

        private void DrawLogo(Graphics g, int left, int width, ref int y)
        {
            if (_logo == null)
                return;

            const int maxLogoHeight = 60;

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

        private static void DrawNote(Graphics g, string note, Font font, int left, int width, ref int y, int lineHeight)
        {
            string text = $"( {note} )";

            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.LineLimit,
                Trimming = StringTrimming.Word
            };

            SizeF noteSize = g.MeasureString(text, font, width);
            var noteRect = new RectangleF(left, y, width, Math.Max(noteSize.Height + 4, lineHeight));

            g.DrawString(text, font, Brushes.Black, noteRect, sf);

            y += (int)Math.Ceiling(noteSize.Height) + 3;
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