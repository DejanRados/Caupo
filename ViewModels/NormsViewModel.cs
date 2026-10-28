
using Caupo.Data;
using Caupo.Properties;
using Caupo.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SearchSuggestion = Caupo.UserControls.SearchSuggestion;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class NormsViewModel : INotifyPropertyChanged
    {
        private decimal? proizvodnaCijena;
        public decimal? ProizvodnaCijena
        {
            get => proizvodnaCijena;
            set { proizvodnaCijena = value; OnPropertyChanged(); }
        }

        private decimal? prodajnaCijena;
        public decimal? ProdajnaCijena
        {
            get => prodajnaCijena;
            set { prodajnaCijena = value; OnPropertyChanged(); }
        }

        private string? marza;
        public string? Marza
        {
            get => marza;
            set { marza = value; OnPropertyChanged(); }
        }

        private string? _imagePathFirstButton;
        public string? ImagePathFirstButton
        {
            get => _imagePathFirstButton;
            set { _imagePathFirstButton = value; OnPropertyChanged(); }
        }

        private string? _imagePathLastButton;
        public string? ImagePathLastButton
        {
            get => _imagePathLastButton;
            set { _imagePathLastButton = value; OnPropertyChanged(); }
        }

        private string? _imagePathPhotoButton;
        public string? ImagePathPhotoButton
        {
            get => _imagePathPhotoButton;
            set { _imagePathPhotoButton = value; OnPropertyChanged(); }
        }

        private string? _imagePathEditButton;
        public string? ImagePathEditButton
        {
            get => _imagePathEditButton;
            set { _imagePathEditButton = value; OnPropertyChanged(); }
        }

        private string? _imagePathAddButton;
        public string? ImagePathAddButton
        {
            get => _imagePathAddButton;
            set { _imagePathAddButton = value; OnPropertyChanged(); }
        }

        private string? _imagePathDeleteButton;
        public string? ImagePathDeleteButton
        {
            get => _imagePathDeleteButton;
            set { _imagePathDeleteButton = value; OnPropertyChanged(); }
        }

        private string? _imagePathCancelButton;
        public string? ImagePathCancelButton
        {
            get => _imagePathCancelButton;
            set { _imagePathCancelButton = value; OnPropertyChanged(); }
        }

        private string? _imagePathSaveButton;
        public string? ImagePathSaveButton
        {
            get => _imagePathSaveButton;
            set { _imagePathSaveButton = value; OnPropertyChanged(); }
        }

        private string? _imagePathExportButton;
        public string? ImagePathExportButton
        {
            get => _imagePathExportButton;
            set { _imagePathExportButton = value; OnPropertyChanged(); }
        }

        private string? _imagePathImportButton;
        public string? ImagePathImportButton
        {
            get => _imagePathImportButton;
            set { _imagePathImportButton = value; OnPropertyChanged(); }
        }

        private string? _imagePathSearchTextBox;
        public string? ImagePathSearchTextBox
        {
            get => _imagePathSearchTextBox;
            set { _imagePathSearchTextBox = value; OnPropertyChanged(); }
        }

        private Brush? _fontColor;
        public Brush? FontColor
        {
            get => _fontColor;
            set
            {
                if (_fontColor == value) return;
                _fontColor = value;
                OnPropertyChanged();
            }
        }

        private Brush? _backColor;
        public Brush? BackColor
        {
            get => _backColor;
            set
            {
                if (_backColor == value) return;
                _backColor = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<TblArtikli> _jela = new();
        public ObservableCollection<TblArtikli> Jela
        {
            get => _jela;
            set { _jela = value; OnPropertyChanged(); }
        }

        private ObservableCollection<TblArtikli> _suggestions = new();
        public ObservableCollection<TblArtikli> Suggestions
        {
            get => _suggestions;
            set { _suggestions = value; OnPropertyChanged(); }
        }

        public IEnumerable<SearchSuggestion> SearchSuggestions =>
            Suggestions.Select(jelo => new SearchSuggestion
            {
                Title = jelo.Artikl ?? string.Empty,
                Subtitle = $"Šifra: {jelo.Sifra}",
                Detail = $"Cijena: {jelo.Cijena:N2} KM",
                Item = jelo
            });

        public ObservableCollection<TblRepromaterijal> Ingredients { get; set; } = new();
        public ObservableCollection<TblJediniceMjere> JediniceMjere { get; set; } = new();

        private TblRepromaterijal? _selectedIngredient;
        public TblRepromaterijal? SelectedIngredient
        {
            get => _selectedIngredient;
            set
            {
                if (_selectedIngredient == value) return;
                _selectedIngredient = value;
                OnPropertyChanged();
            }
        }

        private TblNormativ? _selectedNorm;
        public TblNormativ? SelectedNorm
        {
            get => _selectedNorm;
            set
            {
                if (_selectedNorm == value) return;
                _selectedNorm = value;
                OnPropertyChanged();
            }
        }

        private TblJediniceMjere? _selectedJedinicaMjere;
        public TblJediniceMjere? SelectedJedinicaMjere
        {
            get => _selectedJedinicaMjere;
            set
            {
                if (_selectedJedinicaMjere == value) return;
                _selectedJedinicaMjere = value;
                OnPropertyChanged();
            }
        }

        private TblArtikli? _selectedJelo;
        public TblArtikli? SelectedJelo
        {
            get => _selectedJelo;
            set
            {
                if (_selectedJelo == value) return;
                _selectedJelo = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<TblNormativ>? _norm = new();
        public ObservableCollection<TblNormativ>? Norm
        {
            get => _norm;
            set { _norm = value; OnPropertyChanged(); }
        }

        private ObservableCollection<TblRepromaterijal>? _ingredientFilter = new();
        public ObservableCollection<TblRepromaterijal>? IngredientFilter
        {
            get => _ingredientFilter;
            set { _ingredientFilter = value; OnPropertyChanged(); }
        }

        private bool _isSuggestionsOpen;
        public bool IsSuggestionsOpen
        {
            get => _isSuggestionsOpen;
            set
            {
                if (_isSuggestionsOpen == value) return;
                _isSuggestionsOpen = value;
                OnPropertyChanged();
            }
        }

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                Debug.WriteLine("SearchText changed to: " + _searchText);
                FilterItems(_searchText);
            }
        }

        private string? _searchTextIngredient;
        public string? SearchTextIngredient
        {
            get => _searchTextIngredient;
            set
            {
                if (_searchTextIngredient == value) return;
                _searchTextIngredient = value;
                OnPropertyChanged();
                Debug.WriteLine("SearchTextIngredient changed to: " + _searchTextIngredient);
                FilterItemsIngredient(_searchTextIngredient);
            }
        }

        public ICommand DeleteCommand { get; }
        public ICommand FirstJeloCommand { get; }
        public ICommand PreviousJeloCommand { get; }
        public ICommand NextJeloCommand { get; }
        public ICommand LastJeloCommand { get; }

        public NormsViewModel()
        {
            Suggestions = new ObservableCollection<TblArtikli>();
            Jela = new ObservableCollection<TblArtikli>();
            Ingredients = new ObservableCollection<TblRepromaterijal>();
            IngredientFilter = new ObservableCollection<TblRepromaterijal>();
            Norm = new ObservableCollection<TblNormativ>();

            SelectedIngredient = IngredientFilter.FirstOrDefault();
            DeleteCommand = new AsyncRelayCommand(async () => await DeleteNorm(SelectedNorm));
            FirstJeloCommand = new AsyncRelayCommand(async () => await NavigateJeloAsync(0, true));
            PreviousJeloCommand = new AsyncRelayCommand(async () => await NavigateJeloAsync(-1, false));
            NextJeloCommand = new AsyncRelayCommand(async () => await NavigateJeloAsync(1, false));
            LastJeloCommand = new AsyncRelayCommand(async () => await NavigateJeloAsync(-1, true));

            Debug.WriteLine("SearchText = " + SearchText);
            ProizvodnaCijena = 0;
            ProdajnaCijena = 0;
            Marza = "0.00 KM        0%";

            Start();
        }

        private async Task NavigateJeloAsync(int direction, bool boundary)
        {
            if (Jela.Count == 0) return;

            int currentIndex = Jela.IndexOf(SelectedJelo!);
            int targetIndex = boundary
                ? (direction == 0 ? 0 : Jela.Count - 1)
                : currentIndex + direction;

            if (currentIndex < 0 && !boundary)
                targetIndex = direction > 0 ? 0 : Jela.Count - 1;

            if (targetIndex < 0 || targetIndex >= Jela.Count) return;

            var jelo = Jela[targetIndex];
            if (SelectedJelo != jelo)
                await LoadNormativ(jelo);
        }

        private async void Start()
        {
            await SetImage();
            await LoadJela();
            await LoadIngredientsAsync();
            await LoadJediniceMjere();

            if (SelectedJelo != null)
                await LoadNormativ(SelectedJelo);
        }

        public async Task DeleteNorm(TblNormativ? norm)
        {
            if (norm == null) return;

            YesNoPopup myMessageBox = new YesNoPopup
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
            myMessageBox.MessageText.Text =
                "Da li ste sigurni da želite obrisati normativ:" +
                Environment.NewLine + norm.Repromaterijal +
                ", količina: " + norm.Kolicina + " ?";
            myMessageBox.ShowDialog();

            if (myMessageBox.Kliknuo != "Da") return;

            try
            {
                using var db = new AppDbContext();
                var stavka = await db.Normativ.FindAsync(norm.IdStavkeNormativa);
                if (stavka != null)
                {
                    db.Normativ.Remove(stavka);
                    await db.SaveChangesAsync();

                    if (SelectedJelo != null)
                        await LoadNormativ(SelectedJelo);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Brisanje normativa error: " +
                    ex.Message + Environment.NewLine + "Stack: " + ex.StackTrace);
            }
        }

        public async Task LoadJela()
        {
            try
            {
                using var db = new AppDbContext();
                var artikli = await db.Artikli.ToListAsync();
                var jela = artikli
                    .Where(a => a.VrstaArtikla == 1)
                    .OrderBy(a => a.Pozicija)
                    .ToList();

                Jela.Clear();
                foreach (var jelo in jela)
                    Jela.Add(jelo);

                Debug.WriteLine("Jela ima >>>>>> " + Jela.Count);
                SelectedJelo = Jela.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public async Task InsertNorm(TblNormativ normativ, TblArtikli jelo)
        {
            using var db = new AppDbContext();

            var duplicateNormativ = await db.Normativ
                .Where(a => a.Repromaterijal == normativ.Repromaterijal &&
                            a.IdProizvoda == normativ.IdProizvoda)
                .FirstOrDefaultAsync();

            if (duplicateNormativ != null)
            {
                string poruka = normativ.Repromaterijal +
                    " se već koristi u izradi ovog jela.";
                OnErrorOccurred(poruka);
                return;
            }

            await db.Normativ.AddAsync(normativ);
            await db.SaveChangesAsync();
            Debug.WriteLine("Novi zapis je uspešno dodat!");
            await LoadNormativ(jelo);
        }

        public async Task UpdateNorm(TblNormativ normativ, TblArtikli jelo)
        {
            using var db = new AppDbContext();

            var existingNorm = await db.Normativ.FirstOrDefaultAsync(
                a => a.IdStavkeNormativa == normativ.IdStavkeNormativa);

            if (existingNorm == null)
            {
                OnErrorOccurred("Normativ nije pronađen u bazi.");
                return;
            }

            existingNorm.Repromaterijal = normativ.Repromaterijal;
            existingNorm.Kolicina = normativ.Kolicina;
            existingNorm.JedinicaMjere = normativ.JedinicaMjere;
            existingNorm.IdProizvoda = normativ.IdProizvoda;
            existingNorm.Cijena = normativ.Cijena;

            await db.SaveChangesAsync();
            Debug.WriteLine("Zapis je uspješno ažuriran!");
            await LoadNormativ(jelo);
        }

        public async Task LoadNormativ(TblArtikli jelo)
        {
            SelectedJelo = jelo;
            Norm?.Clear();
            ProizvodnaCijena = 0;
            ProdajnaCijena = 0;
            Marza = string.Empty;

            Debug.WriteLine("Jelo : " + jelo.Artikl + " ima ID: " + jelo.IdArtikla);

            try
            {
                using var db = new AppDbContext();

                var norm = await db.Normativ
                    .Where(a => a.IdProizvoda == jelo.IdArtikla)
                    .OrderBy(a => a.IdStavkeNormativa)
                    .ToListAsync();

                var namirnice = await db.Repromaterijal
                    .AsNoTracking()
                    .ToListAsync();

                var kaloPoNamirnici = namirnice
                    .Where(a => !string.IsNullOrWhiteSpace(a.Repromaterijal))
                    .GroupBy(a => a.Repromaterijal!.Trim(),
                             StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(a => a.Key, a => a.First().Kalo,
                                  StringComparer.OrdinalIgnoreCase);

                foreach (var ingredient in norm)
                {
                    string naziv = ingredient.Repromaterijal?.Trim() ?? string.Empty;

                    ingredient.Kalo = kaloPoNamirnici.TryGetValue(naziv, out var kalo)
                        ? kalo
                        : null;

                    Norm?.Add(ingredient);
                    ProizvodnaCijena += ingredient.Ukupno;
                }

                ProdajnaCijena = jelo.Cijena;
                SelectedNorm = Norm?.FirstOrDefault();

                if (norm.Count > 0)
                {
                    decimal proizvodna = ProizvodnaCijena ?? 0m;
                    decimal prodajna = ProdajnaCijena ?? 0m;
                    decimal razlika = prodajna - proizvodna;

                    decimal postotak = prodajna != 0m
                        ? razlika / prodajna * 100m
                        : 0m;

                    Marza = $"{razlika:F2} KM    {postotak:F2}%";
                }

                Debug.WriteLine("Jelo ima >>>>>> " + Norm?.Count + " sastojaka");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public async Task LoadJediniceMjere()
        {
            try
            {
                using var db = new AppDbContext();
                var JM = await db.JediniceMjere.ToListAsync();

                JediniceMjere.Clear();
                foreach (var jm in JM)
                {
                    JediniceMjere.Add(jm);
                    Debug.WriteLine(jm.JedinicaMjere +
                        ", JM Count = " + JediniceMjere.Count);
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

        public async Task SetImage()
        {
            await Task.Delay(1);
            string tema = Settings.Default.Tema;

            if (tema == "Tamna")
            {
                Debug.WriteLine("Aktivna tema koju vidi viewmodel je : " + tema);
                FontColor = new SolidColorBrush(Color.FromRgb(212, 212, 212));
                BackColor = new SolidColorBrush(Color.FromRgb(50, 50, 50));
            }
            else
            {
                Debug.WriteLine("Aktivna tema koju vidi viewmodel je : " + tema);
                FontColor = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                BackColor = new SolidColorBrush(Color.FromRgb(212, 212, 212));
            }
        }

        public async Task LoadIngredientsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var ingredients = await db.Repromaterijal.ToListAsync();

                Ingredients.Clear();
                IngredientFilter?.Clear();

                foreach (var ingredient in ingredients)
                {
                    Ingredients.Add(ingredient);
                    IngredientFilter?.Add(ingredient);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public void FilterItems(string? searchtext)
        {
            string query = (searchtext ?? string.Empty).Trim();
            Suggestions.Clear();

            if (!string.IsNullOrWhiteSpace(query))
            {
                foreach (var jelo in Jela.Where(a =>
                    (a.Artikl ?? string.Empty).Contains(
                        query, StringComparison.OrdinalIgnoreCase)))
                {
                    Suggestions.Add(jelo);
                }
            }

            OnPropertyChanged(nameof(SearchSuggestions));
        }

        public void FilterItemsIngredient(string? searchtext)
        {
            Debug.WriteLine("Okida search");

            var filtered = Ingredients
                .Where(a => (a.Repromaterijal ?? string.Empty)
                    .Contains(searchtext ?? string.Empty,
                              StringComparison.OrdinalIgnoreCase))
                .ToList();

            IngredientFilter =
                new ObservableCollection<TblRepromaterijal>(filtered);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
