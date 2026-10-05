using Caupo.Data;
using Caupo.Models;
using Caupo.Properties;
using Caupo.Services;
using Caupo.Views;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Caupo.ViewModels
{
    public class KnjigaSankaViewModel : INotifyPropertyChanged
    {
        #region POLJA

        private readonly KnjigaSankaService _service;

        private DatabaseTables.TblFirma? _firma;
        private DateTime _odabraniDatum = DateTime.Today;
        private decimal _total;
        private bool _initialized;

        #endregion


        #region PROPERTYJI

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<StavkaKnjigeSanka> Knjiga { get; } = new ObservableCollection<StavkaKnjigeSanka>();


        public DatabaseTables.TblFirma? Firma
        {
            get => _firma;
            private set
            {
                if (_firma == value)
                    return;

                _firma = value;
                OnPropertyChanged(nameof(Firma));
            }
        }


        public DateTime OdabraniDatum
        {
            get => _odabraniDatum;
            set
            {
                DateTime noviDatum = value.Date;

                if (_odabraniDatum.Date == noviDatum)
                    return;

                _odabraniDatum = noviDatum;

                OnPropertyChanged(nameof(OdabraniDatum));

                _ = LoadDataAsync();
            }
        }


        public decimal Total
        {
            get => _total;
            private set
            {
                if (_total == value)
                    return;

                _total = value;

                OnPropertyChanged(nameof(Total));
            }
        }

        #endregion


        #region KOMANDE

        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }
        public ICommand PrintCommand { get; }

        #endregion


        #region KONSTRUKTOR

        public KnjigaSankaViewModel(KnjigaSankaService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));

            PreviousDayCommand = new RelayCommand(PreviousDay);
            NextDayCommand = new RelayCommand(NextDay);
            PrintCommand = new RelayCommand(PrintReport);

            _ = InitializeAsync();
        }

        #endregion


        #region INICIJALIZACIJA

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _initialized = true;

            LoadFirma();

            _odabraniDatum = DateTime.Today;
            OnPropertyChanged(nameof(OdabraniDatum));

            try
            {
                await _service.SinhronizujDoDanasAsync();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                _initialized = false;

                Knjiga.Clear();
                Total = 0;

                Debug.WriteLine("[KNJIGA ŠANKA] InitializeAsync: " + ex);

                ShowMessage(    "Greška - Knjiga šanka",   $"Knjiga šanka nije mogla biti obračunata.\n\nRazlog:\n{ex.Message}");
            }
        }

        private static void ShowMessage(string title, string message)
        {
            MyMessageBox myMessageBox = new MyMessageBox();
            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            myMessageBox.MessageTitle.Text = title;
            myMessageBox.MessageText.Text = message;
            myMessageBox.ShowDialog();
        }

        private void LoadFirma()
        {
            try
            {
                Firma = new DatabaseTables.TblFirma
                {
                    NazivFirme = Settings.Default.Firma,
                    Adresa = Settings.Default.Adresa,
                    Grad = Settings.Default.Mjesto,
                    JIB = Settings.Default.JIB,
                    PDV = Settings.Default.PDV
                };

                Debug.WriteLine("[KNJIGA ŠANKA] Firma učitana: " + (Firma.NazivFirme ?? "null"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KNJIGA ŠANKA] LoadFirma: " + ex);
            }
        }

        #endregion


        #region UČITAVANJE KNJIGE

        private async Task LoadDataAsync()
        {
            DateTime datum = OdabraniDatum.Date;

            Debug.WriteLine($"[KNJIGA LOAD] START datum={datum:dd.MM.yyyy}, property={OdabraniDatum:dd.MM.yyyy}");

            try
            {
                var data = await _service.GetKnjigaZaDanAsync(datum);

                Debug.WriteLine($"[KNJIGA LOAD] SERVICE GOTOV datum={datum:dd.MM.yyyy}, property={OdabraniDatum:dd.MM.yyyy}");

                Knjiga.Clear();

                foreach (var item in data)
                    Knjiga.Add(item);

                Total = data.Sum(x => x.Promet);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[KNJIGA LOAD ERROR] {ex}");
            }
        }

        #endregion


        #region NAVIGACIJA DATUMOM

        private void PreviousDay()
        {
            OdabraniDatum = OdabraniDatum.AddDays(-1);
        }


        private void NextDay()
        {
            OdabraniDatum = OdabraniDatum.AddDays(1);
        }

        #endregion


        #region ŠTAMPA

        private void PrintReport()
        {
            if (Firma == null)
            {
                Debug.WriteLine("[KNJIGA ŠANKA] Štampa nije pokrenuta jer podaci firme nisu učitani.");
                return;
            }

            try
            {
                FlowDocument doc = new FlowDocument
                {
                    PageWidth = 793.7,
                    PageHeight = 1122.5,
                    ColumnWidth = double.PositiveInfinity,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 10,
                    PagePadding = new Thickness(48)
                };

                // ============================================================
                // ZAGLAVLJE DOKUMENTA
                // ============================================================

                Table headerTable = new Table { CellSpacing = 0 };

                headerTable.Columns.Add(new TableColumn { Width = new GridLength(230) });
                headerTable.Columns.Add(new TableColumn { Width = new GridLength(285) });
                headerTable.Columns.Add(new TableColumn { Width = new GridLength(180) });

                TableRowGroup headerRowGroup = new TableRowGroup();
                headerTable.RowGroups.Add(headerRowGroup);

                TableRow headerRow = new TableRow();
                headerRowGroup.Rows.Add(headerRow);

                Paragraph firmaPar = new Paragraph
                {
                    TextAlignment = TextAlignment.Left,
                    FontSize = 12,
                    Margin = new Thickness(0)
                };

                firmaPar.Inlines.Add((Firma.NazivFirme ?? string.Empty) + Environment.NewLine);
                firmaPar.Inlines.Add((Firma.Adresa ?? string.Empty) + Environment.NewLine);
                firmaPar.Inlines.Add((Firma.Grad ?? string.Empty) + Environment.NewLine);
                firmaPar.Inlines.Add("JIB: " + (Firma.JIB ?? string.Empty) + Environment.NewLine);
                firmaPar.Inlines.Add("PDV: " + (Firma.PDV ?? string.Empty));

                headerRow.Cells.Add(new TableCell(firmaPar)
                {
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0)
                });

                Paragraph naslovPar = new Paragraph
                {
                    TextAlignment = TextAlignment.Center,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5)
                };

                naslovPar.Inlines.Add("DNEVNI LIST ŠANKA");

                Paragraph datumPar = new Paragraph
                {
                    TextAlignment = TextAlignment.Center,
                    FontSize = 12,
                    FontWeight = FontWeights.Medium,
                    Margin = new Thickness(0)
                };

                datumPar.Inlines.Add($"Za dan: {OdabraniDatum:dd.MM.yyyy}");

                TableCell centerCell = new TableCell
                {
                    BorderThickness = new Thickness(0),
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(0)
                };

                centerCell.Blocks.Add(naslovPar);
                centerCell.Blocks.Add(datumPar);
                headerRow.Cells.Add(centerCell);

                Paragraph obrazacPar = new Paragraph
                {
                    TextAlignment = TextAlignment.Right,
                    FontSize = 10,
                    Margin = new Thickness(0)
                };

                obrazacPar.Inlines.Add("Obrazac DLŠ");

                headerRow.Cells.Add(new TableCell(obrazacPar)
                {
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(0)
                });

                doc.Blocks.Add(headerTable);

                doc.Blocks.Add(new Paragraph(new Run(" "))
                {
                    FontSize = 6,
                    Margin = new Thickness(0)
                });

                // ============================================================
                // TABELA
                // ============================================================

                Table table = new Table { CellSpacing = 0 };
                doc.Blocks.Add(table);

                // Ukupno 695
                table.Columns.Add(new TableColumn { Width = new GridLength(22) });
                table.Columns.Add(new TableColumn { Width = new GridLength(165) });
                table.Columns.Add(new TableColumn { Width = new GridLength(30) });
                table.Columns.Add(new TableColumn { Width = new GridLength(84) });
                table.Columns.Add(new TableColumn { Width = new GridLength(71) });
                table.Columns.Add(new TableColumn { Width = new GridLength(65) });
                table.Columns.Add(new TableColumn { Width = new GridLength(72) });
                table.Columns.Add(new TableColumn { Width = new GridLength(55) });
                table.Columns.Add(new TableColumn { Width = new GridLength(55) });
                table.Columns.Add(new TableColumn { Width = new GridLength(76) });

                // ============================================================
                // HEADER TABELE
                // ============================================================

                string[] kolone =
                {
            "#",
            "Naziv robe",
            "JM",
            "Prenesene zalihe iz prethodnog dana",
            "Nabavke u toku dana",
            "Ukupno zaduženje",
            "Utrošak u toku dana",
            "Cijena",
            "Iznos",
            "Ostatak robe-Prenos za naredni dan"
        };

                TableRowGroup tableHeaderGroup = new TableRowGroup();
                table.RowGroups.Add(tableHeaderGroup);

                TableRow tableHeaderRow = new TableRow();
                tableHeaderGroup.Rows.Add(tableHeaderRow);

                foreach (string col in kolone)
                {
                    TableCell cell = new TableCell(new Paragraph(new Run(col)) { Margin = new Thickness(0) })
                    {
                        FontWeight = FontWeights.Medium,
                        TextAlignment = TextAlignment.Center,
                        Padding = new Thickness(3, 5, 3, 5),
                        FontSize = 9,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0.5)
                    };

                    tableHeaderRow.Cells.Add(cell);
                }

                // ============================================================
                // PODACI
                // ============================================================

                TableRowGroup bodyGroup = new TableRowGroup();
                table.RowGroups.Add(bodyGroup);

                foreach (var item in Knjiga)
                {
                    TableRow row = new TableRow();
                    bodyGroup.Rows.Add(row);

                    TableCell redniBrojCell = CreateCell(item.RedniBroj.GetValueOrDefault().ToString(), TextAlignment.Center);

                    if (item.IsPromet)
                    {
                        redniBrojCell.FontWeight = FontWeights.Bold;
                        redniBrojCell.Background = new SolidColorBrush(Color.FromRgb(235, 235, 235));
                    }

                    row.Cells.Add(redniBrojCell);
                    //row.Cells.Add(CreateCell(item.RedniBroj.GetValueOrDefault().ToString(), TextAlignment.Center));
                    row.Cells.Add(CreateCell(item.Naziv, TextAlignment.Left));
                    row.Cells.Add(CreateCell(item.JedinicaMjere, TextAlignment.Center));
                    row.Cells.Add(CreateCell(FormatQuantity(item.OstatakOdJuce), TextAlignment.Right));
                    row.Cells.Add(CreateCell(FormatQuantity(item.NabavljenoDanas), TextAlignment.Right));
                    row.Cells.Add(CreateCell(FormatQuantity(item.NaStanju), TextAlignment.Right));
                    row.Cells.Add(CreateCell(FormatQuantity(item.UtrosenoDanas), TextAlignment.Right));
                    row.Cells.Add(CreateCell(item.Cijena.ToString("F2"), TextAlignment.Right));
                    row.Cells.Add(CreateCell(item.Promet.ToString("F2"), TextAlignment.Right));
                    row.Cells.Add(CreateCell(FormatQuantity(item.OstatakZaSutra), TextAlignment.Right));
                }

                // ============================================================
                // UKUPNO
                // ============================================================

                Paragraph totalPar = new Paragraph
                {
                    TextAlignment = TextAlignment.Right,
                    FontSize = 12,
                    FontWeight = FontWeights.Medium,
                    Margin = new Thickness(0, 8, 0, 0)
                };

                totalPar.Inlines.Add("UKUPNO: " + Total.ToString("F2"));
                doc.Blocks.Add(totalPar);

                // ============================================================
                // POTPIS
                // ============================================================

                Paragraph footer = new Paragraph
                {
                    TextAlignment = TextAlignment.Left,
                    FontSize = 12,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                footer.Inlines.Add("POTPIS: __________________________");
                doc.Blocks.Add(footer);

                // ============================================================
                // PRINT
                // ============================================================

                PrintDialog printDialog = new PrintDialog
                {
                    PrintQueue = LocalPrintServer.GetDefaultPrintQueue()
                };

                if (printDialog.ShowDialog() == true)
                {
                    IDocumentPaginatorSource paginatorSource = doc;

                    DocumentPaginator paginator = new KnjigaSankaPaginator(
                        paginatorSource.DocumentPaginator,
                        kolone,
                        new double[] { 22, 165, 30, 84, 71, 65, 72, 55, 55, 76 },
                        new Size(793.7, 1122.5),
                        48);

                    string documentName = $"KnjigaSanka_{OdabraniDatum:dd_MM_yyyy}";
                    printDialog.PrintDocument(paginator, documentName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KNJIGA ŠANKA] PrintReport: " + ex);
            }
        }


        private static string FormatQuantity(decimal value)
        {
            return value.ToString("0.00#");
        }


        private static TableCell CreateCell(string? text, TextAlignment alignment)
        {
            return new TableCell(
                new Paragraph(
                    new Run(text ?? string.Empty)))
            {
                TextAlignment = alignment,
                Padding = new Thickness(3),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5)
            };
        }

        #endregion


        #region INOTIFYPROPERTYCHANGED

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class KnjigaSankaPaginator : DocumentPaginator
    {
        private readonly DocumentPaginator _source;
        private readonly string[] _headers;
        private readonly double[] _widths;
        private readonly Size _pageSize;
        private readonly double _margin;

        private readonly double _topMargin = 20;
        private readonly double _headerHeight = 52;

        private double ReservedHeight => _topMargin + _headerHeight;

        public KnjigaSankaPaginator(DocumentPaginator source, string[] headers, double[] widths, Size pageSize, double margin)
        {
            _source = source;
            _headers = headers;
            _widths = widths;
            _pageSize = pageSize;
            _margin = margin;

            _source.PageSize = new Size(pageSize.Width, pageSize.Height - ReservedHeight);
        }

        public override DocumentPage GetPage(int pageNumber)
        {
            DocumentPage sourcePage = _source.GetPage(pageNumber);

            if (pageNumber == 0)
            {
                return new DocumentPage(
                    sourcePage.Visual,
                    _pageSize,
                    sourcePage.BleedBox,
                    sourcePage.ContentBox);
            }

            ContainerVisual pageVisual = new ContainerVisual();

            // Header druge i svake naredne stranice
            DrawingVisual headerVisual = CreateHeaderVisual();
            pageVisual.Children.Add(headerVisual);

            // Sadržaj počinje odmah ispod headera
            ContainerVisual contentVisual = new ContainerVisual
            {
                Transform = new TranslateTransform(0, ReservedHeight - _margin)
            };

            contentVisual.Children.Add(sourcePage.Visual);
            pageVisual.Children.Add(contentVisual);

            return new DocumentPage(
                pageVisual,
                _pageSize,
                new Rect(_pageSize),
                new Rect(_pageSize));
        }

        private DrawingVisual CreateHeaderVisual()
        {
            DrawingVisual visual = new DrawingVisual();

            using (DrawingContext dc = visual.RenderOpen())
            {
                double x = _margin;
                double y = _topMargin;

                for (int i = 0; i < _headers.Length; i++)
                {
                    double width = _widths[i];

                    Rect rect = new Rect(
                        x,
                        y,
                        width,
                        _headerHeight);

                    dc.DrawRectangle(
                        Brushes.White,
                        new Pen(Brushes.Gray, 0.5),
                        rect);

                    FormattedText text = new FormattedText(
                        _headers[i],
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(
                            new FontFamily("Segoe UI"),
                            FontStyles.Normal,
                            FontWeights.Medium,
                            FontStretches.Normal),
                        9,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                    text.MaxTextWidth = Math.Max(1, width - 6);
                    text.MaxTextHeight = _headerHeight - 6;
                    text.TextAlignment = TextAlignment.Center;
                    text.Trimming = TextTrimming.None;

                    double textY = y + Math.Max(
                        3,
                        (_headerHeight - text.Height) / 2);

                    dc.DrawText(
                        text,
                        new Point(x + 3, textY));

                    x += width;
                }
            }

            return visual;
        }

        public override bool IsPageCountValid => _source.IsPageCountValid;

        public override int PageCount => _source.PageCount;

        public override Size PageSize
        {
            get => _pageSize;
            set
            {
                _source.PageSize = new Size(
                    value.Width,
                    value.Height - ReservedHeight);
            }
        }

        public override IDocumentPaginatorSource Source => _source.Source;
    }
}