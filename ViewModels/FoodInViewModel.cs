
using Caupo.Data;
using Caupo.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using SearchSuggestion = Caupo.UserControls.SearchSuggestion;

using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class FoodInViewModel : INotifyPropertyChanged
    {
        public TblFirma Klijent = new TblFirma();

        public TblDobavljaci SelectedSupplier
        {
            get
            {
                if (SelectedStockIn == null || string.IsNullOrEmpty(SelectedStockIn.Dobavljac))
                    return null;

                return Suppliers.FirstOrDefault(s => s.Dobavljac == SelectedStockIn.Dobavljac);
            }
        }

        public ObservableCollection<DatabaseTables.TblDobavljaci> Suppliers { get; set; } = new ObservableCollection<DatabaseTables.TblDobavljaci>();
        public ObservableCollection<DatabaseTables.TblUlazRepromaterijal> StockIn { get; set; } = new ObservableCollection<DatabaseTables.TblUlazRepromaterijal>();
        public ObservableCollection<DatabaseTables.TblUlazRepromaterijalStavka> StockInItems { get; set; } = new ObservableCollection<DatabaseTables.TblUlazRepromaterijalStavka>();

        private DatabaseTables.TblUlazRepromaterijal? _selectedStockIn;
        public DatabaseTables.TblUlazRepromaterijal? SelectedStockIn
        {
            get => _selectedStockIn;
            set
            {
                if (_selectedStockIn != value)
                {
                    _selectedStockIn = value;
                    OnPropertyChanged(nameof(SelectedStockIn));
                }
            }
        }

        private DatabaseTables.TblUlazRepromaterijalStavka? _selectedStockInItem;
        public DatabaseTables.TblUlazRepromaterijalStavka? SelectedStockInItem
        {
            get => _selectedStockInItem;
            set
            {
                if (_selectedStockInItem != value)
                {
                    _selectedStockInItem = value;
                    OnPropertyChanged(nameof(SelectedStockInItem));
                }
            }
        }

        private ObservableCollection<TblUlazRepromaterijal>? stockInFilter;

        public ObservableCollection<TblUlazRepromaterijal>? StockInFilter
        {
            get => stockInFilter;
            set
            {
                stockInFilter = value;
                OnPropertyChanged(nameof(StockInFilter));
                OnPropertyChanged(nameof(SearchSuggestions));
            }
        }



        public IEnumerable<SearchSuggestion> SearchSuggestions =>
            (StockInFilter ?? Enumerable.Empty<TblUlazRepromaterijal>())
            .Select(ulaz => new SearchSuggestion
            {
                Title = ulaz.Dobavljac ?? string.Empty,
                Subtitle = ulaz.BrojUlaza.ToString(),
                Detail = ulaz.Datum.ToString("dd.MM.yyyy"),
                Item = ulaz
            });


        private DatabaseTables.TblRepromaterijal? _selectedArticle;
        public DatabaseTables.TblRepromaterijal? SelectedArticle
        {
            get => _selectedArticle;
            set
            {
                _selectedArticle = value;
                OnPropertyChanged(nameof(SelectedArticle));
            }
        }

        private ObservableCollection<TblRepromaterijal>? _artikli;
        public ObservableCollection<TblRepromaterijal>? Artikli
        {
            get => _artikli;
            set
            {
                _artikli = value;
                OnPropertyChanged(nameof(Artikli));
            }
        }

        private ObservableCollection<TblRepromaterijal>? _artikliFilter;
        public ObservableCollection<TblRepromaterijal>? ArtikliFilter
        {
            get => _artikliFilter;
            set
            {
                _artikliFilter = value;
                OnPropertyChanged(nameof(ArtikliFilter));
            }
        }

        public string? _searchArticleText;
        public string? SearchArticleText
        {
            get => _searchArticleText;
            set
            {
                if (_searchArticleText != value)
                {
                    _searchArticleText = value;
                    OnPropertyChanged(nameof(SearchArticleText));
                    Debug.WriteLine("SearchText changed to: ");
                    FilterArticleItems(_searchArticleText);
                }
            }
        }

        public string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    Debug.WriteLine("SearchText changed to: " + _searchText);
                    FilterItems(_searchText);
                }
            }
        }

        private decimal? iznosRacuna = 0;
        public decimal? IznosRacuna
        {
            get => iznosRacuna;
            set
            {
                iznosRacuna = value;
                OnPropertyChanged(nameof(IznosRacuna));
            }
        }

        private string? _imagePathPrintButton;
        public string? ImagePathPrintButton
        {
            get => _imagePathPrintButton;
            set
            {
                _imagePathPrintButton = value;
                OnPropertyChanged(nameof(ImagePathPrintButton));
            }
        }

        public decimal EnteredPrice { get; set; }
        public decimal EnteredQuantity { get; set; }
        public decimal EnteredDiscount { get; set; }

        public ICommand AddNewStockInCommand { get; }
        public ICommand SaveStockInCommand { get; }
        public ICommand DeleteArticleCommand { get; }

        // Navigacija kroz ulaze
        public IAsyncRelayCommand FirstStockInCommand { get; }
        public IAsyncRelayCommand PreviousStockInCommand { get; }
        public IAsyncRelayCommand NextStockInCommand { get; }
        public IAsyncRelayCommand LastStockInCommand { get; }

        public bool HasUnsavedChanges { get; set; }

        public event Func<string, bool>? ShowDeletePopupRequested;

        public FoodInViewModel()
        {
            AddNewStockInCommand = new RelayCommand(AddNewStockIn);
            SaveStockInCommand = new RelayCommand(async () => await SaveSelectedStockInAsync());
            DeleteArticleCommand = new RelayCommand<TblUlazRepromaterijalStavka>(DeleteStockInItem);

            FirstStockInCommand = new AsyncRelayCommand(() => NavigateToStockInAsync(0));
            PreviousStockInCommand = new AsyncRelayCommand(() => NavigateStockInAsync(-1));
            NextStockInCommand = new AsyncRelayCommand(() => NavigateStockInAsync(1));
            LastStockInCommand = new AsyncRelayCommand(() => NavigateToStockInAsync(-1));

            StockIn = new ObservableCollection<TblUlazRepromaterijal>();
            StockInFilter = new ObservableCollection<TblUlazRepromaterijal>();
            StockInItems = new ObservableCollection<TblUlazRepromaterijalStavka>();
            Suppliers = new ObservableCollection<TblDobavljaci>();
            Artikli = new ObservableCollection<TblRepromaterijal>();
            ArtikliFilter = new ObservableCollection<TblRepromaterijal>();

            Start();
        }

        private async void Start()
        {
            await LoadArticlesAsync();
            await LoadSuppliersAsync();
            await LoadFirmaAsync();
            HasUnsavedChanges = false;
        }

        // ============================================================
        // NAVIGACIJA ULAZA
        // ============================================================

        private async Task NavigateToStockInAsync(int position)
        {
            if (StockInFilter == null || StockInFilter.Count == 0)
                return;

            int index = position == -1 ? StockInFilter.Count - 1 : 0;

            SelectedStockIn = StockInFilter[index];
            await LoadStockInItems(SelectedStockIn);
        }

        private async Task NavigateStockInAsync(int direction)
        {
            if (StockInFilter == null || StockInFilter.Count == 0)
                return;

            int index = StockInFilter.IndexOf(SelectedStockIn);
            int next = index + direction;

            if (next < 0 || next >= StockInFilter.Count)
                return;

            SelectedStockIn = StockInFilter[next];
            await LoadStockInItems(SelectedStockIn);
        }

        // ============================================================
        // FIRMA
        // ============================================================

        public async Task LoadFirmaAsync()
        {
            try
            {
                using var db = new AppDbContext();
                Klijent = await db.Firma.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        // ============================================================
        // BRISANJE STAVKE
        // ============================================================

        private async void DeleteStockInItem(TblUlazRepromaterijalStavka stavka)
        {
            if (stavka == null)
                return;

            bool confirmDelete = ShowDeletePopupRequested?.Invoke(stavka.Artikl) ?? false;

            if (!confirmDelete)
                return;

            using var db = new AppDbContext();

            var item = await db.UlazRepromaterijalStavka.FindAsync(stavka.RedniBroj);
            if (item != null)
            {
                db.UlazRepromaterijalStavka.Remove(item);
                await db.SaveChangesAsync();
            }

            StockInItems.Remove(stavka);
        }

        // ============================================================
        // IZMJENA STAVKE
        // ============================================================

        public async Task UpdateStockInItem()
        {
            if (SelectedStockInItem == null)
                return;

            Debug.WriteLine("Radimo UPDATE stavke: " + SelectedStockInItem.Artikl);

            using var db = new AppDbContext();

            var item = await db.UlazRepromaterijalStavka.FindAsync(SelectedStockInItem.RedniBroj);

            if (item != null)
            {
                Debug.WriteLine("Našao je u bazi tu stavku redni broj: " + item.RedniBroj);

                item.CijenaBezUPDV = EnteredPrice;
                item.Kolicina = EnteredQuantity;
                item.Rabat = EnteredDiscount;
                item.Cijena = SelectedStockInItem.Cijena;
                item.BrojUlaza = SelectedStockIn.BrojUlaza;
                item.Artikl = SelectedStockInItem.Artikl;
                item.JedinicaMjere = SelectedStockInItem.JedinicaMjere;
                item.PoreskaStopa = SelectedStockInItem.PoreskaStopa;
                item.IznosBezUPDV = EnteredQuantity * EnteredPrice;
                item.CijenaSaUPDV = EnteredPrice * 1.17m;
                item.IznosUPDVa = EnteredQuantity * (item.CijenaSaUPDV * 0.14529m);

                await db.SaveChangesAsync();

                SelectedStockInItem.Cijena = item.Cijena;
                SelectedStockInItem.BrojUlaza = item.BrojUlaza;
                SelectedStockInItem.Artikl = item.Artikl;
                SelectedStockInItem.Kolicina = item.Kolicina;
                SelectedStockInItem.JedinicaMjere = item.JedinicaMjere;
                SelectedStockInItem.CijenaBezUPDV = item.CijenaBezUPDV;
                SelectedStockInItem.PoreskaStopa = item.PoreskaStopa;
                SelectedStockInItem.IznosBezUPDV = item.IznosBezUPDV;
                SelectedStockInItem.Rabat = item.Rabat;
                SelectedStockInItem.IznosUPDVa = item.IznosUPDVa;
                SelectedStockInItem.CijenaSaUPDV = item.CijenaSaUPDV;

                CollectionViewSource.GetDefaultView(StockInItems).Refresh();

                Debug.WriteLine("Trebalo bi da je snimio kol: " + item.Kolicina +
                                ", cijena: " + item.CijenaBezUPDV);
            }
            else
            {
                Debug.WriteLine("Nije našao u bazi stavku: " + SelectedStockInItem.Artikl);

                SelectedStockInItem.CijenaBezUPDV = EnteredPrice;
                SelectedStockInItem.Kolicina = EnteredQuantity;
                SelectedStockInItem.Rabat = EnteredDiscount;
                SelectedStockInItem.BrojUlaza = SelectedStockIn.BrojUlaza;
                SelectedStockInItem.IznosBezUPDV = EnteredQuantity * EnteredPrice;
                SelectedStockInItem.CijenaSaUPDV = EnteredPrice * 1.17m;
                SelectedStockInItem.IznosUPDVa =
                    EnteredQuantity * (SelectedStockInItem.CijenaSaUPDV * 0.14529m);

                CollectionViewSource.GetDefaultView(StockInItems).Refresh();
                HasUnsavedChanges = true;
            }
        }

        // ============================================================
        // DODAVANJE ARTIKLA
        // ============================================================

        public void ProcessArticle()
        {
            Debug.WriteLine("Kreće ProcessArticle()");

            if (SelectedArticle == null)
                return;

            Debug.WriteLine("Imamo SelectedArticle " + SelectedArticle.Repromaterijal);

            TblUlazRepromaterijalStavka stavka = new TblUlazRepromaterijalStavka();

            stavka.BrojUlaza = SelectedStockIn.BrojUlaza;
            stavka.Artikl = SelectedArticle.Repromaterijal;
            stavka.Kolicina = EnteredQuantity;
            stavka.Cijena = SelectedArticle.PlanskaCijena;
            stavka.JedinicaMjere = SelectedArticle.JedinicaMjere;
            stavka.CijenaBezUPDV = EnteredPrice;
            stavka.PoreskaStopa = 2;
            stavka.IznosBezUPDV = EnteredQuantity * EnteredPrice;
            stavka.Rabat = EnteredDiscount;
            stavka.CijenaSaUPDV = EnteredPrice * 1.17m;
            stavka.IznosUPDVa = EnteredQuantity * (stavka.CijenaSaUPDV * 0.14529m);

            StockInItems.Add(stavka);
            HasUnsavedChanges = true;

            Debug.WriteLine("Dodao stavku: " + Environment.NewLine +
                "Artikl: " + stavka.Artikl + Environment.NewLine +
                "BrojUlaza: " + stavka.BrojUlaza + Environment.NewLine +
                "Kolicina: " + stavka.Kolicina + Environment.NewLine +
                "Cijena: " + stavka.Cijena + Environment.NewLine +
                "JedinicaMjere: " + stavka.JedinicaMjere + Environment.NewLine +
                "CijenaBezUPDV: " + stavka.CijenaBezUPDV + Environment.NewLine +
                "PoreskaStopa: " + stavka.PoreskaStopa + Environment.NewLine +
                "IznosBezUPDV: " + stavka.IznosBezUPDV + Environment.NewLine +
                "Rabat: " + stavka.Rabat + Environment.NewLine +
                "IznosUPDVa: " + stavka.IznosUPDVa + Environment.NewLine +
                "CijenaSaUPDV: " + stavka.CijenaSaUPDV);

            Debug.WriteLine("Imamo stavki: " + StockInItems.Count);
        }

        // ============================================================
        // SPREMANJE ULAZA
        // ============================================================

        public async Task SaveSelectedStockInAsync()
        {
            if (SelectedStockIn == null)
                return;

            if (string.IsNullOrWhiteSpace(SelectedStockIn.Dobavljac))
                return;

            if (string.IsNullOrWhiteSpace(SelectedStockIn.BrojFakture))
                return;

            try
            {
                using var db = new AppDbContext();
                using var transaction = await db.Database.BeginTransactionAsync();

                var existing = await db.UlazRepromaterijal
                    .FirstOrDefaultAsync(u => u.BrojUlaza == SelectedStockIn.BrojUlaza);

                List<TblUlazRepromaterijalStavka> existingItems =
                    new List<TblUlazRepromaterijalStavka>();

                if (existing != null)
                {
                    existingItems = await db.UlazRepromaterijalStavka
                        .Where(s => s.BrojUlaza == existing.BrojUlaza)
                        .ToListAsync();
                }

                bool isNew = existing == null;

                if (isNew)
                {
                    await db.UlazRepromaterijal.AddAsync(SelectedStockIn);
                    await db.SaveChangesAsync();

                    foreach (var s in StockInItems)
                    {
                        TblUlazRepromaterijalStavka stavka = new TblUlazRepromaterijalStavka
                        {
                            BrojUlaza = SelectedStockIn.BrojUlaza,
                            Artikl = s.Artikl,
                            Kolicina = s.Kolicina,
                            Cijena = s.Cijena,
                            JedinicaMjere = s.JedinicaMjere,
                            CijenaBezUPDV = s.CijenaBezUPDV,
                            PoreskaStopa = s.PoreskaStopa,
                            IznosBezUPDV = s.IznosBezUPDV,
                            Rabat = s.Rabat,
                            IznosUPDVa = s.IznosUPDVa,
                            CijenaSaUPDV = s.CijenaSaUPDV
                        };

                        await db.UlazRepromaterijalStavka.AddAsync(stavka);
                    }
                }
                else
                {
                    db.Entry(existing).CurrentValues.SetValues(SelectedStockIn);

                    foreach (var s in StockInItems)
                    {
                        bool isExisting = existingItems.Any(st => st.RedniBroj == s.RedniBroj);

                        if (!isExisting)
                        {
                            TblUlazRepromaterijalStavka nova = new TblUlazRepromaterijalStavka
                            {
                                BrojUlaza = existing.BrojUlaza,
                                Artikl = s.Artikl,
                                Kolicina = s.Kolicina,
                                Cijena = s.Cijena,
                                JedinicaMjere = s.JedinicaMjere,
                                CijenaBezUPDV = s.CijenaBezUPDV,
                                PoreskaStopa = s.PoreskaStopa,
                                IznosBezUPDV = s.IznosBezUPDV,
                                Rabat = s.Rabat,
                                IznosUPDVa = s.IznosUPDVa,
                                CijenaSaUPDV = s.CijenaSaUPDV
                            };

                            await db.UlazRepromaterijalStavka.AddAsync(nova);
                        }
                    }
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                HasUnsavedChanges = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Greška u SaveSelectedStockInAsync: " + ex);
                throw;
            }
        }

        // ============================================================
        // NOVI ULAZ
        // ============================================================

        private async void AddNewStockIn()
        {
            Debug.WriteLine("Dodavanje ulaza repromaterijala " + StockIn.Count);

            if (StockIn.Count != 0)
            {
                if (SelectedStockIn == null)
                    return;

                if (string.IsNullOrWhiteSpace(SelectedStockIn.Dobavljac))
                    return;

                if (string.IsNullOrWhiteSpace(SelectedStockIn.BrojFakture))
                    return;
            }

            var newStockIn = new TblUlazRepromaterijal
            {
                Datum = DateTime.Now,
                BrojUlaza = GenerateNextBrojUlaza(),
                Radnik = Globals.ulogovaniKorisnik.IdRadnika.ToString(),
                RadnikName = Globals.ulogovaniKorisnik.Radnik
            };

            StockIn.Add(newStockIn);
            StockInFilter?.Add(newStockIn);

            SelectedStockIn = newStockIn;
            await LoadStockInItems(SelectedStockIn);

            OnPropertyChanged(nameof(SelectedStockIn));
        }

        // ============================================================
        // ŠTAMPANJE KALKULACIJE
        // ============================================================

        public void PrintKalkulacija(
            ObservableCollection<TblUlazRepromaterijalStavka> stavke,
            TblFirma klijent,
            TblUlazRepromaterijal ulaz,
            TblDobavljaci dobavljac)
        {
            FlowDocument doc = new FlowDocument
            {
                PageWidth = 1100,
                PageHeight = 793,
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                PagePadding = new Thickness(30)
            };

            // HEADER
            Table headerTable = new Table();
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(300) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(400) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(300) });

            TableRowGroup trg = new TableRowGroup();
            headerTable.RowGroups.Add(trg);

            TableRow row = new TableRow();
            trg.Rows.Add(row);

            Paragraph korisnikPar = new Paragraph { TextAlignment = TextAlignment.Left };
            korisnikPar.Inlines.Add("   " + klijent.NazivFirme + Environment.NewLine);
            korisnikPar.Inlines.Add("   " + klijent.Adresa + Environment.NewLine);
            korisnikPar.Inlines.Add("   " + klijent.Grad + Environment.NewLine);
            korisnikPar.Inlines.Add("   JIB: " + klijent.JIB + Environment.NewLine);
            korisnikPar.Inlines.Add("   PDV: " + klijent.PDV);
            row.Cells.Add(new TableCell(korisnikPar) { BorderThickness = new Thickness(0) });

            Paragraph sredinaPar = new Paragraph { TextAlignment = TextAlignment.Center };

            Run naslovRun = new Run(
                "KALKULACIJA CIJENA: " + ulaz.BrojUlaza + "/" +
                DateTime.Now.ToString("yy") + Environment.NewLine)
            {
                FontSize = 20,
                FontWeight = FontWeights.Normal
            };
            sredinaPar.Inlines.Add(naslovRun);

            Run razmakRun = new Run(Environment.NewLine) { FontSize = 10 };
            sredinaPar.Inlines.Add(razmakRun);

            Run datumRun = new Run("Datum: " + ulaz.Datum.ToString("dd.MM.yyyy") + Environment.NewLine)
            {
                FontSize = 15
            };
            sredinaPar.Inlines.Add(datumRun);

            Run fakturaRun = new Run("Broj fakture: " + ulaz.BrojFakture)
            {
                FontSize = 15
            };
            sredinaPar.Inlines.Add(fakturaRun);

            row.Cells.Add(new TableCell(sredinaPar) { BorderThickness = new Thickness(0) });

            Paragraph dobavljacPar = new Paragraph { TextAlignment = TextAlignment.Left };
            dobavljacPar.Inlines.Add("   " + dobavljac.Dobavljac + Environment.NewLine);
            dobavljacPar.Inlines.Add("   " + dobavljac.Adresa + Environment.NewLine);
            dobavljacPar.Inlines.Add("   " + dobavljac.Mjesto + Environment.NewLine);
            dobavljacPar.Inlines.Add("   JIB: " + dobavljac.JIB + Environment.NewLine);
            dobavljacPar.Inlines.Add("   PDV: " + dobavljac.PDV);
            row.Cells.Add(new TableCell(dobavljacPar) { BorderThickness = new Thickness(0) });

            doc.Blocks.Add(headerTable);
            doc.Blocks.Add(new Paragraph(new Run(" ")) { FontSize = 6 });

            // TABELA
            Table table = new Table { CellSpacing = 0 };
            doc.Blocks.Add(table);

            string[] kolone =
            {
                "#",
                "Naziv artikla",
                "JM",
                "Količina",
                "Fakturna cijena po jedinici mjere bez PDV-a",
                "Fakturna vrijednost bez PDV-a",
                "Zavisni troškovi bez PDV",
                "Nabavna cijena po jedinici mjere bez PDV",
                "Nabavna vrijednost bez PDV -a",
                "Stopa razlike u cijeni",
                "Iznos razlike u cijeni",
                "Prodajna vrijednost bez PDV-a",
                "Stopa PDV-a",
                "Iznos PDV-a",
                "Maloprodajna vrijednost sa PDV-om",
                "Maloprodajna cijena sa PDV-om"
            };

            double[] sirine =
            {
                40, 200, 55, 55, 55, 55, 55, 55,
                55, 55, 55, 55, 50, 50, 75, 75
            };

            foreach (double sirina in sirine)
                table.Columns.Add(new TableColumn { Width = new GridLength(sirina) });

            TableRowGroup headerGroup = new TableRowGroup();
            table.RowGroups.Add(headerGroup);

            TableRow headerRow = new TableRow();
            headerGroup.Rows.Add(headerRow);

            foreach (var col in kolone)
            {
                TextBlock tb = new TextBlock
                {
                    Text = col,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0)
                };

                TableCell cell = new TableCell(new BlockUIContainer(tb))
                {
                    Padding = new Thickness(3),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0.5)
                };

                headerRow.Cells.Add(cell);
            }

            TableRowGroup bodyGroup = new TableRowGroup();
            table.RowGroups.Add(bodyGroup);

            decimal sumFakVr = 0;
            decimal sumNabavna = 0;
            decimal sumZavisni = 0;
            decimal sumRabat = 0;
            decimal sumProdVr = 0;
            decimal sumIznosPDV = 0;
            decimal sumMP = 0;

            int index = 1;

            foreach (var s in stavke)
            {
                TableRow r = new TableRow();
                bodyGroup.Rows.Add(r);

                r.Cells.Add(CreateCell(index.ToString(), TextAlignment.Center));
                r.Cells.Add(CreateCell(s.Artikl ?? "", TextAlignment.Left));
                r.Cells.Add(CreateCell(s.JedinicaMjere?.ToString() ?? "", TextAlignment.Center));
                r.Cells.Add(CreateCell(s.Kolicina.ToString("F2"), TextAlignment.Right));
                r.Cells.Add(CreateCell(s.CijenaBezUPDV.ToString("F2"), TextAlignment.Right));
                r.Cells.Add(CreateCell(s.FakVrBezPDV.ToString("F2"), TextAlignment.Right));
                r.Cells.Add(CreateCell("0.00", TextAlignment.Right));
                r.Cells.Add(CreateCell(s.NabavnaCijenaBezPDV.ToString("F2"), TextAlignment.Right));
                r.Cells.Add(CreateCell(s.NabavnaVrijednostBezPDV.ToString("F2"), TextAlignment.Right));
                r.Cells.Add(CreateCell(s.MarzaPostotak.ToString("F2"), TextAlignment.Right));
                r.Cells.Add(CreateCell(s.RazlikaCijBezPDV.ToString("F2"), TextAlignment.Right));
                r.Cells.Add(CreateCell(s.ProdajnaVrijBezIPDV.ToString("F2"), TextAlignment.Right));
                r.Cells.Add(CreateCell(s.PoreskaStopaPostotak?.ToString("F2") ?? "0", TextAlignment.Center));
                r.Cells.Add(CreateCell(s.IznosIPDV.ToString("F2"), TextAlignment.Right));
                r.Cells.Add(CreateCell(s.MPVrijednost.ToString("F2"), TextAlignment.Right));
                r.Cells.Add(CreateCell(s.Cijena?.ToString("F2") ?? "0", TextAlignment.Right));

                sumFakVr += s.FakVrBezPDV;
                sumNabavna += s.NabavnaVrijednostBezPDV;
                sumZavisni += 0;
                sumRabat += s.Rabat;
                sumProdVr += s.ProdajnaVrijBezIPDV;
                sumIznosPDV += s.IznosIPDV;
                sumMP += s.MPVrijednost;

                index++;
            }

            TableRow sumRow = new TableRow();
            bodyGroup.Rows.Add(sumRow);

            sumRow.Cells.Add(CreateCell("", TextAlignment.Center));
            sumRow.Cells.Add(CreateCell("UKUPNO", TextAlignment.Left));
            sumRow.Cells.Add(CreateCell("", TextAlignment.Center));
            sumRow.Cells.Add(CreateCell("", TextAlignment.Right));
            sumRow.Cells.Add(CreateCell("", TextAlignment.Right));
            sumRow.Cells.Add(CreateCell(sumFakVr.ToString("F2"), TextAlignment.Right));
            sumRow.Cells.Add(CreateCell(sumZavisni.ToString("F2"), TextAlignment.Right));
            sumRow.Cells.Add(CreateCell(sumRabat.ToString("F2"), TextAlignment.Right));
            sumRow.Cells.Add(CreateCell(sumNabavna.ToString("F2"), TextAlignment.Right));
            sumRow.Cells.Add(CreateCell("", TextAlignment.Right));
            sumRow.Cells.Add(CreateCell("", TextAlignment.Right));
            sumRow.Cells.Add(CreateCell(sumProdVr.ToString("F2"), TextAlignment.Right));
            sumRow.Cells.Add(CreateCell("", TextAlignment.Center));
            sumRow.Cells.Add(CreateCell(sumIznosPDV.ToString("F2"), TextAlignment.Right));
            sumRow.Cells.Add(CreateCell(sumMP.ToString("F2"), TextAlignment.Right));
            sumRow.Cells.Add(CreateCell("", TextAlignment.Right));

            // FOOTER
            Table footerTable = new Table();
            footerTable.Columns.Add(new TableColumn { Width = new GridLength(350) });
            footerTable.Columns.Add(new TableColumn { Width = new GridLength(400) });
            footerTable.Columns.Add(new TableColumn { Width = new GridLength(350) });

            TableRowGroup footerGroup = new TableRowGroup();
            footerTable.RowGroups.Add(footerGroup);

            TableRow footerRow = new TableRow();
            footerGroup.Rows.Add(footerRow);

            footerRow.Cells.Add(new TableCell(new Paragraph())
            {
                BorderThickness = new Thickness(0)
            });

            Paragraph sumaPar = new Paragraph();
            sumaPar.Inlines.Add($"UKUPNO: {sumFakVr:F2}" + Environment.NewLine);
            sumaPar.Inlines.Add($"PDV: {sumIznosPDV:F2}" + Environment.NewLine);
            sumaPar.Inlines.Add($"UKUPNO ZA PLAĆANJE: {(sumNabavna + sumIznosPDV):F2}");
            footerRow.Cells.Add(new TableCell(sumaPar));

            Paragraph potpisPar = new Paragraph();
            potpisPar.Inlines.Add("                                     Kalkulaciju sačinio: " + Environment.NewLine);
            potpisPar.Inlines.Add("M.P.                                                          " + Environment.NewLine);
            potpisPar.Inlines.Add("                                     ____________________");
            footerRow.Cells.Add(new TableCell(potpisPar));

            doc.Blocks.Add(footerTable);

            try
            {
                PrintDialog printDlg = new PrintDialog();

                LocalPrintServer server = new LocalPrintServer();
                PrintQueue defaultQueue = server.DefaultPrintQueue;

                defaultQueue.Refresh();

                if (defaultQueue.IsNotAvailable || defaultQueue.IsOffline)
                {
                    var pdfPrinter = server.GetPrintQueues()
                        .FirstOrDefault(p => p.FullName.Contains("Microsoft Print to PDF"));

                    if (pdfPrinter != null)
                        printDlg.PrintQueue = pdfPrinter;
                }
                else
                {
                    printDlg.PrintQueue = defaultQueue;
                }

                bool? result = printDlg.ShowDialog();

                if (result == true)
                {
                    printDlg.PrintDocument(
                        ((IDocumentPaginatorSource)doc).DocumentPaginator,
                        "Kalkulacija cijena");
                }
            }
            catch (System.Printing.PrintQueueException ex)
            {
                MessageBox.Show("Ne može se otvoriti dijalog za štampu:\n" + ex.Message);
            }
        }

        private TableCell CreateCell(string text, TextAlignment alignment)
        {
            return new TableCell(new Paragraph(new Run(text)))
            {
                TextAlignment = alignment,
                Padding = new Thickness(5),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5)
            };
        }

        // ============================================================
        // GENERISANJE BROJA ULAZA
        // ============================================================

        private int GenerateNextBrojUlaza()
        {
            return StockIn.Any() ? StockIn.Max(x => x.BrojUlaza) + 1 : 1;
        }

        // ============================================================
        // UČITAVANJE REPROMATERIJALA
        // ============================================================

        public async Task LoadArticlesAsync()
        {
            try
            {
                using var db = new AppDbContext();

                var artikli = await db.Repromaterijal.ToListAsync();

                Artikli?.Clear();
                ArtikliFilter?.Clear();

                foreach (var artikl in artikli)
                {
                    Artikli?.Add(artikl);
                    ArtikliFilter?.Add(artikl);

                    Debug.WriteLine(artikl.Repromaterijal +
                                    ", Artikli Count = " + Artikli?.Count);
                }

                Debug.WriteLine("ArtikliFilter Count = " + (ArtikliFilter?.Count ?? 0));

                SelectedArticle = ArtikliFilter?.FirstOrDefault();

                if (SelectedArticle != null)
                    Debug.WriteLine("SelectedArticle = " + SelectedArticle.Repromaterijal);
                else
                    Debug.WriteLine("SelectedArticle je null!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception na LoadArticlesAsync(): " + ex);
            }
        }

        // ============================================================
        // UČITAVANJE STAVKI ULAZA
        // ============================================================

        public async Task LoadStockInItems(TblUlazRepromaterijal selectedulaz)
        {
            if (selectedulaz == null)
                return;

            try
            {
                using var db = new AppDbContext();

                IznosRacuna = 0;

                var allStockInitems = await db.UlazRepromaterijalStavka.ToListAsync();

                var stockInitems = allStockInitems
                    .Where(a => a.BrojUlaza == selectedulaz.BrojUlaza)
                    .OrderBy(a => a.RedniBroj)
                    .ToList();

                StockInItems.Clear();

                foreach (var ri in stockInitems)
                {
                    StockInItems.Add(ri);
                    Debug.WriteLine("Ubacuje u stavke broj ulaza: " + ri.BrojUlaza);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public event EventHandler<string?>? ErrorOccurred;

        protected virtual void OnErrorOccurred(string? message)
        {
            ErrorOccurred?.Invoke(this, message);
        }

        // ============================================================
        // UČITAVANJE ULAZA
        // ============================================================

        public async Task LoadStockInAsync(int brojulaza)
        {
            Debug.WriteLine($"LoadStockInAsync started. BrojUlaza: {brojulaza}");

            try
            {
                using var db = new AppDbContext();

                var stockIns = await (
                    from u in db.UlazRepromaterijal
                    join r in db.Radnici
                        on u.Radnik equals r.IdRadnika.ToString() into rad
                    from r2 in rad.DefaultIfEmpty()
                    select new TblUlazRepromaterijal
                    {
                        IdUlaza = u.IdUlaza,
                        BrojUlaza = u.BrojUlaza,
                        Datum = u.Datum,
                        Dobavljac = u.Dobavljac,
                        BrojFakture = u.BrojFakture,
                        Locked = u.Locked,
                        Radnik = u.Radnik,
                        RadnikName = r2 != null ? r2.Radnik : string.Empty
                    }).ToListAsync();

                StockIn.Clear();
                StockInFilter?.Clear();

                foreach (var receipt in stockIns)
                {
                    StockIn.Add(receipt);
                    StockInFilter?.Add(receipt);

                    Debug.WriteLine(
                        $"Adding Ulaz: BrojUlaza={receipt.BrojUlaza}, " +
                        $"RadnikName={receipt.RadnikName}");
                }

                if (brojulaza != 0)
                {
                    SelectedStockIn = StockInFilter?
                        .FirstOrDefault(x => x.BrojUlaza == brojulaza);

                    Debug.WriteLine(
                        $"SelectedStockIn for BrojUlaza={brojulaza} is " +
                        $"{SelectedStockIn?.BrojUlaza}");
                }
                else
                {
                    SelectedStockIn = StockInFilter?.LastOrDefault();

                    Debug.WriteLine(
                        $"SelectedStockIn (last) is {SelectedStockIn?.BrojUlaza}");
                }

                Debug.WriteLine("StockInFilter Count: " + StockInFilter?.Count);

                await LoadStockInItems(SelectedStockIn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        // ============================================================
        // DOBAVLJAČI
        // ============================================================

        public async Task LoadSuppliersAsync()
        {
            try
            {
                using var db = new AppDbContext();

                var suppliers = await db.Dobavljaci.ToListAsync();

                Suppliers.Clear();

                foreach (var supplier in suppliers)
                    Suppliers.Add(supplier);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        // ============================================================
        // PRETRAGA ULAZA
        // ============================================================

        public void FilterItems(string? searchtext)
        {
            Debug.WriteLine("Okida search");

            string q = (searchtext ?? string.Empty).Trim();

            var filtered = StockIn
                .Where(a =>
                    a.BrojUlaza.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    a.Datum.ToString("dd.MM.yyyy").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (a.Dobavljac?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();

            StockInFilter = new ObservableCollection<TblUlazRepromaterijal>(filtered);

            // Pretraga ne mijenja SelectedStockIn.
            // Korisnik bira ulaz iz popup liste ili navigacijskim dugmetom.
        }

        // ============================================================
        // PRETRAGA REPROMATERIJALA
        // ============================================================

        public void FilterArticleItems(string? searcharticletext)
        {
            Debug.WriteLine("Okida article search");

            var lowerSearch = (searcharticletext ?? string.Empty).ToLower();

            var filtered = Artikli
                .Where(a =>
                    (a.Repromaterijal ?? string.Empty)
                    .ToLower()
                    .Contains(lowerSearch))
                .ToList();

            ArtikliFilter = new ObservableCollection<TblRepromaterijal>(filtered);
        }

        // ============================================================
        // PROPERTY CHANGED
        // ============================================================

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
