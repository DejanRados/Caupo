
using Caupo.Data;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using SearchSuggestion = Caupo.UserControls.SearchSuggestion;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class IngredientsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<TblRepromaterijal> _ingredients = [];
        private ObservableCollection<TblRepromaterijal> _ingredientFilter = [];
        private TblRepromaterijal? _selectedIngredient;
        private TblJediniceMjere? _selectedJedinicaMjere;
        private string _searchText = string.Empty;
        private bool _isBusy;
        private int? _editingId;

        private string _editorName = string.Empty;
        private string _editorPurchasePrice = string.Empty;
        private string _editorPlannedPrice = string.Empty;
        private string _editorKalo = string.Empty;
        private TblJediniceMjere? _editorUnit;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<TblRepromaterijal> Ingredients
        {
            get => _ingredients;
            private set
            {
                _ingredients = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalCount));
            }
        }

        public ObservableCollection<TblRepromaterijal> IngredientFilter
        {
            get => _ingredientFilter;
            private set
            {
                _ingredientFilter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SearchSuggestions));
                OnPropertyChanged(nameof(VisibleCount));
            }
        }

        public ObservableCollection<TblJediniceMjere> JediniceMjere { get; } = [];

        public TblRepromaterijal? SelectedIngredient
        {
            get => _selectedIngredient;
            set
            {
                if (ReferenceEquals(_selectedIngredient, value))
                    return;

                _selectedIngredient = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedIngredient));

                SelectedJedinicaMjere = JediniceMjere.FirstOrDefault(x => x.IdJedinice == value?.JedinicaMjere);
                NotifyNavigationCommands();
            }
        }

        public TblJediniceMjere? SelectedJedinicaMjere
        {
            get => _selectedJedinicaMjere;
            private set
            {
                _selectedJedinicaMjere = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                value ??= string.Empty;
                if (_searchText == value)
                    return;

                _searchText = value;
                OnPropertyChanged();
                FilterItems();
            }
        }

        public IEnumerable<SearchSuggestion> SearchSuggestions =>
            IngredientFilter.Select(x => new SearchSuggestion
            {
                Title = x.Repromaterijal ?? string.Empty,
                Subtitle = x.JedinicaMjereName ?? string.Empty,
                Detail = x.NabavnaCijena?.ToString("F2", CultureInfo.CurrentCulture) ?? string.Empty,
                Item = x
            });

        public int TotalCount => Ingredients.Count;
        public int VisibleCount => IngredientFilter.Count;
        public bool HasSelectedIngredient => SelectedIngredient != null;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value)
                    return;

                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public bool IsEditing => _editingId.HasValue;
        public string EditorTitle => IsEditing ? "Izmjena namirnice" : "Nova namirnica";
        public string EditorDescription => IsEditing ? "Uredite podatke odabrane namirnice" : "Unesite podatke nove namirnice";

        public string EditorName
        {
            get => _editorName;
            set
            {
                _editorName = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string EditorPurchasePrice
        {
            get => _editorPurchasePrice;
            set
            {
                _editorPurchasePrice = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string EditorPlannedPrice
        {
            get => _editorPlannedPrice;
            set
            {
                _editorPlannedPrice = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public string EditorKalo
        {
            get => _editorKalo;
            set
            {
                _editorKalo = value ?? string.Empty;
                OnPropertyChanged();
            }
        }

        public TblJediniceMjere? EditorUnit
        {
            get => _editorUnit;
            set
            {
                _editorUnit = value;
                OnPropertyChanged();
            }
        }

        public IRelayCommand FirstIngredientCommand { get; }
        public IRelayCommand PreviousIngredientCommand { get; }
        public IRelayCommand NextIngredientCommand { get; }
        public IRelayCommand LastIngredientCommand { get; }

        public IngredientsViewModel()
        {
            FirstIngredientCommand = new RelayCommand(MoveFirst, CanMoveFirst);
            PreviousIngredientCommand = new RelayCommand(MovePrevious, CanMovePrevious);
            NextIngredientCommand = new RelayCommand(MoveNext, CanMoveNext);
            LastIngredientCommand = new RelayCommand(MoveLast, CanMoveLast);
        }

        public async Task InitializeAsync()
        {
            await LoadJediniceMjere();
            await LoadIngredientsAsync();
        }

        public async Task LoadJediniceMjere()
        {
            await using var db = new AppDbContext();
            var units = await db.JediniceMjere.AsNoTracking().OrderBy(x => x.IdJedinice).ToListAsync();

            JediniceMjere.Clear();
            foreach (var unit in units)
                JediniceMjere.Add(unit);

            SelectedJedinicaMjere = JediniceMjere.FirstOrDefault(x => x.IdJedinice == SelectedIngredient?.JedinicaMjere);
        }

        public async Task LoadIngredientsAsync(int? preferredId = null)
        {
            int? selectedId = preferredId ?? SelectedIngredient?.IdRepromaterijala;

            await using var db = new AppDbContext();
            var ingredients = await db.Repromaterijal.AsNoTracking()
                .OrderBy(x => x.Repromaterijal).ToListAsync();

            Ingredients = new ObservableCollection<TblRepromaterijal>(ingredients);
            FilterItems(selectedId);
        }

        private void FilterItems(int? preferredId = null)
        {
            var filtered = Ingredients.Where(x => (x.Repromaterijal ?? string.Empty)
                .Contains(SearchText.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();

            IngredientFilter = new ObservableCollection<TblRepromaterijal>(filtered);

            int? selectedId = preferredId ?? SelectedIngredient?.IdRepromaterijala;
            SelectedIngredient = IngredientFilter.FirstOrDefault(x => x.IdRepromaterijala == selectedId)
                ?? IngredientFilter.FirstOrDefault();

            NotifyNavigationCommands();
        }

        private List<TblRepromaterijal> CurrentOrder()
        {
            return CollectionViewSource.GetDefaultView(IngredientFilter).Cast<TblRepromaterijal>().ToList();
        }

        private int SelectedIndex()
        {
            return CurrentOrder().IndexOf(SelectedIngredient!);
        }

        private void MoveFirst()
        {
            var items = CurrentOrder();
            if (items.Count > 0) SelectedIngredient = items[0];
        }

        private void MovePrevious()
        {
            var items = CurrentOrder();
            int index = items.IndexOf(SelectedIngredient!);
            if (index > 0) SelectedIngredient = items[index - 1];
        }

        private void MoveNext()
        {
            var items = CurrentOrder();
            int index = items.IndexOf(SelectedIngredient!);
            if (index >= 0 && index < items.Count - 1) SelectedIngredient = items[index + 1];
        }

        private void MoveLast()
        {
            var items = CurrentOrder();
            if (items.Count > 0) SelectedIngredient = items[^1];
        }

        private bool CanMoveFirst() => SelectedIndex() > 0;
        private bool CanMovePrevious() => SelectedIndex() > 0;
        private bool CanMoveNext() => SelectedIndex() >= 0 && SelectedIndex() < IngredientFilter.Count - 1;
        private bool CanMoveLast() => SelectedIndex() >= 0 && SelectedIndex() < IngredientFilter.Count - 1;

        private void NotifyNavigationCommands()
        {
            FirstIngredientCommand.NotifyCanExecuteChanged();
            PreviousIngredientCommand.NotifyCanExecuteChanged();
            NextIngredientCommand.NotifyCanExecuteChanged();
            LastIngredientCommand.NotifyCanExecuteChanged();
        }

        public void BeginAdd()
        {
            _editingId = null;
            EditorName = string.Empty;
            EditorPurchasePrice = string.Empty;
            EditorPlannedPrice = string.Empty;
            EditorKalo = "0";
            EditorUnit = JediniceMjere.FirstOrDefault();

            OnPropertyChanged(nameof(IsEditing));
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(EditorDescription));
        }

        public bool BeginEdit()
        {
            var ingredient = SelectedIngredient;
            if (ingredient == null || !ingredient.IdRepromaterijala.HasValue)
                return false;

            _editingId = ingredient.IdRepromaterijala.Value;
            EditorName = ingredient.Repromaterijal ?? string.Empty;
            EditorPurchasePrice = ingredient.NabavnaCijena?.ToString("F2", CultureInfo.CurrentCulture) ?? "0";
            EditorPlannedPrice = ingredient.PlanskaCijena?.ToString("F2", CultureInfo.CurrentCulture) ?? "0";
            EditorKalo = (ingredient.Kalo ?? 0m).ToString("0.##", CultureInfo.CurrentCulture);
            EditorUnit = JediniceMjere.FirstOrDefault(x => x.IdJedinice == ingredient.JedinicaMjere);

            OnPropertyChanged(nameof(IsEditing));
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(EditorDescription));
            return true;
        }

        public void CancelEdit()
        {
            _editingId = null;
        }

        private string? ValidateEditor(out decimal purchase, out decimal planned, out decimal kalo)
        {
            purchase = 0;
            planned = 0;
            kalo = 0;

            if (string.IsNullOrWhiteSpace(EditorName))
                return "Unesite naziv namirnice.";

            if (EditorUnit == null)
                return "Odaberite jedinicu mjere.";

            if (!decimal.TryParse(EditorPurchasePrice, NumberStyles.Number, CultureInfo.CurrentCulture, out purchase) || purchase < 0)
                return "Nabavna cijena mora biti ispravan nenegativan broj.";

            if (!decimal.TryParse(EditorPlannedPrice, NumberStyles.Number, CultureInfo.CurrentCulture, out planned) || planned < 0)
                return "Planska cijena mora biti ispravan nenegativan broj.";

            if (!decimal.TryParse(EditorKalo, NumberStyles.Number, CultureInfo.CurrentCulture, out kalo) || kalo > 100)
                return "Kalo mora biti ispravan procenat koji nije veći od 100%.";

            return null;
        }

        public async Task<string?> SaveEditorAsync()
        {
            if (IsBusy)
                return "Spremanje je već u toku.";

            string? validation = ValidateEditor(out decimal purchase, out decimal planned, out decimal kalo);
            if (validation != null)
                return validation;

            IsBusy = true;
            try
            {
                await using var db = new AppDbContext();
                string name = EditorName.Trim();

                bool duplicate = await db.Repromaterijal.AnyAsync(x =>
                    x.Repromaterijal != null && x.Repromaterijal.ToLower() == name.ToLower() &&
                    (!_editingId.HasValue || x.IdRepromaterijala != _editingId.Value));

                if (duplicate)
                    return $"Namirnica '{name}' već postoji u bazi.";

                TblRepromaterijal ingredient;

                if (_editingId.HasValue)
                {
                    ingredient = await db.Repromaterijal.FindAsync(_editingId.Value)
                        ?? throw new InvalidOperationException("Odabrana namirnica više ne postoji u bazi.");
                }
                else
                {
                    ingredient = new TblRepromaterijal();
                    db.Repromaterijal.Add(ingredient);
                }

                ingredient.Repromaterijal = name;
                ingredient.JedinicaMjere = EditorUnit!.IdJedinice;
                ingredient.NabavnaCijena = purchase;
                ingredient.PlanskaCijena = planned;
                ingredient.Kalo = kalo;

                await db.SaveChangesAsync();
                await LoadIngredientsAsync(ingredient.IdRepromaterijala);
                _editingId = null;
                return null;
            }
            catch (Exception ex)
            {
                return $"Spremanje namirnice nije uspjelo: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<string?> DeleteSelectedAsync()
        {
            if (SelectedIngredient?.IdRepromaterijala is not int id)
                return "Nije odabrana namirnica.";

            IsBusy = true;
            try
            {
                await using var db = new AppDbContext();
                var ingredient = await db.Repromaterijal.FindAsync(id);

                if (ingredient == null)
                    return "Namirnica više ne postoji u bazi.";

                db.Repromaterijal.Remove(ingredient);
                await db.SaveChangesAsync();

                SelectedIngredient = null;
                await LoadIngredientsAsync();
                return null;
            }
            catch (DbUpdateException)
            {
                return "Namirnicu nije moguće obrisati. Moguće je da je povezana s normativima ili drugim evidencijama.";
            }
            catch (Exception ex)
            {
                return $"Brisanje namirnice nije uspjelo: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void ExportExcel(string filePath)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Namirnice");

            string[] headers = ["ID", "Namirnica", "Jedinica mjere", "Planska cijena", "Nabavna cijena", "Kalo (%)"];
            for (int column = 0; column < headers.Length; column++)
                sheet.Cell(1, column + 1).Value = headers[column];

            int row = 2;
            foreach (var ingredient in Ingredients)
            {
                sheet.Cell(row, 1).Value = ingredient.IdRepromaterijala ?? 0;
                sheet.Cell(row, 2).Value = ingredient.Repromaterijal ?? string.Empty;
                sheet.Cell(row, 3).Value = ingredient.JedinicaMjere ?? 0;
                sheet.Cell(row, 4).Value = ingredient.PlanskaCijena ?? 0m;
                sheet.Cell(row, 5).Value = ingredient.NabavnaCijena ?? 0m;
                sheet.Cell(row, 6).Value = ingredient.Kalo ?? 0m;
                row++;
            }

            sheet.Range(1, 1, 1, 6).Style.Font.Bold = true;
            sheet.Columns(1, 6).AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public async Task<(int Count, string? Error)> ImportExcelAsync(string filePath)
        {
            if (IsBusy)
                return (0, "Druga operacija je već u toku.");

            IsBusy = true;
            try
            {
                using var workbook = new XLWorkbook(filePath);
                var sheet = workbook.Worksheets.First();

                string sixthHeader = sheet.Cell(1, 6).GetString().Trim();
                bool hasKalo = sixthHeader.Equals("Kalo (%)", StringComparison.OrdinalIgnoreCase)
                    || sixthHeader.Equals("Kalo", StringComparison.OrdinalIgnoreCase);

                bool oldSixColumnFormat = !hasKalo && string.IsNullOrWhiteSpace(sixthHeader)
                    && sheet.RowsUsed().Skip(1).Any(x => !x.Cell(6).IsEmpty());

                int plannedColumn = oldSixColumnFormat ? 5 : 4;
                int purchaseColumn = oldSixColumnFormat ? 6 : 5;

                var imported = new List<TblRepromaterijal>();
                var names = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

                await using var db = new AppDbContext();
                var existingNames = await db.Repromaterijal.AsNoTracking()
                    .Select(x => x.Repromaterijal).ToListAsync();

                foreach (var existingName in existingNames)
                    if (!string.IsNullOrWhiteSpace(existingName))
                        names.Add(existingName.Trim());

                foreach (var row in sheet.RowsUsed().Skip(1))
                {
                    string name = row.Cell(2).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (!names.Add(name))
                        return (0, $"Red {row.RowNumber()}: namirnica '{name}' već postoji ili se ponavlja u fajlu.");

                    int unit = row.Cell(3).GetValue<int>();
                    if (!JediniceMjere.Any(x => x.IdJedinice == unit))
                        return (0, $"Red {row.RowNumber()}: nepoznata jedinica mjere ({unit}).");

                    decimal planned = row.Cell(plannedColumn).GetValue<decimal>();
                    decimal purchase = row.Cell(purchaseColumn).GetValue<decimal>();
                    decimal kalo = hasKalo ? row.Cell(6).GetValue<decimal>() : 0m;

                    if (planned < 0 || purchase < 0)
                        return (0, $"Red {row.RowNumber()}: cijene ne mogu biti negativne.");

                    if (kalo > 100)
                        return (0, $"Red {row.RowNumber()}: kalo ne može biti veći od 100%.");

                    imported.Add(new TblRepromaterijal
                    {
                        Repromaterijal = name,
                        JedinicaMjere = unit,
                        PlanskaCijena = planned,
                        NabavnaCijena = purchase,
                        Kalo = kalo
                    });
                }

                if (imported.Count == 0)
                    return (0, "Excel fajl ne sadrži namirnice za uvoz.");

                await db.Repromaterijal.AddRangeAsync(imported);
                await db.SaveChangesAsync();
                await LoadIngredientsAsync();
                return (imported.Count, null);
            }
            catch (Exception ex)
            {
                return (0, $"Uvoz nije uspio: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
