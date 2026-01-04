using Caupo.Data;
using Caupo.Models;
using Caupo.Properties;
using Caupo.Services;
using Microsoft.EntityFrameworkCore;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Caupo.ViewModels
{

    public class KnjigaKuhinjeViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly KnjigaKuhinjeService _service;

        private DatabaseTables.TblFirma? _firma;
        public DatabaseTables.TblFirma? Firma
        {
            get => _firma;
            set
            {
                _firma = value;
                OnPropertyChanged(nameof(Firma)); // ako implementiraš INotifyPropertyChanged
            }
        }
        public ObservableCollection<StavkaKnjigeKuhinje> Knjiga { get; }
            = new ObservableCollection<StavkaKnjigeKuhinje>();

        public ObservableCollection<Jelo> ListaHrane { get; }
        = new ObservableCollection<Jelo>();

        private DateTime _odabraniDatum = DateTime.Today;
        public DateTime OdabraniDatum
        {
            get => _odabraniDatum;
            set
            {
                _odabraniDatum = value;
                OnPropertyChanged(nameof(OdabraniDatum));
                _ = LoadDataAsync(); // automatski refresh
            }
        }
        private string? _imagePathPrintButton;
        public string? ImagePathPrintButton
        {
            get { return _imagePathPrintButton; }
            set
            {
                _imagePathPrintButton = value;
                OnPropertyChanged(nameof(ImagePathPrintButton));
            }
        }

        private string? _imagePathFirstButton;
        public string? ImagePathFirstButton
        {
            get { return _imagePathFirstButton; }
            set
            {
                _imagePathFirstButton = value;
                OnPropertyChanged(nameof(ImagePathFirstButton));
            }
        }

        private string? _imagePathLastButton;
        public string? ImagePathLastButton
        {
            get { return _imagePathLastButton; }
            set
            {
                _imagePathLastButton = value;
                OnPropertyChanged(nameof(ImagePathLastButton));
            }
        }

        private decimal? _Total = 0;
        public decimal? Total
        {
            get { return _Total; }
            set
            {
                if (_Total != value)
                {
                    _Total = value;
                    OnPropertyChanged(nameof(Total));
                }
            }
        }


        private Brush? _fontColor;
        public Brush? FontColor
        {
            get { return _fontColor; }
            set
            {
                if (_fontColor != value)
                {
                    _fontColor = value;
                    OnPropertyChanged(nameof(FontColor));
                }
            }
        }

        private Brush? _backColor;
        public Brush? BackColor
        {
            get { return _backColor; }
            set
            {
                if (_backColor != value)
                {
                    _backColor = value;
                    OnPropertyChanged(nameof(BackColor));
                }
            }
        }

      

   

        public KnjigaKuhinjeViewModel(KnjigaKuhinjeService service)
        {
            _service = service;
            _=Start();
          
        }

        public async Task Start()
        {
            await SetImage();
            await LoadDataAsync();
            await LoadFirmaAsync();
            await GetJelaZaOdabraniDatumAsync();
        }
        private async Task LoadDataAsync()
        {
            Debug.WriteLine("Trigerovan  private async Task LoadDataAsync() u  KnjigaSankaViewModel");
            Knjiga.Clear();
            var data = await _service.GetKnjigaZaDanAsync(OdabraniDatum);
            foreach (var item in data)
            {
                Debug.WriteLine(item.Namirnica + " =" + item.IsPromet );
               
                Knjiga.Add(item);
            }
               
        }

        public async Task GetJelaZaOdabraniDatumAsync()
        {
            ListaHrane.Clear();
            Total = 0;
            try
            {
                using var db = new AppDbContext();
                var jela = await (from r in db.Racuni
                                  join s in db.RacunStavka
                                      on r.BrojRacuna equals s.BrojRacuna
                                  where r.Datum.Date == OdabraniDatum.Date
                                        && s.VrstaArtikla == 1
                                  group s by s.Artikl into g
                                  select new
                                  {
                                      NazivArtikla = g.Key,
                                      UkupnaKolicina = g.Sum(x => x.Kolicina),
                                      UkupanIznos = g.Sum(x => x.Cijena != null ? x.Cijena * x.Kolicina : 0),
                                      ProsjecnaCijena = g.Average(x => x.Cijena != null ? x.Cijena : 0)
                                  })
                               .ToListAsync();
                int i = 1;
                foreach (var item in jela)
                {
                    Debug.WriteLine(item.NazivArtikla);
                    Jelo jelo = new Jelo();
                    jelo.RedniBroj = i;
                    jelo.Naziv = item.NazivArtikla;
                    jelo.Kolicina = item.UkupnaKolicina ?? 0m;
                    jelo.Cijena = item.ProsjecnaCijena ?? 0m; 
                    jelo.Vrijednost = item.UkupanIznos ?? 0m;
                    Total += jelo.Vrijednost;
                    ListaHrane.Add(jelo);
                    i++;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
           

          
        }


        public async Task LoadFirmaAsync()
        {
            try
            {
                using var db = new AppDbContext();
                Firma = await db.Firma.FirstOrDefaultAsync();
                Debug.WriteLine("Firma učitana: " + (Firma?.NazivFirme ?? "null"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }


        public async Task SetImage()
        {
            await Task.Delay(1);
            string tema = Settings.Default.Tema;
            Debug.WriteLine("Aktivna tema koju vidi viewmodel je : " + tema);
            if (tema == "Tamna")
            {
                ImagePathPrintButton = "pack://application:,,,/Images/Dark/printer.svg";
                FontColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212));   Application.Current.Resources["GlobalFontColor"] =    FontColor;
                BackColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));


            }
            else
            {
                ImagePathPrintButton = "pack://application:,,,/Images/Light/printer.svg";
                FontColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));  Application.Current.Resources["GlobalFontColor"] =    FontColor;
                BackColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212));

            }
        }

        public void PrintReport(
         ObservableCollection<StavkaKnjigeKuhinje> knjiga, string nazivFirme, string adresa, string grad, string jib, string pdv, DateTime odabraniDatum, decimal? total)
        {
            // FlowDocument
            FlowDocument doc = new FlowDocument();
            doc.PagePadding = new Thickness(40);
            doc.PageHeight = 842; // A4 landscape
            doc.PageWidth = 1191;
            doc.ColumnWidth = double.PositiveInfinity;
            doc.FontFamily = new FontFamily("Segoe UI");
            doc.FontSize = 11;

            // LANDSCAPE
            doc.PageHeight = 768;
            doc.PageWidth = 1120;

            // === ZAGLAVLJE ===
            Table headerTable = new Table();
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(350) }); // lijeva kolona
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(350) }); // srednja kolona
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(300) }); // desna kolona

            TableRowGroup trg2 = new TableRowGroup();
            headerTable.RowGroups.Add(trg2);
            TableRow row2 = new TableRow();
            trg2.Rows.Add(row2);

            // --- Lijevo: podaci o firmi ---
            Paragraph firmaPar = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 12
            };
            firmaPar.Inlines.Add(nazivFirme + Environment.NewLine);
            firmaPar.Inlines.Add(adresa + Environment.NewLine);
            firmaPar.Inlines.Add(grad + Environment.NewLine);
            firmaPar.Inlines.Add("JIB: " + jib + Environment.NewLine);
            firmaPar.Inlines.Add("PDV: " + pdv);

            row2.Cells.Add(new TableCell(firmaPar) { BorderThickness = new Thickness(0) });

            // --- Centar: naslov ---
            Paragraph naslovPar = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 20,
                FontWeight = FontWeights.Bold
            };
            naslovPar.Inlines.Add("DNEVNI LIST KUHINJE");

            Paragraph datumPar = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeights.Medium
            };
            datumPar.Inlines.Add($"Za dan: {odabraniDatum:dd.MM.yyyy}");

            // Napravi TableCell, podesi svojstva i dodaj blokove
            var centerCell = new TableCell
            {
                BorderThickness = new Thickness(0),
                TextAlignment = TextAlignment.Center
            };
            centerCell.Blocks.Add(naslovPar);
            centerCell.Blocks.Add(datumPar);

            // Dodaj u red
            row2.Cells.Add(centerCell);


            // --- Desno: obrazac ---
            Paragraph obrazacPar = new Paragraph
            {
                TextAlignment = TextAlignment.Right,
                FontSize = 10
            };
            obrazacPar.Inlines.Add("Obrazac DLK");
            row2.Cells.Add(new TableCell(obrazacPar) { BorderThickness = new Thickness(0) });

            // Dodaj tabelu u dokument
            doc.Blocks.Add(headerTable);

            doc.Blocks.Add(new Paragraph(new Run(" ")) { FontSize = 6 }); // mali razmak ispod



            // ====== Glavna tabela za horizontalni raspored ======
            Table mainTable = new Table();
            mainTable.CellSpacing = 5; // razmak između lijeve i desne tabele

            // Definišemo dvije kolone: lijeva i desna
            mainTable.Columns.Add(new TableColumn() { Width = new GridLength(650) }); // lijeva tabela
            mainTable.Columns.Add(new TableColumn() { Width = new GridLength(400) }); // desna tabela

            TableRowGroup trg = new TableRowGroup();
            mainTable.RowGroups.Add(trg);

            TableRow row = new TableRow();
            trg.Rows.Add(row);

            // ====== Lijeva tabela: tblKnjiga (tvoje postojeće punjenje ostaje) ======
            Table tblKnjiga = new Table();
            tblKnjiga.CellSpacing = 0;
            tblKnjiga.BorderBrush = Brushes.Black;
            tblKnjiga.BorderThickness = new Thickness(0.5);

            // kolone
            tblKnjiga.Columns.Add(new TableColumn() { Width = new GridLength(25) });   // #
            tblKnjiga.Columns.Add(new TableColumn() { Width = new GridLength(155) }); // Namirnica
            tblKnjiga.Columns.Add(new TableColumn() { Width = new GridLength(25) });  // JM
            tblKnjiga.Columns.Add(new TableColumn() { Width = new GridLength(60) });  // Prenesene
            tblKnjiga.Columns.Add(new TableColumn() { Width = new GridLength(60) });  // Nabavljeno kolicina
            tblKnjiga.Columns.Add(new TableColumn() { Width = new GridLength(80) }); // Nabavljeno dobavljač
            tblKnjiga.Columns.Add(new TableColumn() { Width = new GridLength(60) });  // Nabavljeno dokument
            tblKnjiga.Columns.Add(new TableColumn() { Width = new GridLength(60) });  // Ukupno
            tblKnjiga.Columns.Add(new TableColumn() { Width = new GridLength(60) });  // Utroseno
            tblKnjiga.Columns.Add(new TableColumn() { Width = new GridLength(60) });  // Ostatak

            TableRowGroup knjigaGroup = new TableRowGroup();


            tblKnjiga.RowGroups.Add(knjigaGroup);

            TableRow groupHeader = new TableRow();
            knjigaGroup.Rows.Insert(0, groupHeader); // dodajemo iznad postojećeg headera

            TableCell groupCell = new TableCell(new Paragraph(new Run("Nabavke u toku dana")))
            {
                TextAlignment = TextAlignment.Center,
                Background = Brushes.Gray,
                Foreground = Brushes.White,
                ColumnSpan = 3 // pokriva Količina, Dobavljač i Dokument
            };
            groupHeader.Cells.Add(new TableCell(new Paragraph(new Run(""))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });
            groupHeader.Cells.Add(new TableCell(new Paragraph(new Run(""))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });
            groupHeader.Cells.Add(new TableCell(new Paragraph(new Run(""))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });
            groupHeader.Cells.Add(new TableCell(new Paragraph(new Run(""))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });
            groupHeader.Cells.Add(groupCell);
            groupHeader.Cells.Add(new TableCell(new Paragraph(new Run(""))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });
            groupHeader.Cells.Add(new TableCell(new Paragraph(new Run(""))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });
            groupHeader.Cells.Add(new TableCell(new Paragraph(new Run(""))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });



            // Header
            TableRow knjigaHeader = new TableRow();
            knjigaGroup.Rows.Add(knjigaHeader);
            knjigaHeader.Cells.Add(new TableCell(new Paragraph(new Run("#"))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });
            knjigaHeader.Cells.Add(new TableCell(new Paragraph(new Run("Namirnica"))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });
            knjigaHeader.Cells.Add(new TableCell(new Paragraph(new Run("JM"))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });
            knjigaHeader.Cells.Add(new TableCell(new Paragraph(new Run("Prenesene zalihe iz prethodnog dana"))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White});

          
            // Grupna kolona "Nabavke u toku dana"
            knjigaHeader.Cells.Add(new TableCell(new Paragraph(new Run("Količina"))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });
            knjigaHeader.Cells.Add(new TableCell(new Paragraph(new Run("Dobavljač"))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });
            knjigaHeader.Cells.Add(new TableCell(new Paragraph(new Run("Dokument"))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });

            knjigaHeader.Cells.Add(new TableCell(new Paragraph(new Run("Ukupno zaduženje"))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White});
            knjigaHeader.Cells.Add(new TableCell(new Paragraph(new Run("Utrošak u toku dana"))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });
            knjigaHeader.Cells.Add(new TableCell(new Paragraph(new Run("Ostatak robe - Prenos za naredni dan"))) { TextAlignment = TextAlignment.Center, Background = Brushes.Gray, Foreground = Brushes.White });

            // Rows (tvoje postojeće foreach)
            foreach (var s in knjiga)
            {
                TableRow r = new TableRow();
                r.Cells.Add(new TableCell(new Paragraph(new Run(s.RedniBroj.ToString()))) { TextAlignment = TextAlignment.Center, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5)   });
                r.Cells.Add(new TableCell(new Paragraph(new Run(s.Namirnica))) { TextAlignment = TextAlignment.Left, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
                r.Cells.Add(new TableCell(new Paragraph(new Run(s.JedinicaMjere))) { TextAlignment = TextAlignment.Center, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
                r.Cells.Add(new TableCell(new Paragraph(new Run((s.OstatakOdJuce ?? 0m).ToString("F2")))) { TextAlignment = TextAlignment.Right, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });

                r.Cells.Add(new TableCell(new Paragraph(new Run((s.NabavljenoDanas ?? 0m).ToString("F2")))) { TextAlignment = TextAlignment.Right, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
                r.Cells.Add(new TableCell(new Paragraph(new Run(s.Dobavljac))) { TextAlignment = TextAlignment.Center, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
                r.Cells.Add(new TableCell(new Paragraph(new Run(s.Dokument))) { TextAlignment = TextAlignment.Right, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });

                r.Cells.Add(new TableCell(new Paragraph(new Run((s.NaStanju ?? 0m).ToString("F2")))) { TextAlignment = TextAlignment.Right, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
                r.Cells.Add(new TableCell(new Paragraph(new Run((s.UtrosenoDanas ?? 0m).ToString("F2")))) { TextAlignment = TextAlignment.Right, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
                r.Cells.Add(new TableCell(new Paragraph(new Run((s.OstatakZaSutra ?? 0m).ToString("F2")))) { TextAlignment = TextAlignment.Right, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });

                knjigaGroup.Rows.Add(r);
            }

            // ====== Desna tabela: tblHrana (tvoje postojeće foreach) ======
            Table tblHrana = new Table();
            tblHrana.CellSpacing = 0;
            tblHrana.BorderBrush = Brushes.Black;
            tblHrana.BorderThickness = new Thickness(0.5);

            tblHrana.Columns.Add(new TableColumn() { Width = new GridLength(30) });
            tblHrana.Columns.Add(new TableColumn() { Width = new GridLength(170) });
            tblHrana.Columns.Add(new TableColumn() { Width = new GridLength(60) });
            tblHrana.Columns.Add(new TableColumn() { Width = new GridLength(60) });
            tblHrana.Columns.Add(new TableColumn() { Width = new GridLength(60) });

            TableRowGroup hranaGroup = new TableRowGroup();
            tblHrana.RowGroups.Add(hranaGroup);

            TableRow groupHeader2 = new TableRow();
            hranaGroup.Rows.Insert(0, groupHeader2); // dodajemo iznad postojećeg headera

            TableCell groupCell2 = new TableCell(new Paragraph(new Run("Realizacija hrane")))
            {
                TextAlignment = TextAlignment.Center,
                Background = Brushes.Gray,
                Foreground = Brushes.White,
                ColumnSpan = 5
            };
            
           
            groupHeader2.Cells.Add(groupCell2);
          
          


            TableRow hranaHeader = new TableRow();
            hranaGroup.Rows.Add(hranaHeader);
            hranaHeader.Cells.Add(new TableCell(new Paragraph(new Run("#"))) { Background = Brushes.Gray, Foreground = Brushes.White, Padding = new Thickness(2) });
            hranaHeader.Cells.Add(new TableCell(new Paragraph(new Run("Naziv"))) { Background = Brushes.Gray, Foreground = Brushes.White, Padding = new Thickness(2) });
            hranaHeader.Cells.Add(new TableCell(new Paragraph(new Run("Količina"))) { Background = Brushes.Gray, Foreground = Brushes.White, Padding = new Thickness(2) });
            hranaHeader.Cells.Add(new TableCell(new Paragraph(new Run("Cijena"))) { Background = Brushes.Gray, Foreground = Brushes.White, Padding = new Thickness(2) });
            hranaHeader.Cells.Add(new TableCell(new Paragraph(new Run("Vrijednost"))) { Background = Brushes.Gray, Foreground = Brushes.White, Padding = new Thickness(2) });

            foreach (var h in ListaHrane)
            {
                TableRow r = new TableRow();
                r.Cells.Add(new TableCell(new Paragraph(new Run(h.RedniBroj.ToString()))) { TextAlignment = TextAlignment.Center, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
                r.Cells.Add(new TableCell(new Paragraph(new Run(h.Naziv))) { TextAlignment = TextAlignment.Left, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
                r.Cells.Add(new TableCell(new Paragraph(new Run(h.Kolicina.GetValueOrDefault(0).ToString("F2")))) { TextAlignment = TextAlignment.Right, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
                r.Cells.Add(new TableCell(new Paragraph(new Run(h.Cijena.GetValueOrDefault(0).ToString("F2")))) { TextAlignment = TextAlignment.Right, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
                r.Cells.Add(new TableCell(new Paragraph(new Run(h.Vrijednost.GetValueOrDefault(0).ToString("F2")))) { TextAlignment = TextAlignment.Right, Padding = new Thickness(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0.5) });
                hranaGroup.Rows.Add(r);
            }
            TableRow totalHranaRow = new TableRow();

            totalHranaRow.Cells.Add(new TableCell(new Paragraph(new Run(""))) { ColumnSpan = 3, BorderThickness = new Thickness(0) }); // prazno lijevo

            totalHranaRow.Cells.Add(new TableCell(new Paragraph(new Run("UKUPNO:")))
            {
                TextAlignment = TextAlignment.Right,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(2),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5,0.5,0,0.5)
            });

            totalHranaRow.Cells.Add(new TableCell(new Paragraph(new Run(total.GetValueOrDefault().ToString("F2"))))
            {
                TextAlignment = TextAlignment.Right,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(2),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0,0.5,0.5,0.5)
            });

            hranaGroup.Rows.Add(totalHranaRow);

            // ====== Dodavanje tabela u horizontalni layout ======
            TableCell leftCell = new TableCell();
            leftCell.TextAlignment = TextAlignment.Left;
            leftCell.Blocks.Add(tblKnjiga);
            row.Cells.Add(leftCell);

            TableCell rightCell = new TableCell();
            rightCell.TextAlignment = TextAlignment.Left;
            rightCell.Blocks.Add(tblHrana);
            row.Cells.Add(rightCell);

   



            // ====== Dodavanje glavne tabele u dokument ======
            doc.Blocks.Add(mainTable);


            // ====== FOOTER ======
            Paragraph ukupno = new Paragraph();
            ukupno.Inlines.Add(new Bold(new Run("POTPIS:_______________________ ")));
            ukupno.TextAlignment = TextAlignment.Right;
            doc.Blocks.Add(ukupno);

            // Print
            PrintDialog pd = new PrintDialog();
            if (pd.ShowDialog() == true)
            {
                pd.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Knjiga kuhinje");
            }
        }

        private TableCell CreateCell(string text, TextAlignment alignment)
        {
            return new TableCell(new Paragraph(new Run(text)))
            {
                TextAlignment = alignment,
                Padding = new Thickness(3),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5)
            };
        }
        public class Jelo()
        {
            public int? RedniBroj {  get; set; }
            public string? Naziv { get; set; }
            public decimal? Kolicina {  get; set; }
            public decimal? Cijena { get; set; }
            public decimal? Vrijednost { get; set; }
        }
    }
  


}
