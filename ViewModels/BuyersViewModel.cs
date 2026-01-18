using Caupo.Data;
using Caupo.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;


namespace Caupo.ViewModels
{
    public partial class BuyersViewModel : INotifyPropertyChanged
    {


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
        public ObservableCollection<DatabaseTables.TblKupci> Buyers { get; set; } = new ObservableCollection<DatabaseTables.TblKupci> ();
        public ObservableCollection<DatabaseTables.TblRacuni> Receipts { get; set; } = new ObservableCollection<DatabaseTables.TblRacuni> ();

        public ObservableCollection<DatabaseTables.TblRacunStavka> ReceiptItems { get; set; } = new ObservableCollection<DatabaseTables.TblRacunStavka> ();

        private DatabaseTables.TblRacuni? _selectedReceipt;
        public DatabaseTables.TblRacuni? SelectedReceipt
        {
            get => _selectedReceipt;
            set
            {
                if(_selectedReceipt != value)
                {
                    _selectedReceipt = value;
                    OnPropertyChanged (nameof (SelectedReceipt));
                    _ = LoadReceiptItemsAsync (value);
                }
            }
        }

        private DatabaseTables.TblKupci? _selectedBuyer;

        public DatabaseTables.TblKupci? SelectedBuyer
        {
            get => _selectedBuyer;
            set
            {
                if(_selectedBuyer != value)
                {
                    _selectedBuyer = value;
                    OnPropertyChanged (nameof (SelectedBuyer));
                    _ = LoadReceiptsAsync (value);
                    EditBuyerCommand.NotifyCanExecuteChanged ();
                    DeleteBuyerCommand.NotifyCanExecuteChanged ();
                }
            }
        }


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



        private ObservableCollection<TblKupci>? buyersFilter;
        public ObservableCollection<TblKupci>? BuyersFilter
        {
            get => buyersFilter;
            set
            {
                buyersFilter = value;
                OnPropertyChanged (nameof (BuyersFilter));
            }
        }

        public string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if(_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged (nameof (SearchText));
                    Debug.WriteLine ($"SearchText changed to: ");  // Dodaj log za testiranje
                    FilterItems (_searchText);
                }
            }
        }





        public BuyersViewModel()
        {

            Buyers = new ObservableCollection<TblKupci> ();
            BuyersFilter = new ObservableCollection<TblKupci> ();
            Receipts = new ObservableCollection<TblRacuni> ();
            ReceiptItems = new ObservableCollection<DatabaseTables.TblRacunStavka> ();

            Debug.WriteLine ("SearchText = " + SearchText);
        }

        // --- RelayCommand za Edit ---
        [RelayCommand (CanExecute = nameof (CanEditDelete))]
        private async Task EditBuyerAsync()
        {
            if(SelectedBuyer == null || SelectedBuyer.Kupac == "Gradjani")
            {
                Debug.WriteLine ("EditBuyerAsync: SelectedBuyer je null, ili Gradjani izlazim.");
                return;
            }
            await OpenKupacPopupAsync (SelectedBuyer);
        }

        private bool CanEditDelete() => SelectedBuyer != null;

        // --- RelayCommand za Delete (isto princip) ---
        [RelayCommand (CanExecute = nameof (CanEditDelete))]
        private async Task DeleteBuyerAsync()
        {
            await DeleteSelectedBuyerAsync ();
        }

        // --- RelayCommand za Add ---
        [RelayCommand]
        private async Task AddBuyerAsync()
        {
            await OpenKupacPopupAsync ();
        }

        public async Task InitializeAsync()
        {
            await Task.WhenAll (
                LoadBuyersAsync (),
                LoadFirmaAsync ()
            );
        }

        public async Task LoadBuyersAsync()
        {
            try
            {
                using var db = new AppDbContext ();

                var buyers = await db.Kupci
                    .AsNoTracking ()
                    .OrderBy (k => k.Kupac)
                    .ToListAsync ();

                Buyers.Clear ();
                BuyersFilter.Clear ();

                foreach(var b in buyers)
                {
                    Buyers.Add (b);
                    BuyersFilter.Add (b);
                }

                SelectedBuyer = BuyersFilter.FirstOrDefault ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex);
            }
        }

        public async Task LoadFirmaAsync()
        {
            try
            {
                using var db = new AppDbContext ();
                Firma = await db.Firma.AsNoTracking ().FirstOrDefaultAsync ();
                OnPropertyChanged (nameof (Firma));
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex);
            }
        }


        public async Task LoadReceiptsAsync(TblKupci kupac)
        {
            if(kupac == null)
                return;

            try
            {
                using var db = new AppDbContext ();

                var receipts = await db.Racuni
                          .Where (r => r.Kupac == kupac.Kupac)
                          .OrderBy (r => r.BrojRacuna)
                          .AsNoTracking ()
                          .Select (r => new TblRacuni
                          {
                              BrojRacuna = r.BrojRacuna,
                              Kupac = r.Kupac,
                              BrojFiskalnogRacuna = r.BrojFiskalnogRacuna,
                              Datum = r.Datum,

                              RadnikName = db.Radnici
                                  .Where (rad => rad.IdRadnika.ToString () == r.Radnik)
                                  .Select (rad => rad.Radnik)
                                  .FirstOrDefault () ?? string.Empty,

                              Iznos = db.RacunStavka
                                  .Where (s => s.BrojRacuna == r.BrojRacuna)
                                  .Sum (s => s.Cijena * s.Kolicina) ?? 0
                          })
                          .ToListAsync ();

                Receipts.Clear ();
                foreach(var r in receipts)
                    Receipts.Add (r);

                SelectedReceipt = Receipts.FirstOrDefault ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex);
            }
        }


        public async Task LoadReceiptItemsAsync(TblRacuni racun)
        {
            if(racun == null)
                return;

            try
            {
                using var db = new AppDbContext ();

                var items = await db.RacunStavka
                    .Where (s => s.BrojRacuna == racun.BrojRacuna)
                    .OrderBy (s => s.IdStavke)
                    .AsNoTracking ()
                    .ToListAsync ();

                ReceiptItems.Clear ();
                foreach(var i in items)
                    ReceiptItems.Add (i);
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex);
            }
        }


        public async Task<decimal> IznosRacuna(int brojRacuna)
        {
            using var db = new AppDbContext ();

            return await db.RacunStavka
                .Where (x => x.BrojRacuna == brojRacuna)
                .SumAsync (x => x.Kolicina * x.Cijena) ?? 0;
        }

        private async Task<bool> OpenKupacPopupAsync(TblKupci? kupac = null)
        {
            var vm = new BuyerPopupViewModel (kupac);
            var window = new BuyerPopup
            {
                DataContext = vm,
                //Owner = Application.Current.MainWindow
            };

            var tcs = new TaskCompletionSource<bool> ();

            vm.CloseRequested += (s, result) =>
            {
                tcs.SetResult (result);
                window.Close ();
            };

            window.ShowDialog ();

            if(tcs.Task.Result) // korisnik kliknuo Save
            {
                using var db = new AppDbContext ();

                if(kupac == null)
                {
                    // Novi kupac
                    db.Kupci.Add (vm.Model);
                    await db.SaveChangesAsync ();
                    Buyers.Add (vm.Model);
                }
                else
                {
                    // Edit kupca
                    db.Kupci.Update (vm.Model);
                    await db.SaveChangesAsync ();

                    // update UI
                    var index = Buyers.IndexOf (kupac);
                    Buyers[index] = vm.Model;
                }

                SelectedBuyer = vm.Model;
                return true;
            }

            return false;
        }

        private async Task DeleteSelectedBuyerAsync()
        {
            if(SelectedBuyer == null || SelectedBuyer.Kupac == "Gradjani")
            {
                Debug.WriteLine ("DeleteSelectedBuyerAsync: SelectedBuyer je null,ili Gradjani  izlazim.");
                return;
            }

            Debug.WriteLine ("DeleteSelectedBuyerAsync: Pokušavam obrisati kupca: " + SelectedBuyer.Kupac);

            // Pretpostavljam da već imaš YesNo popup
            YesNoPopup myMessageBox = new YesNoPopup ();
            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
            myMessageBox.MessageText.Text = "Da li ste sigurni da želite obrisati kupca :" + Environment.NewLine + SelectedBuyer.Kupac + " ?";
            myMessageBox.ShowDialog ();

            Debug.WriteLine ("DeleteSelectedBuyerAsync: Kliknuo = " + myMessageBox.Kliknuo);

            if(myMessageBox.Kliknuo == "Da")
            {
                try
                {
                    using var db = new AppDbContext ();
                    Debug.WriteLine ("DeleteSelectedBuyerAsync: Tražim kupca u bazi sa Id = " + SelectedBuyer.IdKupca);

                    // Dobro je prvo dohvatiti entitet iz context-a
                    var kupacDb = await db.Kupci.FirstOrDefaultAsync (k => k.IdKupca == SelectedBuyer.IdKupca);

                    if(kupacDb == null)
                    {
                        Debug.WriteLine ("DeleteSelectedBuyerAsync: Kupac nije pronađen u bazi!");
                        return;
                    }

                    db.Kupci.Remove (kupacDb);
                    var changes = await db.SaveChangesAsync ();
                    Debug.WriteLine ("DeleteSelectedBuyerAsync: Broj izbrisanih zapisa iz baze: " + changes);

                    // Sada ukloni iz kolekcije
                    Buyers.Remove (SelectedBuyer);
                    BuyersFilter.Remove (SelectedBuyer);
                    Debug.WriteLine ("DeleteSelectedBuyerAsync: Kupac uklonjen iz ObservableCollection.");

                    SelectedBuyer = Buyers.FirstOrDefault ();
                    Debug.WriteLine ("DeleteSelectedBuyerAsync: Novi SelectedBuyer = " + (SelectedBuyer?.Kupac ?? "null"));
                }
                catch(Exception ex)
                {
                    Debug.WriteLine ("DeleteSelectedBuyerAsync: Exception = " + ex);
                }
            }
            else
            {
                Debug.WriteLine ("DeleteSelectedBuyerAsync: Korisnik je odustao od brisanja.");
            }
        }


        public event EventHandler<string?>? ErrorOccurred;
        protected virtual void OnErrorOccurred(string? message)
        {
            ErrorOccurred?.Invoke (this, message);
        }

        public void FilterItems(string? searchtext)
        {
            Debug.WriteLine ("Okida search");

            var lowerSearch = (searchtext ?? string.Empty).ToLower ();

            var filtered = Buyers
                .Where (a =>
                    (a.JIB?.ToString () ?? string.Empty).ToLower ().Contains (lowerSearch) ||
                    (a.Kupac?.ToString () ?? string.Empty).ToLower ().Contains (lowerSearch) ||
                    (a.PDV?.ToString () ?? string.Empty).ToLower ().Contains (lowerSearch) ||
                    (a.Mjesto?.ToString () ?? string.Empty).ToLower ().Contains (lowerSearch)
                )
                .ToList ();

            BuyersFilter = new ObservableCollection<DatabaseTables.TblKupci> (filtered);
        }

        public void PrintFaktura(
            ObservableCollection<TblRacunStavka> stavkeFakture,
            string nazivFirme,
            string adresa,
            string grad,
            string jib,
            string pdv,
            string zr,
            TblKupci kupac,
            string brojFakture,
            string brojFiskalnogRacuna,
            DateTime datumIzdavanja,
            string nacinPlacanja,
            ImageSource logo = null)
        {

            decimal? ukupanIznos = 0;
            decimal? pdvIznos = 0;
            decimal? ukupnoZaPlacanje = 0;
            FlowDocument doc = new FlowDocument
            {
                PageWidth = 700,
                PageHeight = 1122,
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily ("Segoe UI"),
                FontSize = 10,
                PagePadding = new Thickness (50)
            };

            // === ZAGLAVLJE FAKTURE ===
            Table headerTable = new Table ();
            headerTable.Columns.Add (new TableColumn { Width = new GridLength (100) }); // logo
            headerTable.Columns.Add (new TableColumn { Width = new GridLength (250) }); // podaci o firmi
            headerTable.Columns.Add (new TableColumn { Width = new GridLength (350) }); // podaci o kupcu i broj fakture

            TableRowGroup trg = new TableRowGroup ();
            headerTable.RowGroups.Add (trg);

            // Prvi red - Logo i naslov
            TableRow titleRow = new TableRow ();
            trg.Rows.Add (titleRow);

            // --- Logo ---
            Paragraph logoPar = new Paragraph ();
            if(logo != null)
            {
                System.Windows.Controls.Image img = new System.Windows.Controls.Image
                {
                    Source = logo,
                    Width = 80,
                    Height = 60,
                    Stretch = Stretch.Uniform
                };

                // Konvertuj Image u Inline
                InlineUIContainer logoContainer = new InlineUIContainer (img);
                logoPar.Inlines.Add (logoContainer);
            }
            else
            {
                logoPar.Inlines.Add (new Run ("LOGO"));
            }

            titleRow.Cells.Add (new TableCell (logoPar)
            {
                BorderThickness = new Thickness (0),
                TextAlignment = TextAlignment.Left,
                //VerticalAlignment = VerticalAlignment.Top
            });

            // --- Naslov fakture ---
            Paragraph naslovFakture = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness (0, 10, 0, 0)
            };
            naslovFakture.Inlines.Add ("FAKTURA");
            titleRow.Cells.Add (new TableCell (naslovFakture)
            {
                BorderThickness = new Thickness (1),
                //VerticalAlignment = VerticalAlignment.Top
            });

            // --- Broj fakture - desno ---
            Paragraph brojFakturePar = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness (0, 10, 0, 0)
            };
            brojFakturePar.Inlines.Add ($"Br: {brojFakture}");

            titleRow.Cells.Add (new TableCell (brojFakturePar)
            {
                BorderThickness = new Thickness (0),
                //VerticalAlignment = VerticalAlignment.Top
            });

            // Drugi red - Podaci o firmi i kupcu
            TableRow dataRow = new TableRow ();
            trg.Rows.Add (dataRow);



            // --- Podaci o firmi ---
            Paragraph firmaPar = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness (0, 10, 0, 0)
            };
            //firmaPar.Inlines.Add("PRODAVAC:\n");
            firmaPar.Inlines.Add (new Run (nazivFirme) { FontWeight = FontWeights.Bold });
            firmaPar.Inlines.Add (Environment.NewLine);
            firmaPar.Inlines.Add (adresa + Environment.NewLine);
            firmaPar.Inlines.Add (grad + Environment.NewLine);
            firmaPar.Inlines.Add ("JIB: " + jib + Environment.NewLine);
            firmaPar.Inlines.Add ("PDV: " + pdv + Environment.NewLine);
            firmaPar.Inlines.Add ("Žiro račun: " + zr);

            dataRow.Cells.Add (new TableCell (firmaPar)
            {
                BorderThickness = new Thickness (0),
                //VerticalAlignment = VerticalAlignment.Top
            });
            // --- Prazna ćelija ispod naslova ---
            dataRow.Cells.Add (new TableCell (new Paragraph ()) { BorderThickness = new Thickness (0) });
            // --- Podaci o kupcu ---
            Paragraph kupacPar = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness (0, 10, 0, 0)
            };
            kupacPar.Inlines.Add ("KUPAC:\n");
            kupacPar.Inlines.Add (new Run (kupac.Kupac) { FontWeight = FontWeights.Bold });
            kupacPar.Inlines.Add (Environment.NewLine);
            kupacPar.Inlines.Add (kupac.Adresa + Environment.NewLine);
            kupacPar.Inlines.Add (kupac.Mjesto + Environment.NewLine);
            kupacPar.Inlines.Add ("JIB: " + kupac.JIB);

            dataRow.Cells.Add (new TableCell (kupacPar)
            {
                BorderThickness = new Thickness (0),
                //VerticalAlignment = VerticalAlignment.Top
            });

            // Dodaj tabelu u dokument
            doc.Blocks.Add (headerTable);

            doc.Blocks.Add (new Paragraph (new Run (" ")) { FontSize = 10 });

            // === INFORMACIJE O FAKTURI ===
            Table infoTable = new Table ();
            infoTable.Columns.Add (new TableColumn { Width = new GridLength (150) });
            infoTable.Columns.Add (new TableColumn { Width = new GridLength (200) });
            infoTable.Columns.Add (new TableColumn { Width = new GridLength (150) });
            infoTable.Columns.Add (new TableColumn { Width = new GridLength (200) });

            TableRowGroup infoGroup = new TableRowGroup ();
            infoTable.RowGroups.Add (infoGroup);

            // Broj fakture i datum izdavanja
            TableRow infoRow1 = new TableRow ();
            infoGroup.Rows.Add (infoRow1);

            infoRow1.Cells.Add (CreateInfoCell ("Mjesto izdavanja:", TextAlignment.Left, true));
            infoRow1.Cells.Add (CreateInfoCell (Regex.Replace (grad, @"\d", ""), TextAlignment.Left, false));
            infoRow1.Cells.Add (CreateInfoCell ("Način plaćanja:", TextAlignment.Left, true));
            infoRow1.Cells.Add (CreateInfoCell (nacinPlacanja.ToString (), TextAlignment.Left, false));
            // Datum valute
            TableRow infoRow2 = new TableRow ();
            infoGroup.Rows.Add (infoRow2);
            infoRow2.Cells.Add (CreateInfoCell ("Datum izdavanja:", TextAlignment.Left, true));
            infoRow2.Cells.Add (CreateInfoCell (datumIzdavanja.ToString ("dd.MM.yyyy"), TextAlignment.Left, false));
            infoRow2.Cells.Add (CreateInfoCell ("Broj fiskalnog računa", TextAlignment.Left, true));
            infoRow2.Cells.Add (CreateInfoCell (brojFiskalnogRacuna, TextAlignment.Left, false));

            doc.Blocks.Add (infoTable);
            doc.Blocks.Add (new Paragraph (new Run (" ")) { FontSize = 10 });

            // === TABELA STAVKI ===
            Table table = new Table ();
            table.CellSpacing = 0;
            doc.Blocks.Add (table);

            // Definicija kolona
            table.Columns.Add (new TableColumn { Width = new GridLength (30) });
            table.Columns.Add (new TableColumn { Width = new GridLength (200) });
            table.Columns.Add (new TableColumn { Width = new GridLength (50) });
            table.Columns.Add (new TableColumn { Width = new GridLength (80) });
            table.Columns.Add (new TableColumn { Width = new GridLength (80) });
            table.Columns.Add (new TableColumn { Width = new GridLength (80) });
            table.Columns.Add (new TableColumn { Width = new GridLength (80) });

            // Header red
            TableRowGroup headerGroup = new TableRowGroup ();
            table.RowGroups.Add (headerGroup);
            TableRow headerRow = new TableRow ();
            headerGroup.Rows.Add (headerRow);

            string[] kolone = { "", "Artikl", "JM", "Količina", "Cijena", "PDV %", "Iznos" };
            foreach(var col in kolone)
            {
                TableCell cell = new TableCell (new Paragraph (new Run (col)))
                {
                    FontWeight = FontWeights.Medium,
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness (4),
                    FontSize = 9,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness (0, 0, 0, 1.5)
                };
                headerRow.Cells.Add (cell);
            }

            // Podaci stavki
            TableRowGroup bodyGroup = new TableRowGroup ();
            table.RowGroups.Add (bodyGroup);
            int rednibroj = 1;
            foreach(var stavka in stavkeFakture)
            {
                TableRow row = new TableRow ();
                bodyGroup.Rows.Add (row);

                row.Cells.Add (CreateCell (rednibroj.ToString (), TextAlignment.Center));
                row.Cells.Add (CreateCell (stavka.ArtiklNormativ, TextAlignment.Left));
                row.Cells.Add (CreateCell (stavka.JedinicaMjereName, TextAlignment.Center));
                row.Cells.Add (CreateCell (stavka.Kolicina?.ToString ("F2"), TextAlignment.Right));
                row.Cells.Add (CreateCell (stavka.Cijena?.ToString ("F2"), TextAlignment.Right));
                row.Cells.Add (CreateCell (stavka.PoreskaStopaPostotak?.ToString ("F2"), TextAlignment.Right));
                row.Cells.Add (CreateCell (stavka.Iznos?.ToString ("F2"), TextAlignment.Right));
                rednibroj++;
                ukupnoZaPlacanje += stavka.Kolicina * stavka.Cijena;
                decimal? osnovica;
                if(stavka.PoreskaStopaPostotak == 0)
                {
                    osnovica = stavka.Cijena; // Ako je stopa 0%, osnovica = cijena
                }
                else
                {
                    osnovica = stavka.Cijena / (1 + (stavka.PoreskaStopaPostotak / 100m));
                }
                pdvIznos += stavka.Kolicina * (stavka.Cijena - osnovica);
                ukupanIznos += stavka.Kolicina * osnovica;
            }

            doc.Blocks.Add (new Paragraph (new Run (" ")) { FontSize = 10 });

            // === UKUPNI IZNOSI ===
            Table totalTable = new Table ();
            totalTable.Columns.Add (new TableColumn { Width = new GridLength (300) });
            totalTable.Columns.Add (new TableColumn { Width = new GridLength (150) });
            totalTable.Columns.Add (new TableColumn { Width = new GridLength (150) });

            TableRowGroup totalGroup = new TableRowGroup ();
            totalTable.RowGroups.Add (totalGroup);

            // Ukupan iznos
            TableRow totalRow1 = new TableRow ();
            totalGroup.Rows.Add (totalRow1);
            totalRow1.Cells.Add (CreateTotalCell ("", TextAlignment.Right));
            totalRow1.Cells.Add (CreateTotalCell ("Iznos bez PDV:", TextAlignment.Center, true));
            totalRow1.Cells.Add (CreateTotalCell (ukupanIznos.GetValueOrDefault ().ToString ("F2") + " KM ", TextAlignment.Right));

            // PDV
            TableRow totalRow2 = new TableRow ();
            totalGroup.Rows.Add (totalRow2);
            totalRow2.Cells.Add (CreateTotalCell ("", TextAlignment.Right));
            totalRow2.Cells.Add (CreateTotalCell ("Ukupno PDV :", TextAlignment.Center, true));
            totalRow2.Cells.Add (CreateTotalCell (pdvIznos.GetValueOrDefault ().ToString ("F2") + " KM ", TextAlignment.Right));

            // Ukupno za plaćanje
            TableRow totalRow3 = new TableRow ();
            totalGroup.Rows.Add (totalRow3);
            totalRow3.Cells.Add (CreateTotalCell ("", TextAlignment.Right));
            totalRow3.Cells.Add (CreateTotalCell ("UKUPNO ZA PLAĆANJE:", TextAlignment.Center, true));
            totalRow3.Cells.Add (CreateTotalCell (ukupnoZaPlacanje.GetValueOrDefault ().ToString ("F2") + " KM ", TextAlignment.Right, true));

            doc.Blocks.Add (totalTable);

            // === FOOTER I POTPISI ===
            doc.Blocks.Add (new Paragraph (new Run (" ")) { FontSize = 15 });

            Paragraph napomena = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 10,
                FontStyle = FontStyles.Italic
            };
            napomena.Inlines.Add ("Napomena: " + "Faktura je generisana elektronski i vrijedi bez pečata i potpisa.");
            doc.Blocks.Add (napomena);

            doc.Blocks.Add (new Paragraph (new Run (" ")) { FontSize = 20 });

            // Potpisi
            Table potpisTable = new Table ();
            potpisTable.Columns.Add (new TableColumn { Width = new GridLength (350) });
            potpisTable.Columns.Add (new TableColumn { Width = new GridLength (350) });

            TableRowGroup potpisGroup = new TableRowGroup ();
            potpisTable.RowGroups.Add (potpisGroup);

            TableRow potpisRow = new TableRow ();
            potpisGroup.Rows.Add (potpisRow);

            Paragraph potpisProdavac = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 11
            };
            potpisProdavac.Inlines.Add ("__________________________\n");
            potpisProdavac.Inlines.Add ("Potpis prodavca");

            Paragraph potpisKupac = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 11
            };
            potpisKupac.Inlines.Add ("__________________________\n");
            potpisKupac.Inlines.Add ("Potpis kupca");

            potpisRow.Cells.Add (new TableCell (potpisProdavac) { BorderThickness = new Thickness (0) });
            potpisRow.Cells.Add (new TableCell (potpisKupac) { BorderThickness = new Thickness (0) });

            doc.Blocks.Add (potpisTable);

            // === ŠTAMPA ===

            try
            {
                PrintDialog printDlg = new PrintDialog ();
                printDlg.PrintQueue = LocalPrintServer.GetDefaultPrintQueue ();
                if(printDlg.ShowDialog () == true)
                {
                    IDocumentPaginatorSource idpSource = doc;
                    printDlg.PrintDocument (idpSource.DocumentPaginator, "Faktura br. " + brojFakture);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show ($"Greška pri štampanju: {ex.Message}", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Pomoćne metode ostaju iste...
        private TableCell CreateCell(string text, TextAlignment alignment)
        {
            return new TableCell (new Paragraph (new Run (text)))
            {
                TextAlignment = alignment,
                Padding = new Thickness (3),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness (0, 0, 0, 0.5),
                FontSize = 9
            };
        }

        private TableCell CreateInfoCell(string text, TextAlignment alignment, bool isBold)
        {
            Paragraph paragraph = new Paragraph (new Run (text));
            if(isBold)
            {
                paragraph.FontWeight = FontWeights.Medium;
            }

            return new TableCell (paragraph)
            {
                TextAlignment = alignment,
                Padding = new Thickness (2),
                BorderThickness = new Thickness (0),
                FontSize = 10
            };
        }

        private TableCell CreateTotalCell(string text, TextAlignment alignment, bool isBold = false)
        {
            Paragraph paragraph = new Paragraph (new Run (text));
            if(isBold)
            {
                paragraph.FontWeight = FontWeights.Bold;
            }

            return new TableCell (paragraph)
            {
                TextAlignment = alignment,
                Padding = new Thickness (4),
                BorderThickness = new Thickness (0),
                FontSize = 10
            };
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }
    }
}
