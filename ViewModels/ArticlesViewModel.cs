using Caupo.Data;
using Caupo.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public enum ArticleActiveFilter
    {
        Svi,
        Aktivni,
        Neaktivni
    }

    public sealed class ArticleCategoryFilter
    {
        public int? CategoryId { get; }
        public string Name { get; }

        public ArticleCategoryFilter(int? categoryId, string name)
        {
            CategoryId = categoryId;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public sealed class ArticleTaxFilter
    {
        public int? TaxId { get; }
        public string Name { get; }

        public ArticleTaxFilter(int? taxId, string name)
        {
            TaxId = taxId;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ArticlesViewModel : INotifyPropertyChanged
    {
        public event EventHandler<string>? ErrorOccurred;

        private void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, message);
        }

        private readonly HashSet<int> _selectedArticleIds = new HashSet<int>();

        public int BrojOznacenihArtikala => _selectedArticleIds.Count;
        public ObservableCollection<TblNormativPica> Normativi { get; set; } = new ObservableCollection<TblNormativPica>();
        public ObservableCollection<TblKategorije> Kategorije { get; set; } = new ObservableCollection<TblKategorije>();
        public ObservableCollection<TblPoreskeStope> PoreskeStope { get; set; } = new ObservableCollection<TblPoreskeStope>();
        public ObservableCollection<TblJediniceMjere> JediniceMjere { get; set; } = new ObservableCollection<TblJediniceMjere>();
        public ObservableCollection<string> VrstaArtikla { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<ArticleActiveFilter> ActiveFilters { get; } = new ObservableCollection<ArticleActiveFilter>
        {
            ArticleActiveFilter.Svi,
            ArticleActiveFilter.Aktivni,
            ArticleActiveFilter.Neaktivni
        };

        public ObservableCollection<string> ArticleTypeFilters { get; } = new ObservableCollection<string>
        {
            "Sve vrste",
            "Piće",
            "Hrana",
            "Ostalo"
        };

        public ObservableCollection<ArticleCategoryFilter> CategoryFilters { get; } = new ObservableCollection<ArticleCategoryFilter>();
        public ObservableCollection<ArticleTaxFilter> TaxFilters { get; } = new ObservableCollection<ArticleTaxFilter>();

        public IRelayCommand FirstCommand { get; }
        public IRelayCommand PreviousCommand { get; }
        public IRelayCommand NextCommand { get; }
        public IRelayCommand LastCommand { get; }

        private TblJediniceMjere? _selectedJedinicaMjere;
        public TblJediniceMjere? SelectedJedinicaMjere
        {
            get => _selectedJedinicaMjere;
            set
            {
                if (_selectedJedinicaMjere != value)
                {
                    _selectedJedinicaMjere = value;
                    OnPropertyChanged(nameof(SelectedJedinicaMjere));
                }
            }
        }

        private TblKategorije? _selectedKategorija;
        public TblKategorije? SelectedKategorija
        {
            get => _selectedKategorija;
            set
            {
                if (_selectedKategorija != value)
                {
                    _selectedKategorija = value;
                    OnPropertyChanged(nameof(SelectedKategorija));
                }
            }
        }

        private TblPoreskeStope? _selectedPoreskaStopa;
        public TblPoreskeStope? SelectedPoreskaStopa
        {
            get => _selectedPoreskaStopa;
            set
            {
                if (_selectedPoreskaStopa != value)
                {
                    _selectedPoreskaStopa = value;
                    OnPropertyChanged(nameof(SelectedPoreskaStopa));
                }
            }
        }

        private TblNormativPica? _selectedNormativ;
        public TblNormativPica? SelectedNormativ
        {
            get => _selectedNormativ;
            set
            {
                if (_selectedNormativ != value)
                {
                    _selectedNormativ = value;
                    OnPropertyChanged(nameof(SelectedNormativ));
                }
            }
        }

        private string? _selectedVrstaArtikla;
        public string? SelectedVrstaArtikla
        {
            get => _selectedVrstaArtikla;
            set
            {
                if (_selectedVrstaArtikla != value)
                {
                    _selectedVrstaArtikla = value;
                    OnPropertyChanged(nameof(SelectedVrstaArtikla));
                }
            }
        }

        private ArticleActiveFilter _selectedActiveFilter = ArticleActiveFilter.Svi;
        public ArticleActiveFilter SelectedActiveFilter
        {
            get => _selectedActiveFilter;
            set
            {
                if (_selectedActiveFilter != value)
                {
                    _selectedActiveFilter = value;
                    OnPropertyChanged(nameof(SelectedActiveFilter));
                    ClearArticleSelection();
                    FilterItems();
                }
            }
        }

        private string _selectedArticleTypeFilter = "Sve vrste";
        public string SelectedArticleTypeFilter
        {
            get => _selectedArticleTypeFilter;
            set
            {
                if (_selectedArticleTypeFilter != value)
                {
                    _selectedArticleTypeFilter = value ?? "Sve vrste";
                    OnPropertyChanged(nameof(SelectedArticleTypeFilter));
                    OnPropertyChanged(nameof(IsCategoryFilterEnabled));
                    ClearArticleSelection();
                    UpdateCategoryFilters();
                    FilterItems();
                }
            }
        }

        private ArticleCategoryFilter? _selectedCategoryFilter;
        public ArticleCategoryFilter? SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                if (_selectedCategoryFilter != value)
                {
                    _selectedCategoryFilter = value;
                    OnPropertyChanged(nameof(SelectedCategoryFilter));
                    ClearArticleSelection();
                    FilterItems();
                }
            }
        }

        private ArticleTaxFilter? _selectedTaxFilter;
        public ArticleTaxFilter? SelectedTaxFilter
        {
            get => _selectedTaxFilter;
            set
            {
                if (_selectedTaxFilter != value)
                {
                    _selectedTaxFilter = value;
                    OnPropertyChanged(nameof(SelectedTaxFilter));
                    ClearArticleSelection();
                    FilterItems();
                }
            }
        }

        public bool IsCategoryFilterEnabled => SelectedArticleTypeFilter != "Sve vrste";

        private TblArtikli? _selectedArticle;
        public TblArtikli? SelectedArticle
        {
            get => _selectedArticle;
            set
            {
                if (_selectedArticle == value)
                    return;

                _selectedArticle = value;
                OnPropertyChanged(nameof(SelectedArticle));
                UpdateComboBoxes();
                NotifyNavigationCommands();
            }
        }

        private ObservableCollection<TblArtikli> _artikli = new ObservableCollection<TblArtikli>();
        public ObservableCollection<TblArtikli> Artikli
        {
            get => _artikli;
            set
            {
                _artikli = value;
                OnPropertyChanged(nameof(Artikli));
                OnPropertyChanged(nameof(BrojArtikala));
            }
        }

        private ObservableCollection<TblArtikli> _artikliFilter = new ObservableCollection<TblArtikli>();
        public ObservableCollection<TblArtikli> ArtikliFilter
        {
            get => _artikliFilter;
            set
            {
                _artikliFilter = value;
                OnPropertyChanged(nameof(ArtikliFilter));
                OnPropertyChanged(nameof(BrojPrikazanihArtikala));
                NotifyNavigationCommands();
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value ?? string.Empty;
                    OnPropertyChanged(nameof(SearchText));
                    ClearArticleSelection();
                    FilterItems();
                }
            }
        }

        private string? _novaSifra;
        public string? NovaSifra
        {
            get => _novaSifra;
            set
            {
                if (_novaSifra != value)
                {
                    _novaSifra = value;
                    OnPropertyChanged(nameof(NovaSifra));
                }
            }
        }

        public int BrojArtikala => Artikli.Count;
        public int BrojPrikazanihArtikala => ArtikliFilter.Count;

        public ArticlesViewModel()
        {
            FirstCommand = new RelayCommand(MoveFirst, CanMoveFirst);
            PreviousCommand = new RelayCommand(MovePrevious, CanMovePrevious);
            NextCommand = new RelayCommand(MoveNext, CanMoveNext);
            LastCommand = new RelayCommand(MoveLast, CanMoveLast);

            UpdateCategoryFilters();
            UpdateTaxFilters();

            _ = StartAsync();
        }

        private async Task StartAsync()
        {
            await LoadJediniceMjere();
            await LoadKategorije();
            await LoadNormativi();
            LoadVrsteArtikla();
            await LoadPoreskeStope();
            await LoadArticlesAsync();
        }

        public bool IsArticleChecked(TblArtikli? article)
        {
            return article != null && _selectedArticleIds.Contains(article.IdArtikla);
        }

        public void SetArticleChecked(TblArtikli? article, bool isChecked)
        {
            if (article == null)
                return;

            if (isChecked)
                _selectedArticleIds.Add(article.IdArtikla);
            else
                _selectedArticleIds.Remove(article.IdArtikla);

            OnPropertyChanged(nameof(BrojOznacenihArtikala));
        }

        public bool AreAllVisibleArticlesChecked()
        {
            return ArtikliFilter.Count > 0 && ArtikliFilter.All(a => _selectedArticleIds.Contains(a.IdArtikla));
        }

        public void ClearArticleSelection()
        {
            if (_selectedArticleIds.Count == 0)
                return;

            _selectedArticleIds.Clear();
            OnPropertyChanged(nameof(BrojOznacenihArtikala));
        }

        public void SetAllVisibleArticlesChecked(bool isChecked)
        {
            foreach (var article in ArtikliFilter)
            {
                if (isChecked)
                    _selectedArticleIds.Add(article.IdArtikla);
                else
                    _selectedArticleIds.Remove(article.IdArtikla);
            }

            OnPropertyChanged(nameof(BrojOznacenihArtikala));
        }

        private List<TblArtikli> GetCurrentArticleOrder()
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(ArtikliFilter);
            return view.Cast<TblArtikli>().ToList();
        }

        private void MoveFirst()
        {
            var articles = GetCurrentArticleOrder();

            if (articles.Count == 0)
                return;

            SelectedArticle = articles[0];
        }

        private void MovePrevious()
        {
            var articles = GetCurrentArticleOrder();

            if (SelectedArticle == null || articles.Count == 0)
                return;

            int index = articles.IndexOf(SelectedArticle);

            if (index <= 0)
                return;

            SelectedArticle = articles[index - 1];
        }

        private void MoveNext()
        {
            var articles = GetCurrentArticleOrder();

            if (articles.Count == 0)
                return;

            if (SelectedArticle == null)
            {
                SelectedArticle = articles[0];
                return;
            }

            int index = articles.IndexOf(SelectedArticle);

            if (index < 0)
            {
                SelectedArticle = articles[0];
                return;
            }

            if (index >= articles.Count - 1)
                return;

            SelectedArticle = articles[index + 1];
        }

        private void MoveLast()
        {
            var articles = GetCurrentArticleOrder();

            if (articles.Count == 0)
                return;

            SelectedArticle = articles[^1];
        }

        private bool CanMoveFirst()
        {
            var articles = GetCurrentArticleOrder();

            if (articles.Count == 0 || SelectedArticle == null)
                return false;

            return articles.IndexOf(SelectedArticle) > 0;
        }

        private bool CanMovePrevious()
        {
            var articles = GetCurrentArticleOrder();

            if (articles.Count == 0 || SelectedArticle == null)
                return false;

            return articles.IndexOf(SelectedArticle) > 0;
        }

        private bool CanMoveNext()
        {
            var articles = GetCurrentArticleOrder();

            if (articles.Count == 0 || SelectedArticle == null)
                return false;

            int index = articles.IndexOf(SelectedArticle);

            return index >= 0 && index < articles.Count - 1;
        }

        private bool CanMoveLast()
        {
            var articles = GetCurrentArticleOrder();

            if (articles.Count == 0 || SelectedArticle == null)
                return false;

            int index = articles.IndexOf(SelectedArticle);

            return index >= 0 && index < articles.Count - 1;
        }

        private void NotifyNavigationCommands()
        {
            FirstCommand?.NotifyCanExecuteChanged();
            PreviousCommand?.NotifyCanExecuteChanged();
            NextCommand?.NotifyCanExecuteChanged();
            LastCommand?.NotifyCanExecuteChanged();
        }

        public void RefreshNavigationCommands()
        {
            NotifyNavigationCommands();
        }

        public async Task LoadArticlesAsync()
        {
            try
            {
                int? selectedId = SelectedArticle?.IdArtikla;

                await using var db = new AppDbContext();

                var artikli = await db.Artikli
                    .AsNoTracking()
                    .OrderBy(a => a.IdArtikla)
                    .ToListAsync();

                foreach (var artikl in artikli)
                    artikl.KategorijaName = Kategorije.FirstOrDefault(x => x.IdKategorije == artikl.Kategorija)?.Kategorija ?? string.Empty;

                Artikli = new ObservableCollection<TblArtikli>(artikli);

                FilterItems(selectedId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ARTIKLI] LoadArticlesAsync: " + ex);
            }
        }

        public void FilterItems()
        {
            FilterItems(SelectedArticle?.IdArtikla);
        }

        private void FilterItems(int? selectedId)
        {
            string search = SearchText.Trim();

            IEnumerable<TblArtikli> filtered = Artikli;

            filtered = SelectedActiveFilter switch
            {
                ArticleActiveFilter.Aktivni => filtered.Where(a => a.Aktivan),
                ArticleActiveFilter.Neaktivni => filtered.Where(a => !a.Aktivan),
                _ => filtered
            };

            filtered = SelectedArticleTypeFilter switch
            {
                "Piće" => filtered.Where(a => a.VrstaArtikla == 0),
                "Hrana" => filtered.Where(a => a.VrstaArtikla == 1),
                "Ostalo" => filtered.Where(a => a.VrstaArtikla == 2),
                _ => filtered
            };

            if (SelectedCategoryFilter?.CategoryId != null)
                filtered = filtered.Where(a => a.Kategorija == SelectedCategoryFilter.CategoryId.Value);

            if (SelectedTaxFilter?.TaxId != null)
                filtered = filtered.Where(a => a.PoreskaStopa == SelectedTaxFilter.TaxId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(a =>
                    (!string.IsNullOrWhiteSpace(a.Artikl) && a.Artikl.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(a.Sifra) && a.Sifra.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(a.InternaSifra) && a.InternaSifra.Contains(search, StringComparison.OrdinalIgnoreCase)));
            }

            ArtikliFilter = new ObservableCollection<TblArtikli>(filtered);

            TblArtikli? selected = null;

            if (selectedId.HasValue)
                selected = ArtikliFilter.FirstOrDefault(a => a.IdArtikla == selectedId.Value);

            SelectedArticle = selected ?? ArtikliFilter.FirstOrDefault();

            NotifyNavigationCommands();
        }

        private void UpdateCategoryFilters()
        {
            CategoryFilters.Clear();
            CategoryFilters.Add(new ArticleCategoryFilter(null, "Sve kategorije"));

            int? articleType = SelectedArticleTypeFilter switch
            {
                "Piće" => 0,
                "Hrana" => 1,
                "Ostalo" => 2,
                _ => null
            };

            if (articleType.HasValue)
            {
                foreach (var kategorija in Kategorije.Where(k => k.VrstaArtikla == articleType.Value).OrderBy(k => k.Kategorija))
                    CategoryFilters.Add(new ArticleCategoryFilter(kategorija.IdKategorije, kategorija.Kategorija ?? string.Empty));
            }

            _selectedCategoryFilter = CategoryFilters.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedCategoryFilter));
        }

        private void UpdateTaxFilters()
        {
            TaxFilters.Clear();
            TaxFilters.Add(new ArticleTaxFilter(null, "Sve poreske stope"));

            foreach (var poreskaStopa in PoreskeStope)
                TaxFilters.Add(new ArticleTaxFilter(poreskaStopa.IdStope, poreskaStopa.ToString()));

            _selectedTaxFilter = TaxFilters.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedTaxFilter));
        }

        public async Task<bool> DeleteArticle(int articleId)
        {
            await using var db = new AppDbContext();
            var artikl = await db.Artikli.FindAsync(articleId);

            if (artikl == null)
            {
                MyMessageBox myMessageBox = new MyMessageBox();
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                myMessageBox.MessageTitle.Text = "Greška";
                myMessageBox.MessageText.Text = "Artikl sa ID: " + articleId + " nije pronađen u bazi." + Environment.NewLine + "Neuspješno brisanje artikla";
                myMessageBox.ShowDialog();
                return false;
            }

            string? nazivArtikla = artikl.Artikl;
            db.Artikli.Remove(artikl);
            await db.SaveChangesAsync();
            await LoadArticlesAsync();

            MyMessageBox successMessageBox = new MyMessageBox();
            successMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            successMessageBox.MessageTitle.Text = "POTVRDA";
            successMessageBox.MessageText.Text = "Artikl " + nazivArtikla + " je uspješno obrisan iz baze.";
            successMessageBox.ShowDialog();
            return true;
        }

        public async Task<bool> DeactivateArticle(int articleId)
        {
            await using var db = new AppDbContext();
            var artikl = await db.Artikli.FindAsync(articleId);

            if (artikl == null)
            {
                MyMessageBox myMessageBox = new MyMessageBox();
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                myMessageBox.MessageTitle.Text = "Greška";
                myMessageBox.MessageText.Text = "Artikl sa ID: " + articleId + " nije pronađen u bazi.";
                myMessageBox.ShowDialog();
                return false;
            }

            artikl.Aktivan = false;
            await db.SaveChangesAsync();
            await LoadArticlesAsync();
            return true;
        }

        public async Task<bool> InsertArticle(TblArtikli artikl)
        {
            await using var db = new AppDbContext();

            var duplicateArticle = await db.Artikli.FirstOrDefaultAsync(a => a.Sifra == artikl.Sifra || a.InternaSifra == artikl.InternaSifra || a.ArtiklNormativ == artikl.ArtiklNormativ);

            if (duplicateArticle != null)
            {
                string poruka = string.Empty;

                if (duplicateArticle.Sifra == artikl.Sifra)
                    poruka += "Šifra " + artikl.Sifra + ", ";

                if (duplicateArticle.InternaSifra == artikl.InternaSifra)
                    poruka += "Interna šifra " + artikl.InternaSifra + ", ";

                if (duplicateArticle.ArtiklNormativ == artikl.ArtiklNormativ)
                    poruka += "Naziv za prodaju " + artikl.ArtiklNormativ + ", ";

                poruka = poruka.TrimEnd(',', ' ');
                poruka += " se već koristi u bazi. Ova vrijednost mora biti jedinstvena za svaki artikl.";

                OnErrorOccurred(poruka);
                return false;
            }

            await db.Artikli.AddAsync(artikl);
            await db.SaveChangesAsync();

            int insertedId = artikl.IdArtikla;

            await LoadArticlesAsync();
            SelectedArticle = ArtikliFilter.FirstOrDefault(a => a.IdArtikla == insertedId) ?? ArtikliFilter.LastOrDefault();

            MyMessageBox myMessageBox = new MyMessageBox();
            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            myMessageBox.MessageTitle.Text = "POTVRDA";
            myMessageBox.MessageText.Text = "Artikl " + artikl.Artikl + " je uspješno dodan u bazu.";
            myMessageBox.ShowDialog();
            return true;
        }

        public async Task<bool> UpdateArticle(TblArtikli artikl)
        {
            await using var db = new AppDbContext();
            Debug.WriteLine ($"[ARTICLE EDIT] Id={artikl.IdArtikla}, Sifra='{artikl.Sifra}', Interna='{artikl.InternaSifra}', ArtiklNormativ='{artikl.ArtiklNormativ}'");
            var duplicateArticle = await db.Artikli.FirstOrDefaultAsync(a => a.IdArtikla != artikl.IdArtikla && (a.Sifra == artikl.Sifra || a.InternaSifra == artikl.InternaSifra || a.ArtiklNormativ == artikl.ArtiklNormativ));

            if (duplicateArticle != null)
            {
                Debug.WriteLine ($"[ARTICLE EDIT] DUPLIKAT: Id={duplicateArticle.IdArtikla}, Sifra='{duplicateArticle.Sifra}', Interna='{duplicateArticle.InternaSifra}', ArtiklNormativ='{duplicateArticle.ArtiklNormativ}'");
                string poruka = string.Empty;

                if (duplicateArticle.Sifra == artikl.Sifra)
                    poruka += "Šifra " + artikl.Sifra + ", ";

                if (duplicateArticle.InternaSifra == artikl.InternaSifra)
                    poruka += "Interna šifra " + artikl.InternaSifra + ", ";

                if (duplicateArticle.ArtiklNormativ == artikl.ArtiklNormativ)
                    poruka += "Naziv za prodaju " + artikl.ArtiklNormativ + ", ";

                poruka = poruka.TrimEnd(',', ' ');
                poruka += " se već koristi u bazi. Ova vrijednost mora biti jedinstvena za svaki artikl.";

                OnErrorOccurred(poruka);
                return false;
            }

            var existingArticle = await db.Artikli.FindAsync(artikl.IdArtikla);

            if (existingArticle == null)
            {
                MyMessageBox errorMessageBox = new MyMessageBox();
                errorMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                errorMessageBox.MessageTitle.Text = "Greška";
                errorMessageBox.MessageText.Text = "Artikl sa ID: " + artikl.IdArtikla + " nije pronađen u bazi." + Environment.NewLine + "Neuspješno ažuriranje artikla";
                errorMessageBox.ShowDialog();
                return false;
            }

            db.Entry(existingArticle).CurrentValues.SetValues(artikl);
            await db.SaveChangesAsync();

            int updatedId = existingArticle.IdArtikla;
            string? nazivArtikla = existingArticle.Artikl;

            await LoadArticlesAsync();
            SelectedArticle = ArtikliFilter.FirstOrDefault(a => a.IdArtikla == updatedId);

            MyMessageBox myMessageBox = new MyMessageBox();
            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            myMessageBox.MessageTitle.Text = "POTVRDA";
            myMessageBox.MessageText.Text = "Artikl " + nazivArtikla + " je uspješno ažuriran.";
            myMessageBox.ShowDialog();
            return true;
        }

        public async Task<long> GetNextArticleSequenceValueAsync()
        {
            await using var db = new AppDbContext();
            var connection = db.Database.GetDbConnection();
            bool closeConnection = connection.State != System.Data.ConnectionState.Open;

            if (closeConnection)
                await connection.OpenAsync();

            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT seq FROM sqlite_sequence WHERE name = 'tblArtikli' LIMIT 1;";
                object? result = await command.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? 1L : Convert.ToInt64(result) + 1L;
            }
            finally
            {
                if (closeConnection)
                    await connection.CloseAsync();
            }
        }

        public async Task GetNewSifra()
        {
            await using var db = new AppDbContext();

            var poslednjiArtikl = await db.Artikli.OrderByDescending(a => a.IdArtikla).FirstOrDefaultAsync();

            if (poslednjiArtikl == null)
            {
                NovaSifra = "1";
                return;
            }

            if (int.TryParse(poslednjiArtikl.Sifra, out int poslednjaSifra))
                NovaSifra = (poslednjaSifra + 1).ToString();
            else
                NovaSifra = (poslednjiArtikl.IdArtikla + 1).ToString();
        }

        public void UpdateComboBoxes()
        {
            if (SelectedArticle == null)
            {
                SelectedJedinicaMjere = null;
                SelectedKategorija = null;
                SelectedNormativ = null;
                SelectedVrstaArtikla = null;
                SelectedPoreskaStopa = null;
                return;
            }

            SelectedJedinicaMjere = JediniceMjere.FirstOrDefault(item => item.IdJedinice == SelectedArticle.JedinicaMjere);
            SelectedKategorija = Kategorije.FirstOrDefault(item => item.IdKategorije == SelectedArticle.Kategorija);
            SelectedNormativ = Normativi.FirstOrDefault(item => item.Normativ == SelectedArticle.Normativ.ToString());
            SelectedPoreskaStopa = PoreskeStope.FirstOrDefault(item => item.IdStope == SelectedArticle.PoreskaStopa);

            int vrsta = SelectedArticle.VrstaArtikla ?? 0;
            SelectedVrstaArtikla = vrsta >= 0 && vrsta < VrstaArtikla.Count ? VrstaArtikla[vrsta] : null;
        }

        public async Task LoadPoreskeStope()
        {
            try
            {
                await using var db = new AppDbContext();

                var poreskeStope = await db.PoreskeStope.Where(x => x.Aktivna).OrderBy(x => x.IdStope).ToListAsync();

                PoreskeStope.Clear();

                foreach (var poreskaStopa in poreskeStope)
                    PoreskeStope.Add(poreskaStopa);

                UpdateTaxFilters();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ARTIKLI] LoadPoreskeStope: " + ex);
            }
        }

        public async Task LoadNormativi()
        {
            try
            {
                await using var db = new AppDbContext();

                var normativi = await db.NormativPica.ToListAsync();

                Normativi.Clear();

                foreach (var normativ in normativi)
                    Normativi.Add(normativ);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ARTIKLI] LoadNormativi: " + ex);
            }
        }

        public async Task LoadKategorije()
        {
            try
            {
                await using var db = new AppDbContext();

                var kategorije = await db.Kategorije.OrderBy(x => x.Kategorija).ToListAsync();

                Kategorije.Clear();

                foreach (var kategorija in kategorije)
                    Kategorije.Add(kategorija);

                UpdateCategoryFilters();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ARTIKLI] LoadKategorije: " + ex);
            }
        }

        public void LoadVrsteArtikla()
        {
            VrstaArtikla.Clear();
            VrstaArtikla.Add("Piće");
            VrstaArtikla.Add("Hrana");
            VrstaArtikla.Add("Ostalo");
        }

        public async Task<bool> HasArticleBeenSold(int idArtikla)
        {
            if(idArtikla <= 0)
                return false;

            await using var db = new AppDbContext ();
            return await db.RacunStavka.AnyAsync (x => x.IdArtikla == idArtikla);
        }

        public async Task LoadJediniceMjere()
        {
            try
            {
                await using var db = new AppDbContext();

                var jediniceMjere = await db.JediniceMjere.OrderBy(x => x.IdJedinice).ToListAsync();

                JediniceMjere.Clear();

                foreach (var jedinicaMjere in jediniceMjere)
                    JediniceMjere.Add(jedinicaMjere);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ARTIKLI] LoadJediniceMjere: " + ex);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}