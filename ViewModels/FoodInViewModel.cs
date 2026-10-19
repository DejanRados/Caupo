
using Caupo.Data;
using Caupo.Services;
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
using static Caupo.Data.DatabaseTables;
using SearchSuggestion = Caupo.UserControls.SearchSuggestion;

namespace Caupo.ViewModels
{
    public class FoodInViewModel : INotifyPropertyChanged
    {
        private TblUlazRepromaterijal? _selectedStockIn;
        private TblUlazRepromaterijalStavka? _selectedStockInItem;
        private TblRepromaterijal? _selectedArticle;
        private string? _searchText, _searchArticleText, _savedHeader;
        private bool _dirty, _isBusy, _initialized;
        private int _originalBrojUlaza;

        public TblFirma Klijent { get; private set; } = new();
        public TblDobavljaci? SelectedSupplier
        {
            get => Suppliers.FirstOrDefault(s =>
                s.IdDobavljaca.ToString() == SelectedStockIn?.Dobavljac);
            set
            {
                if (SelectedStockIn == null || value == null) return;
                SelectedStockIn.Dobavljac = value.IdDobavljaca.ToString();
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasUnsavedChanges));
                NotifyCommands();
            }
        }
        public ObservableCollection<TblDobavljaci> Suppliers { get; } = new();
        public ObservableCollection<TblUlazRepromaterijal> StockIn { get; } = new();
        public ObservableCollection<TblUlazRepromaterijal> StockInFilter { get; } = new();
        public ObservableCollection<TblUlazRepromaterijalStavka> StockInItems { get; } = new();
        public ObservableCollection<TblRepromaterijal> Artikli { get; } = new();
        public ObservableCollection<TblRepromaterijal> ArtikliFilter { get; } = new();

        public IEnumerable<SearchSuggestion> SearchSuggestions => StockInFilter.Select(x => new SearchSuggestion
        {
            Title = Suppliers.FirstOrDefault(s => s.IdDobavljaca.ToString() == x.Dobavljac)?.Dobavljac ?? string.Empty,
            Subtitle = x.BrojUlaza.ToString(),
            Detail = x.Datum.ToString("dd.MM.yyyy"),
            Item = x
        });

        public TblUlazRepromaterijal? SelectedStockIn
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
                OnPropertyChanged(nameof(HasUnsavedChanges));
                NotifyCommands();
            }
        }

        public TblUlazRepromaterijalStavka? SelectedStockInItem
        {
            get => _selectedStockInItem;
            set
            {
                if (ReferenceEquals(_selectedStockInItem, value)) return;
                _selectedStockInItem = value;
                OnPropertyChanged();
                NotifyCommands();
            }
        }

        public TblRepromaterijal? SelectedArticle
        {
            get => _selectedArticle;
            set
            {
                if (ReferenceEquals(_selectedArticle, value)) return;
                _selectedArticle = value;
                OnPropertyChanged();
                NotifyCommands();
            }
        }

        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                FilterItems(value);
            }
        }

        public string? SearchArticleText
        {
            get => _searchArticleText;
            set
            {
                if (_searchArticleText == value) return;
                _searchArticleText = value;
                OnPropertyChanged();
                FilterArticleItems(value);
            }
        }

        public bool HasUnsavedChanges
        {
            get => _dirty || (SelectedStockIn != null && _savedHeader != null &&
                HeaderFingerprint(SelectedStockIn) != _savedHeader);
            set
            {
                _dirty = value;
                OnPropertyChanged();
                NotifyCommands();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanEdit));
                NotifyCommands();
            }
        }

        public bool CanEdit => !IsBusy && SelectedStockIn != null;
        public decimal? IznosRacuna => StockInItems.Sum(x => x.IznosBezUPDV);
        public decimal EnteredPrice { get; set; }
        public decimal EnteredQuantity { get; set; }
        public decimal EnteredDiscount { get; set; }
        public string? ImagePathPrintButton { get; set; }

        public IAsyncRelayCommand AddNewStockInCommand { get; }
        public IAsyncRelayCommand SaveStockInCommand { get; }
        public IRelayCommand<TblUlazRepromaterijalStavka> DeleteArticleCommand { get; }
        public IAsyncRelayCommand FirstStockInCommand { get; }
        public IAsyncRelayCommand PreviousStockInCommand { get; }
        public IAsyncRelayCommand NextStockInCommand { get; }
        public IAsyncRelayCommand LastStockInCommand { get; }
        public IRelayCommand AddArticleCommand { get; }
        public IRelayCommand EditStockInItemCommand { get; }
        public IRelayCommand PrintStockInCommand { get; }
        public IAsyncRelayCommand DiscardChangesCommand { get; }

        public event Func<string, bool>? ShowDeletePopupRequested;
        public event Func<string, bool>? ConfirmDiscardRequested;
        public event Func<TblRepromaterijal, (bool Accepted, decimal Price, decimal Quantity, decimal Discount)>? AddArticleRequested;
        public event Func<TblUlazRepromaterijalStavka, (bool Accepted, decimal Price, decimal Quantity, decimal Discount)>? EditItemRequested;
        public event EventHandler<string?>? ErrorOccurred;
        public event EventHandler<string?>? InformationOccurred;
        public event PropertyChangedEventHandler? PropertyChanged;

        public FoodInViewModel()
        {
            AddNewStockInCommand = new AsyncRelayCommand(AddNewStockInAsync, () => !IsBusy);
            SaveStockInCommand = new AsyncRelayCommand(SaveSelectedStockInAsync, () => CanEdit);
            DeleteArticleCommand = new RelayCommand<TblUlazRepromaterijalStavka>(
                DeleteStockInItem, x => x != null && CanEdit);
            FirstStockInCommand = new AsyncRelayCommand(
                () => NavigateToEndAsync(false), () => !IsBusy && StockInFilter.Count > 0);
            PreviousStockInCommand = new AsyncRelayCommand(() => NavigateAsync(-1), () => !IsBusy);
            NextStockInCommand = new AsyncRelayCommand(() => NavigateAsync(1), () => !IsBusy);
            LastStockInCommand = new AsyncRelayCommand(
                () => NavigateToEndAsync(true), () => !IsBusy && StockInFilter.Count > 0);
            AddArticleCommand = new RelayCommand(RequestAddArticle, () => CanEdit && SelectedArticle != null);
            EditStockInItemCommand = new RelayCommand(RequestEditItem, () => CanEdit && SelectedStockInItem != null);
            PrintStockInCommand = new RelayCommand(PrintCurrentStockIn,
                () => SelectedStockIn != null && SelectedSupplier != null);
            DiscardChangesCommand = new AsyncRelayCommand(DiscardChangesAsync,
                () => HasUnsavedChanges && !IsBusy);

            StockInItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IznosRacuna));
        }

        private static string HeaderFingerprint(TblUlazRepromaterijal x) =>
            string.Join("|", x.Datum.Ticks, x.Dobavljac, x.BrojFakture, x.Locked);

        private bool CanLeaveCurrent() => !HasUnsavedChanges ||
            (ConfirmDiscardRequested?.Invoke("Postoje nespremljene promjene. Odbaciti ih?") ?? false);

        public async Task Start()
        {
            await LoadArticlesAsync();
            await LoadSuppliersAsync();
            await LoadFirmaAsync();
            _initialized = true;
        }

        public async Task InitializeAsync(int brojUlaza = 0)
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                if (!_initialized) await Start();
                await LoadStockInCoreAsync(brojUlaza);
            }
            catch (Exception ex) { ReportError("Učitavanje ulaza hrane nije uspjelo.", ex); }
            finally { IsBusy = false; }
        }

        public Task LoadStockInAsync(int brojulaza) => InitializeAsync(brojulaza);

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
            var rows = await db.Repromaterijal.AsNoTracking()
                .OrderBy(x => x.Repromaterijal).ToListAsync();

            Artikli.Clear();
            foreach (var row in rows) Artikli.Add(row);
            FilterArticleItems(SearchArticleText);
            SelectedArticle = ArtikliFilter.FirstOrDefault();
        }

        private async Task LoadStockInCoreAsync(int brojulaza)
        {
            using var db = new AppDbContext();
            var rows = await db.UlazRepromaterijal.AsNoTracking()
                .OrderBy(x => x.BrojUlaza).ToListAsync();
            var radnici = await db.Radnici.AsNoTracking().ToListAsync();

            StockIn.Clear();
            foreach (var row in rows)
            {
                row.RadnikName = radnici.FirstOrDefault(
                    r => r.IdRadnika.ToString() == row.Radnik)?.Radnik ?? string.Empty;
                StockIn.Add(row);
            }

            FilterItems(SearchText);
            var selected = brojulaza != 0
                ? StockIn.FirstOrDefault(x => x.BrojUlaza == brojulaza)
                : StockIn.LastOrDefault();

            SelectedStockIn = selected;
            await LoadStockInItems(selected);
            HasUnsavedChanges = false;
        }

        public async Task LoadStockInItems(TblUlazRepromaterijal? selectedulaz)
        {
            StockInItems.Clear();
            SelectedStockInItem = null;
            if (selectedulaz == null) return;

            using var db = new AppDbContext();
            var rows = await db.UlazRepromaterijalStavka.AsNoTracking()
                .Where(x => x.BrojUlaza == selectedulaz.BrojUlaza)
                .OrderBy(x => x.RedniBroj).ToListAsync();

            foreach (var row in rows) StockInItems.Add(row);
            OnPropertyChanged(nameof(IznosRacuna));
        }

        public async Task SelectStockInAsync(TblUlazRepromaterijal? ulaz)
        {
            if (IsBusy || ReferenceEquals(SelectedStockIn, ulaz)) return;
            if (!CanLeaveCurrent()) return;

            int broj = ulaz?.BrojUlaza ?? 0;
            IsBusy = true;
            try { await LoadStockInCoreAsync(broj); }
            catch (Exception ex) { ReportError("Odabir ulaza nije uspio.", ex); }
            finally { IsBusy = false; }
        }

        private async Task NavigateAsync(int direction)
        {
            if (SelectedStockIn == null || StockInFilter.Count == 0) return;
            int index = StockInFilter.IndexOf(SelectedStockIn);
            int next = index + direction;
            if (next >= 0 && next < StockInFilter.Count)
                await SelectStockInAsync(StockInFilter[next]);
        }

        private async Task NavigateToEndAsync(bool last)
        {
            if (StockInFilter.Count == 0) return;
            await SelectStockInAsync(last ? StockInFilter[^1] : StockInFilter[0]);
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
            OnPropertyChanged(nameof(SearchSuggestions));
            FirstStockInCommand.NotifyCanExecuteChanged();
            LastStockInCommand.NotifyCanExecuteChanged();
        }

        public void FilterArticleItems(string? searcharticletext)
        {
            string q = (searcharticletext ?? "").Trim();
            var rows = Artikli.Where(x => string.IsNullOrEmpty(q) ||
                (x.Repromaterijal?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            ArtikliFilter.Clear();
            foreach (var row in rows) ArtikliFilter.Add(row);
        }

        private async Task AddNewStockInAsync()
        {
            if (!CanLeaveCurrent()) return;
            IsBusy = true;
            try
            {
                using var db = new AppDbContext();
                int broj = (await db.UlazRepromaterijal.MaxAsync(
                    x => (int?)x.BrojUlaza) ?? 0) + 1;

                await LoadStockInCoreAsync(0);

                var row = new TblUlazRepromaterijal
                {
                    BrojUlaza = broj,
                    Datum = DateTime.Now,
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
            catch (Exception ex) { ReportError("Kreiranje ulaza nije uspjelo.", ex); }
            finally { IsBusy = false; }
        }

        private void RequestAddArticle()
        {
            if (SelectedArticle == null || !CanEdit) return;
            var result = AddArticleRequested?.Invoke(SelectedArticle);
            if (result is { Accepted: true } v)
                AddArticle(SelectedArticle, v.Price, v.Quantity, v.Discount);
        }

        private void RequestEditItem()
        {
            if (SelectedStockInItem == null || !CanEdit) return;
            var result = EditItemRequested?.Invoke(SelectedStockInItem);
            if (result is { Accepted: true } v)
                UpdateStockInItem(SelectedStockInItem, v.Price, v.Quantity, v.Discount);
        }

        // Kompatibilnost sa starijim FoodInPage.xaml.cs.
        public void ProcessArticle()
        {
            if (SelectedArticle != null)
                AddArticle(SelectedArticle, EnteredPrice, EnteredQuantity, EnteredDiscount);
        }

        public Task UpdateStockInItem()
        {
            if (SelectedStockInItem != null)
                UpdateStockInItem(SelectedStockInItem, EnteredPrice, EnteredQuantity, EnteredDiscount);
            return Task.CompletedTask;
        }

        public void AddArticle(TblRepromaterijal article, decimal price, decimal quantity, decimal discount)
        {
            if (!CanEdit || !ValidateItem(price, quantity, discount)) return;

            var row = new TblUlazRepromaterijalStavka
            {
                BrojUlaza = SelectedStockIn!.BrojUlaza,
                Artikl = article.Repromaterijal,
                Cijena = article.PlanskaCijena,
                JedinicaMjere = article.JedinicaMjere,
                PoreskaStopa = 2
            };

            ApplyItemValues(row, price, quantity, discount);
            StockInItems.Add(row);
            HasUnsavedChanges = true;
        }

        public void UpdateStockInItem(TblUlazRepromaterijalStavka row,
            decimal price, decimal quantity, decimal discount)
        {
            if (!CanEdit || !StockInItems.Contains(row) ||
                !ValidateItem(price, quantity, discount)) return;

            ApplyItemValues(row, price, quantity, discount);
            CollectionViewSource.GetDefaultView(StockInItems).Refresh();
            OnPropertyChanged(nameof(IznosRacuna));
            HasUnsavedChanges = true;
        }

        private static void ApplyItemValues(TblUlazRepromaterijalStavka row,
            decimal price, decimal quantity, decimal discount)
        {
            row.Kolicina = quantity;
            row.Rabat = discount;
            row.CijenaBezUPDV = price;
            row.IznosBezUPDV = quantity * price;
            row.CijenaSaUPDV = price * 1.17m;
            row.IznosUPDVa = quantity * (row.CijenaSaUPDV * 0.14529m);
        }

        private bool ValidateItem(decimal price, decimal quantity, decimal discount)
        {
            if (price >= 0 && quantity > 0 && discount >= 0 && discount <= 100)
                return true;

            OnErrorOccurred("Količina mora biti pozitivna, cijena nenegativna, a rabat između 0 i 100%.");
            return false;
        }

        private void DeleteStockInItem(TblUlazRepromaterijalStavka? row)
        {
            if (row == null || !CanEdit || !StockInItems.Contains(row)) return;
            if (ShowDeletePopupRequested?.Invoke(row.Artikl ?? "") != true) return;
            StockInItems.Remove(row);
            HasUnsavedChanges = true;
        }



        public async Task SaveSelectedStockInAsync()
        {
            if (SelectedStockIn == null || IsBusy) return;
            var edited = SelectedStockIn;

            if (string.IsNullOrWhiteSpace(edited.Dobavljac) ||
                string.IsNullOrWhiteSpace(edited.BrojFakture))
            {
                OnErrorOccurred("Dobavljač i broj fakture su obavezni.");
                return;
            }

            if (StockInItems.Any(x => x.Kolicina <= 0))
            {
                OnErrorOccurred("Sve stavke moraju imati količinu veću od nule.");
                return;
            }

            IsBusy = true;
            bool ulazSpremljen = false;
            try
            {
                using var db = new AppDbContext();
                await using var transaction = await db.Database.BeginTransactionAsync();
                TblUlazRepromaterijal? existing = null;

                if (edited.IdUlaza != 0)
                {
                    existing = await db.UlazRepromaterijal
                        .FirstOrDefaultAsync(x => x.IdUlaza == edited.IdUlaza);

                    if (existing == null)
                        throw new InvalidOperationException("Ulaz više ne postoji u bazi.");

                    if (existing.BrojUlaza != _originalBrojUlaza)
                        throw new InvalidOperationException(
                            "Broj ulaza je promijenjen izvan ove stranice.");
                }

                DateTime? prethodniDatum = existing?.Datum.Date;
                int broj;

                if (existing == null)
                {
                    broj = (await db.UlazRepromaterijal.MaxAsync(
                        x => (int?)x.BrojUlaza) ?? 0) + 1;

                    existing = new TblUlazRepromaterijal
                    {
                        BrojUlaza = broj,
                        Radnik = edited.Radnik,
                        RadnikName = edited.RadnikName
                    };

                    db.UlazRepromaterijal.Add(existing);
                }
                else broj = existing.BrojUlaza;

                existing.Datum = edited.Datum;
                existing.Dobavljac = edited.Dobavljac;
                existing.BrojFakture = edited.BrojFakture;
                existing.Locked = edited.Locked;

                var saved = await db.UlazRepromaterijalStavka
                    .Where(x => x.BrojUlaza == broj).ToListAsync();

                var editedIds = StockInItems.Where(x => x.RedniBroj > 0)
                    .Select(x => x.RedniBroj).ToHashSet();

                foreach (var old in saved.Where(x => !editedIds.Contains(x.RedniBroj)))
                    db.UlazRepromaterijalStavka.Remove(old);

                foreach (var item in StockInItems)
                {
                    TblUlazRepromaterijalStavka target;

                    if (item.RedniBroj > 0)
                    {
                        target = saved.FirstOrDefault(x => x.RedniBroj == item.RedniBroj)
                            ?? throw new InvalidOperationException(
                                $"Stavka {item.RedniBroj} više ne postoji.");
                    }
                    else
                    {
                        target = new TblUlazRepromaterijalStavka();
                        db.UlazRepromaterijalStavka.Add(target);
                    }

                    target.BrojUlaza = broj;
                    target.Artikl = item.Artikl;
                    target.Kolicina = item.Kolicina;
                    target.Cijena = item.Cijena;
                    target.JedinicaMjere = item.JedinicaMjere;
                    target.CijenaBezUPDV = item.CijenaBezUPDV;
                    target.PoreskaStopa = item.PoreskaStopa;
                    target.IznosBezUPDV = item.IznosBezUPDV;
                    target.Rabat = item.Rabat;
                    target.IznosUPDVa = item.IznosUPDVa;
                    target.CijenaSaUPDV = item.CijenaSaUPDV;
                }

                DateTime datumPromjene = edited.Datum.Date;
                if (prethodniDatum.HasValue && prethodniDatum.Value < datumPromjene)
                    datumPromjene = prethodniDatum.Value;

                var kontrola = await db.KnjigaKuhinjeKontrola
                    .FirstOrDefaultAsync(x => x.Id == 1)
                    ?? throw new InvalidOperationException(
                        "Nedostaje kontrolni red Knjige kuhinje (Id=1).");

                if (!kontrola.DatumPromjeneStanja.HasValue ||
                    datumPromjene < kontrola.DatumPromjeneStanja.Value.Date)
                    kontrola.DatumPromjeneStanja = datumPromjene;

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                ulazSpremljen = true;

                using (var knjigaDb = new AppDbContext())
                {
                    var knjigaService = new KnjigaKuhinjeService(knjigaDb);
                    await knjigaService.SinhronizujDoDanasAsync();
                }

                HasUnsavedChanges = false;
                await LoadStockInCoreAsync(broj);
                InformationOccurred?.Invoke(this, "Ulaz hrane je uspješno sačuvan.");
            }
            catch (Exception ex)
            {
                if (ulazSpremljen)
                    ReportError("Ulaz hrane je sačuvan, ali sinhronizacija Knjige kuhinje ili ponovno učitavanje nije uspjelo.", ex);
                else
                    ReportError("Spremanje ulaza hrane nije uspjelo. Transakcija nije potvrđena.", ex);
            }
            finally { IsBusy = false; }
        }



        private async Task DiscardChangesAsync()
        {
            if (SelectedStockIn == null || !CanLeaveCurrent()) return;
            int broj = SelectedStockIn.IdUlaza == 0 ? 0 : SelectedStockIn.BrojUlaza;

            IsBusy = true;
            try { await LoadStockInCoreAsync(broj); }
            catch (Exception ex) { ReportError("Odbacivanje promjena nije uspjelo.", ex); }
            finally { IsBusy = false; }
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

        protected virtual void OnErrorOccurred(string? message) =>
            ErrorOccurred?.Invoke(this, message);

        protected virtual void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void NotifyCommands()
        {
            AddNewStockInCommand.NotifyCanExecuteChanged();
            SaveStockInCommand.NotifyCanExecuteChanged();
            DeleteArticleCommand.NotifyCanExecuteChanged();
            FirstStockInCommand.NotifyCanExecuteChanged();
            PreviousStockInCommand.NotifyCanExecuteChanged();
            NextStockInCommand.NotifyCanExecuteChanged();
            LastStockInCommand.NotifyCanExecuteChanged();
            AddArticleCommand.NotifyCanExecuteChanged();
            EditStockInItemCommand.NotifyCanExecuteChanged();
            PrintStockInCommand.NotifyCanExecuteChanged();
            DiscardChangesCommand.NotifyCanExecuteChanged();
        }

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
            sredinaPar.Inlines.Add(new Run($"KALKULACIJA CIJENA: {ulaz.BrojUlaza}/{DateTime.Now:yy}\n")
            {
                FontSize = 20,
                FontWeight = FontWeights.Normal
            });
            sredinaPar.Inlines.Add(new Run(Environment.NewLine) { FontSize = 10 });
            sredinaPar.Inlines.Add(new Run($"Datum: {ulaz.Datum:dd.MM.yyyy}\n") { FontSize = 15 });
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
                "#", "Naziv artikla", "JM", "Količina",
                "Fakturna cijena po jedinici mjere bez PDV-a",
                "Fakturna vrijednost bez PDV-a",
                "Zavisni troškovi bez PDV",
                "Nabavna cijena po jedinici mjere bez PDV",
                "Nabavna vrijednost bez PDV-a",
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
                r.Cells.Add(CreateCell(s.JedinicaMjereName ?? "", TextAlignment.Center));
                r.Cells.Add(CreateCell(s.Kolicina.ToString("F2"), TextAlignment.Right));
                r.Cells.Add(CreateCell(s.CijenaBezUPDV?.ToString("F2") ?? "", TextAlignment.Right));
                r.Cells.Add(CreateCell(s.FakVrBezPDV?.ToString("F2") ?? "", TextAlignment.Right));
                r.Cells.Add(CreateCell("0.00", TextAlignment.Right));
                r.Cells.Add(CreateCell(s.NabavnaCijenaBezPDV?.ToString("F2") ?? "", TextAlignment.Right));
                r.Cells.Add(CreateCell(s.NabavnaVrijednostBezPDV?.ToString("F2") ?? "", TextAlignment.Right));
                r.Cells.Add(CreateCell(s.MarzaPostotak?.ToString("F2") ?? "", TextAlignment.Right));
                r.Cells.Add(CreateCell(s.RazlikaCijBezPDV?.ToString("F2") ?? "", TextAlignment.Right));
                r.Cells.Add(CreateCell(s.ProdajnaVrijBezIPDV?.ToString("F2") ?? "", TextAlignment.Right));
                r.Cells.Add(CreateCell(s.PoreskaStopaPostotak?.ToString("F2") ?? "", TextAlignment.Center));
                r.Cells.Add(CreateCell(s.IznosIPDV?.ToString("F2") ?? "", TextAlignment.Right));
                r.Cells.Add(CreateCell(s.MPVrijednost?.ToString("F2") ?? "", TextAlignment.Right));
                r.Cells.Add(CreateCell(s.Cijena?.ToString("F2") ?? "", TextAlignment.Right));

                sumFakVr += s.FakVrBezPDV ?? 0m;
                sumNabavna += s.NabavnaVrijednostBezPDV ?? 0m;
                sumRabat += s.Rabat ?? 0m;
                sumProdVr += s.ProdajnaVrijBezIPDV ?? 0m;
                sumIznosPDV += s.IznosIPDV ?? 0m;
                sumMP += s.MPVrijednost ?? 0m;
                index++;
            }

            TableRow sumRow = new TableRow();
            bodyGroup.Rows.Add(sumRow);

            string[] ukupno =
            {
                "", "UKUPNO", "", "", "",
                sumFakVr.ToString("F2"),
                sumZavisni.ToString("F2"),
                sumRabat.ToString("F2"),
                sumNabavna.ToString("F2"),
                "", "",
                sumProdVr.ToString("F2"),
                "",
                sumIznosPDV.ToString("F2"),
                sumMP.ToString("F2"),
                ""
            };

            for (int i = 0; i < ukupno.Length; i++)
                sumRow.Cells.Add(CreateCell(
                    ukupno[i], i == 1 ? TextAlignment.Left : TextAlignment.Right));

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
            sumaPar.Inlines.Add(
                $"UKUPNO ZA PLAĆANJE: {(sumNabavna + sumIznosPDV):F2}");
            footerRow.Cells.Add(new TableCell(sumaPar));

            Paragraph potpisPar = new Paragraph();
            potpisPar.Inlines.Add(
                "                                     Kalkulaciju sačinio: " + Environment.NewLine);
            potpisPar.Inlines.Add(
                "M.P.                                                          " + Environment.NewLine);
            potpisPar.Inlines.Add(
                "                                     ____________________");
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

                    if (pdfPrinter != null) printDlg.PrintQueue = pdfPrinter;
                }
                else printDlg.PrintQueue = defaultQueue;

                if (printDlg.ShowDialog() == true)
                    printDlg.PrintDocument(
                        ((IDocumentPaginatorSource)doc).DocumentPaginator,
                        "Kalkulacija cijena");
            }
            catch (PrintQueueException ex)
            {
                MessageBox.Show(
                    "Ne može se otvoriti dijalog za štampu:\n" + ex.Message);
            }
        }

        private TableCell CreateCell(string text, TextAlignment alignment) =>
            new TableCell(new Paragraph(new Run(text)))
            {
                TextAlignment = alignment,
                Padding = new Thickness(5),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5)
            };
    }
}
