using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Fiscal.Common;
using Caupo.Helpers;
using Caupo.Models;
using Caupo.Properties;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class KasaViewModel : INotifyPropertyChanged
    {
        #region PRIVATE FIELDS

        private bool _initialized;

        // Svi artikli i kategorije se iz baze učitavaju samo jednom.
        private List<TblArtikli> _sviArtikli = [];
        private List<TblKategorije> _sveKategorije = [];

        private readonly ObservableCollection<TblArtikli> _prikazaniArtikli = [];
        private readonly ObservableCollection<TblKategorije> _prikazaneKategorije = [];

        private int? _selectedCategoryId;
        private string? _firstLetterFilter;

        private string? _imagePathPiceButton;
        private string? _imagePathHranaButton;
        private string? _imagePathOstaloButton;

        private string? _imagePathPiceSelectedButton;
        private string? _imagePathHranaSelectedButton;
        private string? _imagePathOstaloSelectedButton;

        private string? _imagePathCategoryButton;

        private decimal _totalSum;

        private ObservableCollection<RacunStavka> _stavkeRacuna = [];

        private RacunStavka? _selectedStavka;

        private ObservableCollection<TblKupci> _kupci = [];
        private TblKupci? _selectedKupac;

        private bool _isMultiUser;
        private bool _isLoggedIn;

        private Category _selectedCategory = Category.Pice;

        #endregion

        #region APPEARANCE

        public string? ImagePathPiceButton
        {
            get => _imagePathPiceButton;
            set
            {
                if (_imagePathPiceButton == value)
                    return;

                _imagePathPiceButton = value;
                OnPropertyChanged(nameof(ImagePathPiceButton));
            }
        }

        public string? ImagePathHranaButton
        {
            get => _imagePathHranaButton;
            set
            {
                if (_imagePathHranaButton == value)
                    return;

                _imagePathHranaButton = value;
                OnPropertyChanged(nameof(ImagePathHranaButton));
            }
        }

        public string? ImagePathOstaloButton
        {
            get => _imagePathOstaloButton;
            set
            {
                if (_imagePathOstaloButton == value)
                    return;

                _imagePathOstaloButton = value;
                OnPropertyChanged(nameof(ImagePathOstaloButton));
            }
        }

        public string? ImagePathPiceSelectedButton
        {
            get => _imagePathPiceSelectedButton;
            set
            {
                if (_imagePathPiceSelectedButton == value)
                    return;

                _imagePathPiceSelectedButton = value;
                OnPropertyChanged(nameof(ImagePathPiceSelectedButton));
            }
        }

        public string? ImagePathHranaSelectedButton
        {
            get => _imagePathHranaSelectedButton;
            set
            {
                if (_imagePathHranaSelectedButton == value)
                    return;

                _imagePathHranaSelectedButton = value;
                OnPropertyChanged(nameof(ImagePathHranaSelectedButton));
            }
        }

        public string? ImagePathOstaloSelectedButton
        {
            get => _imagePathOstaloSelectedButton;
            set
            {
                if (_imagePathOstaloSelectedButton == value)
                    return;

                _imagePathOstaloSelectedButton = value;
                OnPropertyChanged(nameof(ImagePathOstaloSelectedButton));
            }
        }

        public string? ImagePathCategoryButton
        {
            get => _imagePathCategoryButton;
            set
            {
                if (_imagePathCategoryButton == value)
                    return;

                _imagePathCategoryButton = value;
                OnPropertyChanged(nameof(ImagePathCategoryButton));
            }
        }

        #endregion

        #region ARTIKLI

        public ObservableCollection<TblArtikli> PrikazaniArtikli => _prikazaniArtikli;

        public ObservableCollection<TblKategorije> PrikazaneKategorije => _prikazaneKategorije;

        #endregion

        #region CATEGORY

        public enum Category
        {
            Pice = 0,
            Hrana = 1,
            Ostalo = 2
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory == value)
                    return;

                _selectedCategory = value;
                _selectedCategoryId = null;
                _firstLetterFilter = null;

                OnPropertyChanged(nameof(SelectedCategory));
                OnPropertyChanged(nameof(SelectedCategoryId));

                RefreshCategories();
                RefreshArticles();
            }
        }

        public int? SelectedCategoryId
        {
            get => _selectedCategoryId;
            set
            {
                if (_selectedCategoryId == value)
                    return;

                _selectedCategoryId = value;

                OnPropertyChanged(nameof(SelectedCategoryId));

                RefreshArticles();
            }
        }

        #endregion

        #region RACUN

        public decimal TotalSum
        {
            get => _totalSum;
            private set
            {
                if (_totalSum == value)
                    return;

                _totalSum = value;
                OnPropertyChanged(nameof(TotalSum));
            }
        }

        public ObservableCollection<RacunStavka> StavkeRacuna
        {
            get => _stavkeRacuna;
            set
            {
                if (ReferenceEquals(_stavkeRacuna, value))
                    return;

                if (_stavkeRacuna != null)
                    _stavkeRacuna.CollectionChanged -= StavkeRacuna_CollectionChanged;

                _stavkeRacuna = value ?? [];

                _stavkeRacuna.CollectionChanged += StavkeRacuna_CollectionChanged;

                OnPropertyChanged(nameof(StavkeRacuna));

                UpdateTotalSum();
            }
        }

        public RacunStavka? SelectedStavka
        {
            get => _selectedStavka;
            set
            {
                if (ReferenceEquals(_selectedStavka, value))
                    return;

                _selectedStavka = value;
                OnPropertyChanged(nameof(SelectedStavka));
            }
        }

        private void StavkeRacuna_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateTotalSum();
        }

        #endregion

        #region KUPCI

        public ObservableCollection<TblKupci> Kupci
        {
            get => _kupci;
            private set
            {
                _kupci = value;
                OnPropertyChanged(nameof(Kupci));
            }
        }

        public TblKupci? SelectedKupac
        {
            get => _selectedKupac;
            set
            {
                if (ReferenceEquals(_selectedKupac, value))
                    return;

                _selectedKupac = value;

                OnPropertyChanged(nameof(SelectedKupac));

                Debug.WriteLine($"[KASA] SelectedKupac = {_selectedKupac?.Kupac ?? "(nema)"}");
            }
        }

        #endregion

        #region MULTI USER

        public bool IsMultiUser
        {
            get => _isMultiUser;
            set
            {
                if (_isMultiUser == value)
                    return;

                _isMultiUser = value;

                OnPropertyChanged(nameof(IsMultiUser));
                OnPropertyChanged(nameof(IsMultiUserVisible));

                UpdateMultiUserState();
            }
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                if (_isLoggedIn == value)
                    return;

                _isLoggedIn = value;

                OnPropertyChanged(nameof(IsLoggedIn));
                OnPropertyChanged(nameof(IsMultiUserVisible));

                UpdateMultiUserState();
            }
        }

        public bool IsMultiUserVisible =>
            IsMultiUser && !IsLoggedIn;

        public int pokusaj { get; set; } = 3;

        public event Action<bool>? MultiUserVisibilityChanged;

        private void UpdateMultiUserState()
        {
            if (IsMultiUserVisible)
            {
                pokusaj = 3;

                Debug.WriteLine("[KASA] Multi-user login aktivan. Pokušaji resetovani na 3.");
            }

            MultiUserVisibilityChanged?.Invoke(IsMultiUserVisible);
        }

        #endregion

        public sealed class KasaFiscalResult
        {
            public FiscalResult Result { get; init; } = null!;
            public string? Warning { get; init; }
        }

        #region CONSTRUCTOR

        public KasaViewModel()
        {
            _stavkeRacuna.CollectionChanged += StavkeRacuna_CollectionChanged;
        }

        #endregion

        #region INITIALIZATION

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _initialized = true;

            try
            {
                await SetImage();
                await CheckMultiUserAsync();

                await LoadArtikliAsync();
                await LoadCategoriesAsync();
                await LoadKupciAsync();

                Debug.WriteLine($"[KASA] Initialize završen. Artikli={_sviArtikli.Count}, Kategorije={_sveKategorije.Count}, Kupci={Kupci.Count}");
            }
            catch (Exception ex)
            {
                _initialized = false;

                Debug.WriteLine("[KASA] InitializeAsync GREŠKA: " + ex);
            }
        }

        // Ostavljeno radi kompatibilnosti ako se negdje još poziva Start().
        public Task Start()
        {
            return InitializeAsync();
        }

        public async Task ReloadAsync()
        {
            await LoadArtikliAsync();
            await LoadCategoriesAsync();
            await LoadKupciAsync();
        }

        #endregion

        #region SETTINGS / THEME

        public Task CheckMultiUser()
        {
            return CheckMultiUserAsync();
        }

        public Task CheckMultiUserAsync()
        {
            try
            {
                string? multiUser = Settings.Default.MultiUser;

                IsMultiUser = string.Equals(multiUser, "DA", StringComparison.OrdinalIgnoreCase);

                if (!IsMultiUser)
                    IsLoggedIn = true;

                Debug.WriteLine($"[KASA] MultiUser = {IsMultiUser}");
            }
            catch (Exception ex)
            {
                IsMultiUser = false;
                IsLoggedIn = true;

                Debug.WriteLine("[KASA] CheckMultiUserAsync: " + ex);
            }

            return Task.CompletedTask;
        }

        public Task SetImage()
        {
            string? tema = Settings.Default.Tema;

            bool tamna = string.Equals(tema, "Tamna", StringComparison.OrdinalIgnoreCase);

            if (tamna)
            {
                ImagePathPiceButton = "pack://application:,,,/Images/Dark/drink.svg";

                ImagePathHranaButton = "pack://application:,,,/Images/Dark/food.svg";

                ImagePathOstaloButton = "pack://application:,,,/Images/Dark/another.svg";

                ImagePathPiceSelectedButton = "pack://application:,,,/Images/Light/drink.svg";

                ImagePathHranaSelectedButton = "pack://application:,,,/Images/Light/food.svg";

                ImagePathOstaloSelectedButton = "pack://application:,,,/Images/Light/another.svg";

                ImagePathCategoryButton = "pack://application:,,,/Images/Dark/category.svg";
            }
            else
            {
                ImagePathPiceButton = "pack://application:,,,/Images/Light/drink.svg";

                ImagePathHranaButton = "pack://application:,,,/Images/Light/food.svg";

                ImagePathOstaloButton = "pack://application:,,,/Images/Light/another.svg";

                ImagePathPiceSelectedButton = "pack://application:,,,/Images/Dark/drink.svg";

                ImagePathHranaSelectedButton = "pack://application:,,,/Images/Dark/food.svg";

                ImagePathOstaloSelectedButton = "pack://application:,,,/Images/Dark/another.svg";

                ImagePathCategoryButton = "pack://application:,,,/Images/Light/category.svg";
            }

            return Task.CompletedTask;
        }

        #endregion

        #region LOAD DATA

        public async Task LoadArtikliAsync()
        {
            try
            {
                await using var db = new AppDbContext();

                _sviArtikli = await db.Artikli
                    .AsNoTracking()
                    .Where(a => a.Aktivan)
                    .OrderBy(a => a.VrstaArtikla)
                    .ThenBy(a => a.Pozicija)
                    .ToListAsync();

                RefreshArticles();

                Debug.WriteLine($"[KASA] Artikli učitani: {_sviArtikli.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KASA] LoadArtikliAsync: " + ex);
            }
        }

        public async Task LoadCategoriesAsync()
        {
            try
            {
                await using var db = new AppDbContext();

                _sveKategorije = await db.Kategorije
                    .AsNoTracking()
                    .OrderBy(k => k.VrstaArtikla)
                    .ThenBy(k => k.IdKategorije)
                    .ToListAsync();

                RefreshCategories();

                Debug.WriteLine($"[KASA] Kategorije učitane: {_sveKategorije.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KASA] LoadCategoriesAsync: " + ex);
            }
        }

        public async Task LoadKupciAsync()
        {
            try
            {
                await using var db = new AppDbContext();

                var kupci = await db.Kupci
                    .AsNoTracking()
                    .Where(k => k.Kupac != null && k.Kupac != "")
                    .OrderBy(k => k.Kupac)
                    .ToListAsync();

                Kupci = new ObservableCollection<TblKupci>(kupci);

                // Standardni POS kupac.
                SelectedKupac =
                    Kupci.FirstOrDefault(k =>
                        string.Equals(k.Kupac?.Trim(), "Gradjani", StringComparison.OrdinalIgnoreCase)) ?? Kupci.FirstOrDefault();

                Debug.WriteLine($"[KASA] Kupci učitani: {Kupci.Count}");

                Debug.WriteLine($"[KASA] Default kupac: {SelectedKupac?.Kupac ?? "(nema)"}");
            }
            catch (Exception ex)
            {
                SelectedKupac = null;

                Debug.WriteLine("[KASA] LoadKupciAsync: " + ex);
            }
        }

        #endregion

        #region FILTER

        private void RefreshArticles()
        {
            IEnumerable<TblArtikli> query = _sviArtikli.Where(a => a.VrstaArtikla == (int)SelectedCategory);

            if (_selectedCategoryId.HasValue)
                query = query.Where(a => a.Kategorija == _selectedCategoryId.Value);

            if (!string.IsNullOrWhiteSpace(_firstLetterFilter))
            {
                string slovo = _firstLetterFilter.Trim();

                query = query.Where(a =>
                {
                    string naziv = a.VrstaArtikla == 0 ? a.ArtiklNormativ ?? a.Artikl ?? "" : a.Artikl ?? a.ArtiklNormativ ?? "";
                    return naziv.StartsWith(slovo, StringComparison.CurrentCultureIgnoreCase);
                });
            }

            var noviArtikli = query.OrderBy(a => a.Pozicija).ToList();

            PrikazaniArtikli.Clear();

            foreach (var artikl in noviArtikli)
                PrikazaniArtikli.Add(artikl);
        }

        private void RefreshCategories()
        {
            var noveKategorije = _sveKategorije.Where(k => k.VrstaArtikla == (int)SelectedCategory).OrderBy(k => k.IdKategorije).ToList();

            PrikazaneKategorije.Clear();

            foreach (var kategorija in noveKategorije)
                PrikazaneKategorije.Add(kategorija);
        }

        public void FilterByCategory(int categoryId)
        {
            _firstLetterFilter = null;

            SelectedCategoryId = categoryId;
        }

        public void FilterByFirstLetter(string firstLetter)
        {
            if (string.IsNullOrWhiteSpace(firstLetter))
                return;

            _selectedCategoryId = null;

            _firstLetterFilter = firstLetter.Trim();

            OnPropertyChanged(nameof(SelectedCategoryId));

            RefreshArticles();
        }

        public void ArtikliFilterReset()
        {
            _selectedCategoryId = null;
            _firstLetterFilter = null;

            OnPropertyChanged(nameof(SelectedCategoryId));

            RefreshArticles();
        }

        #endregion

        #region RACUN OPERATIONS

        public void DodajArtikl(TblArtikli artikl, decimal kolicina)
        {
            if (artikl == null || kolicina <= 0)
                return;

            var postojecaStavka = NadjiStavkuZaPovecanje(artikl.Sifra);

            if (postojecaStavka != null)
            {
                UpdateStavkuRacunaPlus(postojecaStavka, kolicina);
                return;
            }

            var stavka = new RacunStavka
            {
                ArtiklId = artikl.IdArtikla,
                Name = artikl.Artikl,
                Sifra = artikl.Sifra,
                Naziv = artikl.ArtiklNormativ,
                UnitPrice = artikl.Cijena,
                Proizvod = artikl.VrstaArtikla,
                JedinicaMjere = artikl.JedinicaMjere,
                Quantity = kolicina,
                PoreskaStopa = artikl.PoreskaStopa,
                PorezNaPotrosnju = artikl.PorezNaPotrosnju
            };

            DodajStavkuRacuna(stavka);
        }

        public void DodajStavkuRacuna(RacunStavka stavka)
        {
            if (stavka == null)
                return;

            StavkeRacuna.Add(stavka);
        }

        public RacunStavka? NadjiStavkuZaPovecanje(string sifra)
        {
            if (string.IsNullOrWhiteSpace(sifra))
                return null;

            return StavkeRacuna.LastOrDefault(item => item.Sifra == sifra && string.IsNullOrWhiteSpace(item.Note));
        }

        public bool StavkaPostoji(string sifra)
        {
            if (string.IsNullOrWhiteSpace(sifra))
                return false;

            return StavkeRacuna.Any(item => item.Sifra == sifra);
        }

        public void UpdateTotalSum()
        {
            TotalSum = Math.Round(StavkeRacuna.Sum(item => item.TotalAmount ?? 0m), 2, MidpointRounding.AwayFromZero);
        }

        public void UpdateStavkuRacunaPlus(RacunStavka stavka, decimal kolicina)
        {
            if (stavka == null || kolicina <= 0)
                return;

            stavka.Quantity = (stavka.Quantity ?? 0m) + kolicina;
            UpdateTotalSum();
        }

        public void UpdateStavkuRacunaMinus(RacunStavka stavka, decimal kolicina)
        {
            if (stavka == null || kolicina <= 0)
                return;

            decimal novaKolicina = (stavka.Quantity ?? 0m) - kolicina;

            if (novaKolicina <= 0)
            {
                StavkeRacuna.Remove(stavka);

                if (ReferenceEquals(SelectedStavka, stavka))
                    SelectedStavka = null;
            }
            else
            {
                stavka.Quantity = novaKolicina;
                UpdateTotalSum();
            }
        }

        public void ClearRacun()
        {
            StavkeRacuna.Clear();
            SelectedStavka = null;
        }

        #endregion

        public async Task<TblRadnici?> PrijaviRadnikaAsync(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
                return null;

            await using var db = new AppDbContext();
            var radnik = await db.Radnici.AsNoTracking().FirstOrDefaultAsync(r => r.Lozinka == pin.Trim());

            if (radnik != null)
                IsLoggedIn = true;

            return radnik;
        }

        public async Task<KasaFiscalResult> FiskalizujAsync(int nacinPlacanja)
        {
            if (StavkeRacuna.Count == 0)
                throw new InvalidOperationException("Račun nema stavki.");

            var stavkeZaFiskalizaciju = StavkeRacuna.ToList();
            decimal ukupnoZaFiskalizaciju = Math.Round(stavkeZaFiskalizaciju.Sum(item => item.TotalAmount ?? 0m), 2, MidpointRounding.AwayFromZero);

            FiscalPaymentType paymentType = nacinPlacanja switch
            {
                0 => FiscalPaymentType.Cash,
                1 => FiscalPaymentType.Card,
                2 => FiscalPaymentType.Check,
                3 => FiscalPaymentType.WireTransfer,
                _ => throw new FiscalException("Odaberite ispravan način plaćanja.")
            };

            FiscalBuyer? buyer = null;

            if (SelectedKupac != null)
            {
                buyer = new FiscalBuyer
                {
                    Name = SelectedKupac.Kupac,
                    TaxId = SelectedKupac.JIB,
                    Address = SelectedKupac.Adresa,
                    City = SelectedKupac.Mjesto
                };
            }

            var fiscalRequest = new FiscalRequest
            {
                Items = stavkeZaFiskalizaciju,
                Buyer = buyer,
                Cashier = new FiscalCashier
                {
                    Id = Globals.ulogovaniKorisnik.IdRadnika,
                    Name = Globals.ulogovaniKorisnik.Radnik,
                    IdentificationNumber = Globals.ulogovaniKorisnik.IB
                },
                PaymentType = paymentType,
                TotalAmount = ukupnoZaFiskalizaciju,
                InvoiceType = "Normal",
                TransactionType = "Sale"
            };

            IFiscalService fiscalService = FiscalServiceFactory.Create(Settings.Default.Country);
            FiscalResult result = await fiscalService.IzdajRacunAsync(fiscalRequest);

            Debug.WriteLine($"[FISKALNI] Success={result.Success}, Fiscalized={result.Fiscalized}, Saved={result.SavedToDatabase}, Printed={result.Printed}, FiscalNumber={result.FiscalNumber}");

            if (result.FiscalizationStatus != FiscalizationStatus.Unknown && (result.Success || result.Fiscalized))
            {
                ClearRacun();

                if (result.SavedToDatabase)
                {
                    try
                    {
                        await PrintajBlokoveAsync(stavkeZaFiskalizaciju);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("[FISKALNI] Greška pri printanju blokova: " + ex);
                        string blokWarning = $"Račun je izdan, ali blok za kuhinju/šank nije isprintan: {ex.Message}";
                        string? warning = BuildFiscalWarning(result);
                        warning = string.IsNullOrWhiteSpace(warning) ? blokWarning : warning + Environment.NewLine + blokWarning;

                        return new KasaFiscalResult
                        {
                            Result = result,
                            Warning = warning
                        };
                    }
                }
            }

            return new KasaFiscalResult
            {
                Result = result,
                Warning = BuildFiscalWarning(result)
            };
        }

        private static async Task PrintajBlokoveAsync(IEnumerable<RacunStavka> stavke)
        {
            var sankStavke = stavke.Where(s => s.Proizvod == 0 && s.Printed != "DA").ToList();
            var kuhinjaStavke = stavke.Where(s => s.Proizvod == 1 && s.Printed != "DA").ToList();

            if (kuhinjaStavke.Any())
            {
                var printer = new BlokPrinter(kuhinjaStavke, "Kuhinja", "Kasa", "Kasa");
                await printer.Print();
            }

            if (sankStavke.Any())
            {
                var printer = new BlokPrinter(sankStavke, "Sank", "Kasa", "Kasa");
                await printer.Print();
            }
        }

        private static string? BuildFiscalWarning(FiscalResult result)
        {
            var warnings = new List<string>();

            if (!result.Fiscalized)
                warnings.Add("Račun je lokalno obrađen, ali fiskalizacija nije potvrđena.");

            if (!result.SavedToDatabase)
                warnings.Add("Račun nije spremljen u lokalnu bazu.");

            if (!result.Printed)
                warnings.Add("Račun nije isprintan.");

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                warnings.Add(result.ErrorMessage);

            return warnings.Count == 0 ? null : string.Join(Environment.NewLine, warnings.Distinct());
        }

        #region ARTICLE POSITION

        // Ostavljeno radi kompatibilnosti.
        public async Task UpdateArticlePosition(TblArtikli artikl)
        {
            if (artikl == null)
                return;

            try
            {
                await using var db = new AppDbContext();
                var existing = await db.Artikli.FindAsync(artikl.IdArtikla);

                if (existing == null)
                    return;

                existing.Pozicija = artikl.Pozicija;
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KASA] UpdateArticlePosition: " + ex);
            }
        }

        public async Task SwapArticlePositionsAsync(TblArtikli first, TblArtikli second)
        {
            if (first == null || second == null || first.IdArtikla == second.IdArtikla)
                return;

            try
            {
                int? firstPosition = first.Pozicija;

                int? secondPosition = second.Pozicija;

                await using var db = new AppDbContext();

                var firstDb = await db.Artikli.FindAsync(first.IdArtikla);

                var secondDb = await db.Artikli.FindAsync(second.IdArtikla);

                if (firstDb == null || secondDb == null)
                    return;

                firstDb.Pozicija = secondPosition;

                secondDb.Pozicija = firstPosition;

                await db.SaveChangesAsync();

                first.Pozicija = secondPosition;

                second.Pozicija = firstPosition;

                RefreshArticles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KASA] SwapArticlePositionsAsync: " + ex);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
