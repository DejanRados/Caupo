
using Caupo.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using SearchSuggestion = Caupo.UserControls.SearchSuggestion;

using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class CategoriesViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }

        // ============================================================
        // KOLEKCIJE
        // ============================================================

        public ObservableCollection<TblKategorije> Kategorije { get; } = new();

        private ObservableCollection<TblKategorije> _kategorijeFilter = new();


        public ObservableCollection<TblKategorije> KategorijeFilter
        {
            get => _kategorijeFilter;
            private set
            {
                _kategorijeFilter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SearchSuggestions));
            }
        }


        public IEnumerable<SearchSuggestion> SearchSuggestions =>
            KategorijeFilter.Select(kategorija => new SearchSuggestion
            {
                Title = kategorija.Kategorija ?? string.Empty,
                Subtitle = kategorija.VrstaArtiklaName ?? string.Empty,
                Item = kategorija
            });


        // ============================================================
        // SELEKTOVANA KATEGORIJA
        // ============================================================

        private TblKategorije? _selectedKategorija;

        public TblKategorije? SelectedKategorija
        {
            get => _selectedKategorija;
            set
            {
                if (_selectedKategorija == value)
                    return;

                _selectedKategorija = value;
                OnPropertyChanged();

                _editCommand.RaiseCanExecuteChanged();
                _deleteCommand.RaiseCanExecuteChanged();
            }
        }

        // ============================================================
        // KATEGORIJA ZA UREĐIVANJE
        // ============================================================

        private TblKategorije _editKategorija = new();

        public TblKategorije EditKategorija
        {
            get => _editKategorija;
            set
            {
                _editKategorija = value;
                OnPropertyChanged();
            }
        }

        // ============================================================
        // POPUP
        // ============================================================

        private bool _isPopupOpen;

        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set
            {
                if (_isPopupOpen == value)
                    return;

                _isPopupOpen = value;
                OnPropertyChanged();
            }
        }

        // ============================================================
        // PRETRAGA
        // ============================================================

        private string? _searchText;

        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                    return;

                _searchText = value;
                OnPropertyChanged();

                FilterItems(value);
            }
        }

        public void FilterItems(string? searchText)
        {
            string query = (searchText ?? string.Empty).Trim();

            var filtered = Kategorije
                .Where(k =>
                    string.IsNullOrWhiteSpace(query) ||
                    (k.Kategorija?.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (k.VrstaArtiklaName?.Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase) ?? false) ||
                    k.VrstaArtikla.ToString().Contains(
                        query,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            KategorijeFilter =
                new ObservableCollection<TblKategorije>(filtered);
        }

        // ============================================================
        // KOMANDE
        // ============================================================

        public ICommand AddCommand { get; }
        public ICommand EditCommand => _editCommand;
        public ICommand DeleteCommand => _deleteCommand;
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ICommand FirstCategoryCommand { get; }
        public ICommand PreviousCategoryCommand { get; }
        public ICommand NextCategoryCommand { get; }
        public ICommand LastCategoryCommand { get; }

        private readonly SimpleCommand _editCommand;
        private readonly SimpleCommand _deleteCommand;

        // ============================================================
        // KONSTRUKTOR
        // ============================================================

        public CategoriesViewModel()
        {
            AddCommand = new SimpleCommand(_ => OpenAdd());
            SaveCommand = new SimpleCommand(_ => Save());
            CancelCommand = new SimpleCommand(_ => IsPopupOpen = false);

            _editCommand = new SimpleCommand(
                _ => OpenEdit(),
                _ => SelectedKategorija != null);

            _deleteCommand = new SimpleCommand(
                _ => Delete(),
                _ => SelectedKategorija != null);

            FirstCategoryCommand =
                new SimpleCommand(_ => NavigateToCategory(0));

            PreviousCategoryCommand =
                new SimpleCommand(_ => NavigateCategory(-1));

            NextCategoryCommand =
                new SimpleCommand(_ => NavigateCategory(1));

            LastCategoryCommand =
                new SimpleCommand(_ => NavigateToCategory(-1));

            LoadKategorije();
        }

        // ============================================================
        // UČITAVANJE KATEGORIJA
        // ============================================================

        private void LoadKategorije()
        {
            using var db = new AppDbContext();

            var lista = db.Kategorije
                .OrderBy(k => k.VrstaArtikla)
                .ThenBy(k => k.Kategorija)
                .ToList();

            Kategorije.Clear();

            foreach (var kategorija in lista)
                Kategorije.Add(kategorija);

            FilterItems(SearchText);

            SelectedKategorija = KategorijeFilter.FirstOrDefault();
        }

        // ============================================================
        // NAVIGACIJA
        // ============================================================

        private void NavigateToCategory(int position)
        {
            if (KategorijeFilter.Count == 0)
                return;

            int index = position == -1
                ? KategorijeFilter.Count - 1
                : 0;

            SelectedKategorija = KategorijeFilter[index];
        }

        private void NavigateCategory(int direction)
        {
            if (KategorijeFilter.Count == 0)
                return;

            int index = KategorijeFilter.IndexOf(SelectedKategorija);

            if (index < 0)
            {
                SelectedKategorija = direction > 0
                    ? KategorijeFilter[0]
                    : KategorijeFilter[KategorijeFilter.Count - 1];

                return;
            }

            int next = index + direction;

            if (next < 0 || next >= KategorijeFilter.Count)
                return;

            SelectedKategorija = KategorijeFilter[next];
        }

        // ============================================================
        // DODAVANJE
        // ============================================================

        private void OpenAdd()
        {
            EditKategorija = new TblKategorije();
            IsPopupOpen = true;
        }

        // ============================================================
        // IZMJENA
        // ============================================================

        private void OpenEdit()
        {
            if (SelectedKategorija == null)
                return;

            EditKategorija = new TblKategorije
            {
                IdKategorije = SelectedKategorija.IdKategorije,
                Kategorija = SelectedKategorija.Kategorija,
                VrstaArtikla = SelectedKategorija.VrstaArtikla
            };

            IsPopupOpen = true;
        }

        // ============================================================
        // SPREMANJE
        // ============================================================

        private void Save()
        {
            using var db = new AppDbContext();

            if (EditKategorija.IdKategorije == 0)
            {
                db.Kategorije.Add(EditKategorija);
                db.SaveChanges();

                Kategorije.Add(EditKategorija);

                SelectedKategorija = EditKategorija;
            }
            else
            {
                var dbItem = db.Kategorije
                    .FirstOrDefault(x =>
                        x.IdKategorije == EditKategorija.IdKategorije);

                if (dbItem != null)
                {
                    dbItem.Kategorija = EditKategorija.Kategorija;
                    dbItem.VrstaArtikla = EditKategorija.VrstaArtikla;

                    db.SaveChanges();

                    var original = Kategorije.FirstOrDefault(x =>
                        x.IdKategorije == EditKategorija.IdKategorije);

                    if (original != null)
                    {
                        original.Kategorija = EditKategorija.Kategorija;
                        original.VrstaArtikla = EditKategorija.VrstaArtikla;

                        SelectedKategorija = original;
                    }
                }
            }

            IsPopupOpen = false;

            var selected = SelectedKategorija;

            FilterItems(SearchText);

            if (selected != null &&
                KategorijeFilter.Contains(selected))
            {
                SelectedKategorija = selected;
            }
            else
            {
                SelectedKategorija = KategorijeFilter.FirstOrDefault();
            }
        }

        // ============================================================
        // BRISANJE
        // ============================================================

        private void Delete()
        {
            if (SelectedKategorija == null)
                return;

            var kategorija = SelectedKategorija;

            using var db = new AppDbContext();

            var dbItem = db.Kategorije
                .FirstOrDefault(x =>
                    x.IdKategorije == kategorija.IdKategorije);

            if (dbItem != null)
            {
                db.Kategorije.Remove(dbItem);
                db.SaveChanges();
            }

            Kategorije.Remove(kategorija);

            FilterItems(SearchText);

            SelectedKategorija = KategorijeFilter.FirstOrDefault();
        }

        // ============================================================
        // SIMPLE COMMAND
        // ============================================================

        private class SimpleCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public SimpleCommand(
                Action<object?> execute,
                Predicate<object?>? canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter)
            {
                return _canExecute?.Invoke(parameter) ?? true;
            }

            public void Execute(object? parameter)
            {
                _execute(parameter);
            }

            public event EventHandler? CanExecuteChanged;

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
