
using Caupo.Data;
using Caupo.Properties;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class BeverageInPageViewModel : INotifyPropertyChanged
    {
        // 1. STANJE I PODACI
        private TblUlaz? _selectedStockIn;
        private TblUlazStavke? _selectedStockInItem;
        private TblArtikli? _selectedArticle;
        private string? _searchText;
        private string? _searchArticleText;
        private bool _hasUnsavedChanges;
        private bool _isBusy;
        private bool _isLoading;
        private int _originalBrojUlaza;
        private string? _savedHeader;

        public TblFirma Klijent { get; private set; } = new();
        public TblDobavljaci? SelectedSupplier => Suppliers.FirstOrDefault(s => s.Dobavljac == SelectedStockIn?.Dobavljac);
        public ObservableCollection<TblDobavljaci> Suppliers { get; } = new();
        public ObservableCollection<TblUlaz> StockIn { get; } = new();
        public ObservableCollection<TblUlaz> StockInFilter { get; } = new();
        public ObservableCollection<TblUlazStavke> StockInItems { get; } = new();
        public ObservableCollection<TblArtikli> Artikli { get; } = new();
        public ObservableCollection<TblArtikli> ArtikliFilter { get; } = new();

        public TblUlaz? SelectedStockIn
        {
            get => _selectedStockIn;
            set
            {
                if (ReferenceEquals(_selectedStockIn, value)) return;
                _selectedStockIn = value;
                _originalBrojUlaza = value?.BrojUlaza ?? 0;
                _savedHeader = value == null ? null : HeaderFingerprint(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedSupplier));
                OnPropertyChanged(nameof(CanEdit));
                NotifyCommands();
            }
        }

        public TblUlazStavke? SelectedStockInItem
        {
            get => _selectedStockInItem;
            set { if (ReferenceEquals(_selectedStockInItem, value)) return; _selectedStockInItem = value; OnPropertyChanged(); NotifyCommands(); }
        }

        public TblArtikli? SelectedArticle
        {
            get => _selectedArticle;
            set { if (ReferenceEquals(_selectedArticle, value)) return; _selectedArticle = value; OnPropertyChanged(); NotifyCommands(); }
        }

        public string? SearchText
        {
            get => _searchText;
            set { if (_searchText == value) return; _searchText = value; OnPropertyChanged(); FilterItems(value); }
        }

        public string? SearchArticleText
        {
            get => _searchArticleText;
            set { if (_searchArticleText == value) return; _searchArticleText = value; OnPropertyChanged(); FilterArticleItems(value); }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges || (SelectedStockIn != null && _savedHeader != null && HeaderFingerprint(SelectedStockIn) != _savedHeader);
            private set { if (_hasUnsavedChanges == value) return; _hasUnsavedChanges = value; OnPropertyChanged(); NotifyCommands(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set { if (_isBusy == value) return; _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanEdit)); NotifyCommands(); }
        }

        public bool CanEdit => !IsBusy && SelectedStockIn != null;
        public decimal? IznosRacuna => StockInItems.Sum(s => s.IznosBezUPDV);
        public decimal EnteredPrice { get; set; }
        public decimal EnteredQuantity { get; set; }
        public decimal EnteredDiscount { get; set; }

        // 2. KOMANDE I DIJALOZI
        public IAsyncRelayCommand AddNewStockInCommand { get; }
        public IAsyncRelayCommand SaveStockInCommand { get; }
        public IRelayCommand<TblUlazStavke> DeleteArticleCommand { get; }
        public IAsyncRelayCommand PreviousStockInCommand { get; }
        public IAsyncRelayCommand NextStockInCommand { get; }
        public IRelayCommand AddArticleCommand { get; }
        public IRelayCommand EditStockInItemCommand { get; }
        public IRelayCommand PrintStockInCommand { get; }
        public IAsyncRelayCommand DiscardChangesCommand { get; }

        // View prikazuje dijalog; ViewModel odlučuje kada i zašto.
        public event Func<string, bool>? ShowDeletePopupRequested;
        public event Func<string, bool>? ConfirmDiscardRequested;
        public event Func<TblArtikli, (bool Accepted, decimal Price, decimal Quantity, decimal Discount)>? AddArticleRequested;
        public event Func<TblUlazStavke, (bool Accepted, decimal Price, decimal Quantity, decimal Discount)>? EditItemRequested;
        public event EventHandler<string?>? ErrorOccurred;
        public event EventHandler<string?>? InformationOccurred;
        public event PropertyChangedEventHandler? PropertyChanged;

        public BeverageInPageViewModel()
        {
            AddNewStockInCommand = new AsyncRelayCommand(AddNewStockInAsync, () => !IsBusy);
            SaveStockInCommand = new AsyncRelayCommand(SaveSelectedStockInAsync, () => CanEdit && !IsBusy);
            DeleteArticleCommand = new RelayCommand<TblUlazStavke>(DeleteStockInItem, s => s != null && CanEdit);
            PreviousStockInCommand = new AsyncRelayCommand(() => NavigateAsync(-1), () => !IsBusy);
            NextStockInCommand = new AsyncRelayCommand(() => NavigateAsync(1), () => !IsBusy);
            AddArticleCommand = new RelayCommand(RequestAddArticle, () => CanEdit && SelectedArticle != null);
            EditStockInItemCommand = new RelayCommand(RequestEditItem, () => CanEdit && SelectedStockInItem != null);
            PrintStockInCommand = new RelayCommand(PrintCurrentStockIn, () => SelectedStockIn != null && SelectedSupplier != null);
            DiscardChangesCommand = new AsyncRelayCommand(DiscardChangesAsync, () => HasUnsavedChanges && !IsBusy);
            StockInItems.CollectionChanged += (_, _) => { OnPropertyChanged(nameof(IznosRacuna)); };
        }

        // 3. INICIJALIZACIJA I UČITAVANJE
        public async Task Start()
        {
            await LoadArticlesAsync();
            await LoadSuppliersAsync();
            await LoadFirmaAsync();
        }

        public async Task InitializeAsync(int brojUlaza = 0)
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                await Start();
                await LoadStockInAsync(brojUlaza);
            }
            catch (Exception ex) { ReportError("Učitavanje Ulaza nije uspjelo.", ex); }
            finally { IsBusy = false; }
        }

        public async Task LoadFirmaAsync()
        {
            using var db = new AppDbContext();
            Klijent = await db.Firma.AsNoTracking().FirstOrDefaultAsync() ?? new TblFirma();
            OnPropertyChanged(nameof(Klijent));
        }

        public async Task LoadSuppliersAsync()
        {
            using var db = new AppDbContext();
            var rows = await db.Dobavljaci.AsNoTracking().OrderBy(x => x.Dobavljac).ToListAsync();
            Suppliers.Clear();
            foreach (var row in rows) Suppliers.Add(row);
            OnPropertyChanged(nameof(SelectedSupplier));
        }

        public async Task LoadArticlesAsync()
        {
            using var db = new AppDbContext();
            var rows = await db.Artikli.AsNoTracking().Where(a => a.VrstaArtikla == 0).OrderBy(a => a.Artikl).ToListAsync();
            Artikli.Clear();
            foreach (var row in rows) Artikli.Add(row);
            FilterArticleItems(SearchArticleText);
            SelectedArticle = ArtikliFilter.FirstOrDefault();
        }

        public async Task LoadStockInAsync(int brojulaza)
        {
            using var db = new AppDbContext();
            var rows = await db.Ulaz.AsNoTracking().OrderBy(x => x.BrojUlaza).ToListAsync();
            var radnici = await db.Radnici.AsNoTracking().ToListAsync();
            StockIn.Clear();
            foreach (var row in rows)
            {
                row.RadnikName = radnici.FirstOrDefault(r => r.IdRadnika.ToString() == row.Radnik)?.Radnik ?? string.Empty;
                StockIn.Add(row);
            }
            FilterItems(SearchText);
            var selected = brojulaza != 0 ? StockIn.FirstOrDefault(x => x.BrojUlaza == brojulaza) : StockIn.LastOrDefault();
            await SelectStockInAsync(selected);
        }

        public async Task LoadStockInItems(TblUlaz? selectedulaz)
        {
            StockInItems.Clear();
            SelectedStockInItem = null;
            if (selectedulaz == null) return;
            using var db = new AppDbContext();
            var rows = await db.UlazStavke.AsNoTracking()
                .Where(s => s.BrojUlaza == selectedulaz.BrojUlaza)
                .OrderBy(s => s.RedniBroj).ToListAsync();
            foreach (var row in rows) StockInItems.Add(row);
            OnPropertyChanged(nameof(IznosRacuna));
        }

        // 4. ODABIR, NAVIGACIJA I PRETRAGA
        public async Task SelectStockInAsync(TblUlaz? ulaz)
        {
            if (!CanLeaveCurrent()) return;
            _isLoading = true;
            try
            {
                SelectedStockIn = ulaz;
                await LoadStockInItems(ulaz);
                HasUnsavedChanges = false;
            }
            finally { _isLoading = false; }
        }

        private static string HeaderFingerprint(TblUlaz x) => string.Join("|", x.Datum.Ticks, x.Dobavljac, x.BrojFakture, x.IznosFakture, x.Locked);

        private bool CanLeaveCurrent() => !HasUnsavedChanges || (ConfirmDiscardRequested?.Invoke("Postoje nespremljene promjene. Odbaciti ih?") ?? false);

        private async Task NavigateAsync(int direction)
        {
            if (SelectedStockIn == null || StockInFilter.Count == 0) return;
            int index = StockInFilter.IndexOf(SelectedStockIn);
            int next = index + direction;
            if (next >= 0 && next < StockInFilter.Count) await SelectStockInAsync(StockInFilter[next]);
        }

        public void FilterItems(string? searchtext)
        {
            string q = (searchtext ?? "").Trim();
            var rows = StockIn.Where(x => string.IsNullOrEmpty(q) ||
                x.BrojUlaza.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) ||
                x.Datum.ToString("dd.MM.yyyy").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (x.Dobavljac?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            StockInFilter.Clear();
            foreach (var row in rows) StockInFilter.Add(row);
        }

        public void FilterArticleItems(string? searcharticletext)
        {
            string q = (searcharticletext ?? "").Trim();
            var rows = Artikli.Where(x => string.IsNullOrEmpty(q) ||
                (x.Sifra?.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Artikl?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            ArtikliFilter.Clear();
            foreach (var row in rows) ArtikliFilter.Add(row);
        }

        // 5. NOVI ULAZ I RADNE STAVKE (BEZ UPISA U BAZU)
        private async Task AddNewStockInAsync()
        {
            if (!CanLeaveCurrent()) return;
            using var db = new AppDbContext();
            int max = await db.Ulaz.MaxAsync(x => (int?)x.BrojUlaza) ?? 0;
            var row = new TblUlaz
            {
                Datum = DateTime.Now,
                BrojUlaza = max + 1,
                Radnik = Globals.ulogovaniKorisnik.IdRadnika.ToString(),
                RadnikName = Globals.ulogovaniKorisnik.Radnik
            };
            StockIn.Add(row);
            FilterItems(SearchText);
            SelectedStockIn = row;
            StockInItems.Clear();
            SelectedStockInItem = null;
            HasUnsavedChanges = true;
        }

        private void RequestAddArticle()
        {
            if (SelectedArticle == null || SelectedStockIn == null) return;
            var result = AddArticleRequested?.Invoke(SelectedArticle);
            if (result != null && result.Value.Accepted)
                AddArticle(SelectedArticle, result.Value.Price, result.Value.Quantity, result.Value.Discount);
        }

        private void RequestEditItem()
        {
            if (SelectedStockInItem == null) return;
            var result = EditItemRequested?.Invoke(SelectedStockInItem);
            if (result != null && result.Value.Accepted)
                UpdateStockInItem(SelectedStockInItem, result.Value.Price, result.Value.Quantity, result.Value.Discount);
        }

        public void AddArticle(TblArtikli article, decimal price, decimal quantity, decimal discount)
        {
            if (SelectedStockIn == null) return;
            if (!ValidateItem(price, quantity, discount)) return;
            bool pdv = Settings.Default.PDVKorisnik == "DA";
            var row = new TblUlazStavke
            {
                BrojUlaza = SelectedStockIn.BrojUlaza,
                ObracunPDV = pdv,
                Artikl = article.Artikl,
                Sifra = article.Sifra,
                Kolicina = quantity,
                Cijena = article.Normativ == 0 ? article.Cijena : article.Cijena / article.Normativ,
                VrstaArtikla = article.VrstaArtikla,
                JedinicaMjere = article.JedinicaMjere,
                PoreskaStopa = pdv ? article.PoreskaStopa : 1,
                NivelacijaID = 0
            };
            ApplyItemValues(row, price, quantity, discount);
            StockInItems.Add(row);
            HasUnsavedChanges = true;
        }

        public void UpdateStockInItem(TblUlazStavke row, decimal price, decimal quantity, decimal discount)
        {
            if (!StockInItems.Contains(row) || !ValidateItem(price, quantity, discount)) return;
            ApplyItemValues(row, price, quantity, discount);
            System.Windows.Data.CollectionViewSource.GetDefaultView(StockInItems).Refresh();
            OnPropertyChanged(nameof(IznosRacuna));
            HasUnsavedChanges = true;
        }

        private static void ApplyItemValues(TblUlazStavke row, decimal price, decimal quantity, decimal discount)
        {
            row.Kolicina = quantity;
            row.Rabat = discount;
            row.CijenaBezUPDV = price;
            row.IznosBezUPDV = quantity * price;
            // Privremeno zadržavamo postojeću formulu; poresku kalkulaciju izdvajamo nakon provjere pravila.
            row.CijenaSaUPDV = price * 1.17m;
            row.IznosUPDVa = row.ObracunPDV ? quantity * (row.CijenaSaUPDV * 0.14529m) : 0;
        }

        private bool ValidateItem(decimal price, decimal quantity, decimal discount)
        {
            if (price < 0 || quantity <= 0 || discount < 0 || discount > 100)
            {
                OnErrorOccurred("Količina mora biti veća od nule, cijena nenegativna, a rabat između 0 i 100%.");
                return false;
            }
            return true;
        }

        private void DeleteStockInItem(TblUlazStavke? row)
        {
            if (row == null || !StockInItems.Contains(row)) return;
            if (ShowDeletePopupRequested?.Invoke(row.Artikl ?? "") != true) return;
            StockInItems.Remove(row);
            HasUnsavedChanges = true;
        }

        // 6. TRANSAKCIJSKO SPREMANJE INSERT/UPDATE/DELETE
        public async Task SaveSelectedStockInAsync()
        {
            if (SelectedStockIn == null || IsBusy) return;
            var edited = SelectedStockIn;
            if (string.IsNullOrWhiteSpace(edited.Dobavljac) || string.IsNullOrWhiteSpace(edited.BrojFakture))
            {
                OnErrorOccurred("Dobavljač i broj fakture su obavezni.");
                return;
            }
            if (StockInItems.Any(s => s.Kolicina <= 0))
            {
                OnErrorOccurred("Sve stavke moraju imati količinu veću od nule.");
                return;
            }
            IsBusy = true;
            try
            {
                using var db = new AppDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();
                TblUlaz? existing = null;
                if (edited.IdUlaza != 0)
                    existing = await db.Ulaz.FirstOrDefaultAsync(x => x.IdUlaza == edited.IdUlaza);
                if (edited.IdUlaza != 0 && existing == null)
                    throw new InvalidOperationException("Ulaz više ne postoji u bazi. Ponovo učitajte podatke.");

                int brojUlaza;
                if (existing == null)
                {
                    brojUlaza = (await db.Ulaz.MaxAsync(x => (int?)x.BrojUlaza) ?? 0) + 1;
                    existing = new TblUlaz
                    {
                        BrojUlaza = brojUlaza,
                        Radnik = edited.Radnik,
                        RadnikName = edited.RadnikName
                    };
                    db.Ulaz.Add(existing);
                }
                else
                {
                    brojUlaza = existing.BrojUlaza; // Identitet ulaza se nikad ne mijenja.
                }
                existing.Datum = edited.Datum;
                existing.Dobavljac = edited.Dobavljac;
                existing.BrojFakture = edited.BrojFakture;
                existing.IznosFakture = edited.IznosFakture;
                existing.Locked = edited.Locked;

                var saved = await db.UlazStavke.Where(x => x.BrojUlaza == brojUlaza).ToListAsync();
                var editedIds = StockInItems.Where(x => x.RedniBroj > 0).Select(x => x.RedniBroj).ToHashSet();
                foreach (var old in saved.Where(x => !editedIds.Contains(x.RedniBroj))) db.UlazStavke.Remove(old);
                foreach (var item in StockInItems)
                {
                    TblUlazStavke target;
                    if (item.RedniBroj > 0)
                    {
                        target = saved.FirstOrDefault(x => x.RedniBroj == item.RedniBroj)
                            ?? throw new InvalidOperationException($"Stavka {item.RedniBroj} više ne postoji u ovom Ulazu.");
                    }
                    else
                    {
                        target = new TblUlazStavke();
                        db.UlazStavke.Add(target);
                    }
                    CopyItem(item, target, brojUlaza);
                }
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                int savedNumber = existing.BrojUlaza;
                HasUnsavedChanges = false;
                await LoadStockInAsync(savedNumber);
                InformationOccurred?.Invoke(this, "Ulaz je uspješno sačuvan.");
            }
            catch (Exception ex) { ReportError("Spremanje Ulaza nije uspjelo. Nijedna djelimična izmjena nije potvrđena.", ex); }
            finally { IsBusy = false; }
        }

        private static void CopyItem(TblUlazStavke source, TblUlazStavke target, int brojUlaza)
        {
            target.BrojUlaza = brojUlaza;
            target.ObracunPDV = source.ObracunPDV;
            target.Artikl = source.Artikl;
            target.Sifra = source.Sifra;
            target.Kolicina = source.Kolicina;
            target.Cijena = source.Cijena;
            target.VrstaArtikla = source.VrstaArtikla;
            target.JedinicaMjere = source.JedinicaMjere;
            target.CijenaBezUPDV = source.CijenaBezUPDV;
            target.PoreskaStopa = source.PoreskaStopa;
            target.IznosBezUPDV = source.IznosBezUPDV;
            target.Rabat = source.Rabat;
            target.IznosUPDVa = source.IznosUPDVa;
            target.CijenaSaUPDV = source.CijenaSaUPDV;
            target.NivelacijaID = source.NivelacijaID;
        }

        // 7. ODBACIVANJE I PRIKAZ
        private async Task DiscardChangesAsync()
        {
            if (SelectedStockIn == null || !CanLeaveCurrent()) return;
            int broj = SelectedStockIn.BrojUlaza;
            HasUnsavedChanges = false;
            await LoadStockInAsync(broj);
        }

        private void PrintCurrentStockIn()
        {
            if (SelectedStockIn == null || SelectedSupplier == null) return;
            PrintKalkulacija(StockInItems, Klijent, SelectedStockIn, SelectedSupplier);
        }

        private void ReportError(string message, Exception ex)
        {
            Debug.WriteLine(ex);
            OnErrorOccurred(message + Environment.NewLine + ex.Message);
        }

        protected virtual void OnErrorOccurred(string? message) => ErrorOccurred?.Invoke(this, message);
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void NotifyCommands()
        {
            AddNewStockInCommand.NotifyCanExecuteChanged();
            SaveStockInCommand.NotifyCanExecuteChanged();
            DeleteArticleCommand.NotifyCanExecuteChanged();
            PreviousStockInCommand.NotifyCanExecuteChanged();
            NextStockInCommand.NotifyCanExecuteChanged();
            AddArticleCommand.NotifyCanExecuteChanged();
            EditStockInItemCommand.NotifyCanExecuteChanged();
            PrintStockInCommand.NotifyCanExecuteChanged();
            DiscardChangesCommand.NotifyCanExecuteChanged();
        }

        // 8. ŠTAMPANJE KALKULACIJE (POSTOJEĆI FORMAT)
        public void PrintKalkulacija(ObservableCollection<TblUlazStavke> stavke, TblFirma klijent, TblUlaz ulaz, TblDobavljaci dobavljac)
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
            sredinaPar.Inlines.Add(new Run("KALKULACIJA CIJENA: " + ulaz.BrojUlaza + "/" + ulaz.Datum.ToString("yy") + Environment.NewLine)
            {
                FontSize = 20,
                FontWeight = FontWeights.Normal
            });
            sredinaPar.Inlines.Add(new Run(Environment.NewLine) { FontSize = 10 });
            sredinaPar.Inlines.Add(new Run("Datum: " + ulaz.Datum.ToString("dd.MM.yyyy") + Environment.NewLine) { FontSize = 15 });
            sredinaPar.Inlines.Add(new Run("Broj fakture: " + ulaz.BrojFakture) { FontSize = 15 });
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

            Table table = new Table { CellSpacing = 0 };
            doc.Blocks.Add(table);

            string[] kolone =
            {
                "#", "Naziv artikla", "JM", "Količina", "Fakturna cijena po jedinici mjere bez PDV-a",
                "Fakturna vrijednost bez PDV-a", "Zavisni troškovi bez PDV",
                "Nabavna cijena po jedinici mjere bez PDV", "Nabavna vrijednost bez PDV-a",
                "Stopa razlike u cijeni", "Iznos razlike u cijeni", "Prodajna vrijednost bez PDV-a",
                "Stopa PDV-a", "Iznos PDV-a", "Maloprodajna vrijednost sa PDV-om",
                "Maloprodajna cijena sa PDV-om"
            };

            int[] sirine = { 40, 200, 55, 55, 55, 55, 55, 55, 55, 55, 55, 55, 50, 50, 75, 75 };
            foreach (int sirina in sirine) table.Columns.Add(new TableColumn { Width = new GridLength(sirina) });

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
                    FontWeight = FontWeights.Bold
                };
                headerRow.Cells.Add(new TableCell(new BlockUIContainer(tb))
                {
                    Padding = new Thickness(3),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0.5)
                });
            }

            TableRowGroup bodyGroup = new TableRowGroup();
            table.RowGroups.Add(bodyGroup);

            decimal sumFakVr = 0, sumNabavna = 0, sumZavisni = 0, sumRabat = 0;
            decimal sumProdVr = 0, sumIznosPDV = 0, sumMP = 0;
            int index = 1;

            foreach (var s in stavke)
            {
                TableRow r = new TableRow();
                bodyGroup.Rows.Add(r);

                r.Cells.Add(CreateCell(index.ToString(), TextAlignment.Center));
                r.Cells.Add(CreateCell(s.Artikl ?? "", TextAlignment.Left));
                r.Cells.Add(CreateCell(s.JedinicaMjereName?.ToString() ?? "", TextAlignment.Center));
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
                sumRabat += s.Rabat;
                sumProdVr += s.ProdajnaVrijBezIPDV;
                sumIznosPDV += s.IznosIPDV;
                sumMP += s.MPVrijednost;
                index++;
            }

            TableRow sumRow = new TableRow();
            bodyGroup.Rows.Add(sumRow);
            string[] ukupno =
            {
                "", "UKUPNO", "", "", "",
                sumFakVr.ToString ("F2"), sumZavisni.ToString ("F2"), sumRabat.ToString ("F2"),
                sumNabavna.ToString ("F2"), "", "", sumProdVr.ToString ("F2"), "",
                sumIznosPDV.ToString ("F2"), sumMP.ToString ("F2"), ""
            };
            for (int i = 0; i < ukupno.Length; i++)
                sumRow.Cells.Add(CreateCell(ukupno[i], i == 1 ? TextAlignment.Left : TextAlignment.Right));

            Table footerTable = new Table();
            footerTable.Columns.Add(new TableColumn { Width = new GridLength(350) });
            footerTable.Columns.Add(new TableColumn { Width = new GridLength(400) });
            footerTable.Columns.Add(new TableColumn { Width = new GridLength(350) });

            TableRowGroup footerGroup = new TableRowGroup();
            footerTable.RowGroups.Add(footerGroup);
            TableRow footerRow = new TableRow();
            footerGroup.Rows.Add(footerRow);
            footerRow.Cells.Add(new TableCell(new Paragraph()) { BorderThickness = new Thickness(0) });

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
                    var pdfPrinter = server.GetPrintQueues().FirstOrDefault(p => p.FullName.Contains("Microsoft Print to PDF"));
                    if (pdfPrinter != null) printDlg.PrintQueue = pdfPrinter;
                }
                else printDlg.PrintQueue = defaultQueue;

                if (printDlg.ShowDialog() == true)
                    printDlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Kalkulacija cijena");
            }
            catch (PrintQueueException ex)
            {
                OnErrorOccurred("Ne može se otvoriti dijalog za štampu: " + ex.Message);
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
    }
}
