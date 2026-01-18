using Caupo.Data;
using Caupo.Models;
using Caupo.Properties;
using Caupo.Services;
using Microsoft.EntityFrameworkCore;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Caupo.ViewModels
{

    public class KnjigaSankaViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

        private readonly KnjigaSankaService _service;

        private DatabaseTables.TblFirma? _firma;
        public DatabaseTables.TblFirma? Firma
        {
            get => _firma;
            set
            {
                _firma = value;
                OnPropertyChanged (nameof (Firma)); // ako implementiraš INotifyPropertyChanged
            }
        }
        public ObservableCollection<StavkaKnjigeSanka> Knjiga { get; }
            = new ObservableCollection<StavkaKnjigeSanka> ();

        private DateTime _odabraniDatum = DateTime.Today;
        public DateTime OdabraniDatum
        {
            get => _odabraniDatum;
            set
            {
                _odabraniDatum = value;
                OnPropertyChanged (nameof (OdabraniDatum));
                _ = LoadDataAsync (); // automatski refresh
            }
        }
        private string? _imagePathPrintButton;
        public string? ImagePathPrintButton
        {
            get { return _imagePathPrintButton; }
            set
            {
                _imagePathPrintButton = value;
                OnPropertyChanged (nameof (ImagePathPrintButton));
            }
        }

        private string? _imagePathFirstButton;
        public string? ImagePathFirstButton
        {
            get { return _imagePathFirstButton; }
            set
            {
                _imagePathFirstButton = value;
                OnPropertyChanged (nameof (ImagePathFirstButton));
            }
        }

        private string? _imagePathLastButton;
        public string? ImagePathLastButton
        {
            get { return _imagePathLastButton; }
            set
            {
                _imagePathLastButton = value;
                OnPropertyChanged (nameof (ImagePathLastButton));
            }
        }

        private decimal? _Total = 0;
        public decimal? Total
        {
            get { return _Total; }
            set
            {
                if(_Total != value)
                {
                    _Total = value;
                    OnPropertyChanged (nameof (Total));
                }
            }
        }


        private Brush? _fontColor;
        public Brush? FontColor
        {
            get { return _fontColor; }
            set
            {
                if(_fontColor != value)
                {
                    _fontColor = value;
                    OnPropertyChanged (nameof (FontColor));
                }
            }
        }

        private Brush? _backColor;
        public Brush? BackColor
        {
            get { return _backColor; }
            set
            {
                if(_backColor != value)
                {
                    _backColor = value;
                    OnPropertyChanged (nameof (BackColor));
                }
            }
        }

        public KnjigaSankaViewModel(KnjigaSankaService service)
        {
            _service = service;
            _ = Start ();
        }

        public async Task Start()
        {
            await SetImage ();
            await LoadDataAsync ();

            await LoadFirmaAsync ();
        }
        private async Task LoadDataAsync()
        {
            Debug.WriteLine ("Trigerovan  private async Task LoadDataAsync() u  KnjigaSankaViewModel");
            Knjiga.Clear ();
            var data = await _service.GetKnjigaZaDanAsync (OdabraniDatum);
            foreach(var item in data)
            {
                Debug.WriteLine (item.Naziv + " -- " + item.IsPromet);
                Total += item.Promet;
                Knjiga.Add (item);
            }

        }

        public async Task LoadFirmaAsync()
        {
            try
            {
                using var db = new AppDbContext ();
                Firma = await db.Firma.FirstOrDefaultAsync ();
                Debug.WriteLine ("Firma učitana: " + (Firma?.NazivFirme ?? "null"));
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex);
            }
        }


        public async Task SetImage()
        {
            await Task.Delay (1);
            string tema = Settings.Default.Tema;

            if(tema == "Tamna")
            {
                Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema);

                ImagePathPrintButton = "pack://application:,,,/Images/Dark/printer.svg";

                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
                BackColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));


            }
            else
            {

                ImagePathPrintButton = "pack://application:,,,/Images/Light/printer.svg";
                Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema);
                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
                BackColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));

            }
        }

        public void PrintReport(ObservableCollection<StavkaKnjigeSanka> knjiga, string nazivFirme, string adresa, string grad, string jib, string pdv, DateTime odabraniDatum, decimal? total)
        {
            FlowDocument doc = new FlowDocument
            {
                PageWidth = 700,   // A4 širina u device independent units (1/96 inča)
                PageHeight = 1122, // A4 visina
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily ("Segoe UI"),
                FontSize = 10,
                PagePadding = new Thickness (50) // margine
            };


            // === ZAGLAVLJE ===
            Table headerTable = new Table ();
            headerTable.Columns.Add (new TableColumn { Width = new GridLength (200) }); // lijeva kolona
            headerTable.Columns.Add (new TableColumn { Width = new GridLength (200) }); // srednja kolona
            headerTable.Columns.Add (new TableColumn { Width = new GridLength (150) }); // desna kolona

            TableRowGroup trg = new TableRowGroup ();
            headerTable.RowGroups.Add (trg);
            TableRow row = new TableRow ();
            trg.Rows.Add (row);

            // --- Lijevo: podaci o firmi ---
            Paragraph firmaPar = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 12
            };
            firmaPar.Inlines.Add (nazivFirme + Environment.NewLine);
            firmaPar.Inlines.Add (adresa + Environment.NewLine);
            firmaPar.Inlines.Add (grad + Environment.NewLine);
            firmaPar.Inlines.Add ("JIB: " + jib + Environment.NewLine);
            firmaPar.Inlines.Add ("PDV: " + pdv);

            row.Cells.Add (new TableCell (firmaPar) { BorderThickness = new Thickness (0) });

            // --- Centar: naslov ---
            Paragraph naslovPar = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 20,
                FontWeight = FontWeights.Bold
            };
            naslovPar.Inlines.Add ("DNEVNI LIST ŠANKA");

            Paragraph datumPar = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeights.Medium
            };
            datumPar.Inlines.Add ($"Za dan: {odabraniDatum:dd.MM.yyyy}");

            // Napravi TableCell, podesi svojstva i dodaj blokove
            var centerCell = new TableCell
            {
                BorderThickness = new Thickness (0),
                TextAlignment = TextAlignment.Center
            };
            centerCell.Blocks.Add (naslovPar);
            centerCell.Blocks.Add (datumPar);

            // Dodaj u red
            row.Cells.Add (centerCell);


            // --- Desno: obrazac ---
            Paragraph obrazacPar = new Paragraph
            {
                TextAlignment = TextAlignment.Right,
                FontSize = 10
            };
            obrazacPar.Inlines.Add ("Obrazac DLŠ");
            row.Cells.Add (new TableCell (obrazacPar) { BorderThickness = new Thickness (0) });

            // Dodaj tabelu u dokument
            doc.Blocks.Add (headerTable);

            doc.Blocks.Add (new Paragraph (new Run (" ")) { FontSize = 6 }); // mali razmak ispod



            // === TABELA ===
            Table table = new Table ();
            table.CellSpacing = 0;
            doc.Blocks.Add (table);

            // Definicija kolona
            table.Columns.Add (new TableColumn { Width = new GridLength (25) });
            table.Columns.Add (new TableColumn { Width = new GridLength (150) });
            table.Columns.Add (new TableColumn { Width = new GridLength (30) });
            table.Columns.Add (new TableColumn { Width = new GridLength (56) });
            table.Columns.Add (new TableColumn { Width = new GridLength (56) });
            table.Columns.Add (new TableColumn { Width = new GridLength (56) });
            table.Columns.Add (new TableColumn { Width = new GridLength (56) });
            table.Columns.Add (new TableColumn { Width = new GridLength (56) });
            table.Columns.Add (new TableColumn { Width = new GridLength (56) });
            table.Columns.Add (new TableColumn { Width = new GridLength (56) });

            // Header red
            TableRowGroup headerGroup = new TableRowGroup ();
            table.RowGroups.Add (headerGroup);
            TableRow headerRow = new TableRow ();
            headerGroup.Rows.Add (headerRow);

            string[] kolone = { "#", "Naziv robe", "JM", "Prenesene zalihe iz prethodnog dana", "Nabavke u toku dana", "Ukupno zaduženje", "Utrošak u toku dana", "Cijena", "Iznos", "Ostatak robe-Prenos za naredni dan" };
            foreach(var col in kolone)
            {
                TableCell cell = new TableCell (new Paragraph (new Run (col)))
                {
                    FontWeight = FontWeights.Medium,
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness (4),
                    FontSize = 10,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness (0.5)
                };
                headerRow.Cells.Add (cell);
            }

            // Podaci
            TableRowGroup bodyGroup = new TableRowGroup ();
            table.RowGroups.Add (bodyGroup);

            foreach(var item in knjiga)
            {
                TableRow row2 = new TableRow ();
                bodyGroup.Rows.Add (row2);

                row2.Cells.Add (CreateCell (item.RedniBroj.GetValueOrDefault ().ToString (), TextAlignment.Center));
                row2.Cells.Add (CreateCell (item.Naziv, TextAlignment.Left));
                row2.Cells.Add (CreateCell (item.JedinicaMjere, TextAlignment.Center));
                row2.Cells.Add (CreateCell (item.OstatakOdJuce.ToString ("F2"), TextAlignment.Right));
                row2.Cells.Add (CreateCell (item.NabavljenoDanas.ToString ("F2"), TextAlignment.Right));
                row2.Cells.Add (CreateCell (item.NaStanju.ToString ("F2"), TextAlignment.Right));
                row2.Cells.Add (CreateCell (item.UtrosenoDanas.ToString ("F2"), TextAlignment.Right));
                row2.Cells.Add (CreateCell (item.Cijena.ToString ("F2"), TextAlignment.Right));
                row2.Cells.Add (CreateCell (item.Promet.ToString ("F2"), TextAlignment.Right));
                row2.Cells.Add (CreateCell (item.OstatakZaSutra.ToString ("F2"), TextAlignment.Right));
            }

            // === UKUPNO ===
            Paragraph totalPar = new Paragraph
            {
                TextAlignment = TextAlignment.Right,
                FontSize = 12,
                FontWeight = FontWeights.Medium
            };
            totalPar.Inlines.Add ("UKUPNO: " + total.GetValueOrDefault ().ToString ("F2"));
            doc.Blocks.Add (totalPar);

            // === POTPIS ===
            Paragraph footer = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 12,
                Margin = new Thickness (0, 5, 0, 0)
            };
            footer.Inlines.Add ("POTPIS: __________________________");

            doc.Blocks.Add (footer);

            // === ŠTAMPA ===
            PrintDialog printDlg = new PrintDialog ();
            printDlg.PrintQueue = LocalPrintServer.GetDefaultPrintQueue ();
            if(printDlg.ShowDialog () == true)
            {
                IDocumentPaginatorSource idpSource = doc;
                printDlg.PrintDocument (idpSource.DocumentPaginator, "Dnevni list šanka");
            }
        }

        private TableCell CreateCell(string text, TextAlignment alignment)
        {
            return new TableCell (new Paragraph (new Run (text)))
            {
                TextAlignment = alignment,
                Padding = new Thickness (3),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness (0.5)
            };
        }
    }

}
