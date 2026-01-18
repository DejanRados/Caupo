using Caupo.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{

    public class CategoriesViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string prop)
            => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (prop));

        // ================== PROPERTIES ==================

        public ObservableCollection<TblKategorije> Kategorije { get; } = new ();

        private TblKategorije? _selectedKategorija;
        public TblKategorije? SelectedKategorija
        {
            get => _selectedKategorija;
            set
            {
                _selectedKategorija = value;
                OnPropertyChanged (nameof (SelectedKategorija));

                _editCommand.RaiseCanExecuteChanged ();
                _deleteCommand.RaiseCanExecuteChanged ();
            }
        }


        private TblKategorije _editKategorija = new ();
        public TblKategorije EditKategorija
        {
            get => _editKategorija;
            set
            {
                _editKategorija = value;
                OnPropertyChanged (nameof (EditKategorija));
            }
        }

        private bool _isPopupOpen;
        public bool IsPopupOpen
        {
            get => _isPopupOpen;
            set
            {
                _isPopupOpen = value;
                OnPropertyChanged (nameof (IsPopupOpen));
            }
        }

        // ================== COMMANDS ==================

        public ICommand AddCommand { get; }
        public ICommand EditCommand => _editCommand;
        public ICommand DeleteCommand => _deleteCommand;
        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        private SimpleCommand _editCommand;
        private SimpleCommand _deleteCommand;



        // ================== CTOR ==================

        public CategoriesViewModel()
        {
            AddCommand = new SimpleCommand (_ => OpenAdd ());
            SaveCommand = new SimpleCommand (_ => Save ());
            CancelCommand = new SimpleCommand (_ => IsPopupOpen = false);

            _editCommand = new SimpleCommand (_ => OpenEdit (), _ => SelectedKategorija != null);
            _deleteCommand = new SimpleCommand (_ => Delete (), _ => SelectedKategorija != null);

            LoadKategorije ();
        }

        // ================== LOGIC ==================


        private void LoadKategorije()
        {
            using var db = new AppDbContext ();

            var lista = db.Kategorije.OrderBy (k => k.VrstaArtikla).ToList ();

            Kategorije.Clear ();
            foreach(var k in lista)
                Kategorije.Add (k);
        }

        private void OpenAdd()
        {
            EditKategorija = new TblKategorije ();
            IsPopupOpen = true;
        }

        private void OpenEdit()
        {
            if(SelectedKategorija == null)
                return;

            EditKategorija = new TblKategorije
            {
                IdKategorije = SelectedKategorija.IdKategorije,
                Kategorija = SelectedKategorija.Kategorija,
                VrstaArtikla = SelectedKategorija.VrstaArtikla
            };
            IsPopupOpen = true;
        }

        private void Save()
        {
            using var db = new AppDbContext ();

            if(EditKategorija.IdKategorije == 0)
            {
                db.Kategorije.Add (EditKategorija);
                db.SaveChanges ();

                Kategorije.Add (EditKategorija); // UI
            }
            else
            {
                var dbItem = db.Kategorije
                               .FirstOrDefault (x => x.IdKategorije == EditKategorija.IdKategorije);

                if(dbItem != null)
                {
                    dbItem.Kategorija = EditKategorija.Kategorija;
                    dbItem.VrstaArtikla = EditKategorija.VrstaArtikla;

                    db.SaveChanges ();

                    // UI refresh
                    SelectedKategorija.Kategorija = EditKategorija.Kategorija;
                    SelectedKategorija.VrstaArtikla = EditKategorija.VrstaArtikla;
                }
            }

            IsPopupOpen = false;
        }


        private void Delete()
        {
            if(SelectedKategorija == null)
                return;

            using var db = new AppDbContext ();
            var dbItem = db.Kategorije
                           .FirstOrDefault (x => x.IdKategorije == SelectedKategorija.IdKategorije);

            if(dbItem != null)
            {
                db.Kategorije.Remove (dbItem);
                db.SaveChanges ();
            }

            Kategorije.Remove (SelectedKategorija);

            SelectedKategorija = null;
        }


        // ================== COMMAND IMPLEMENTATION ==================

        private class SimpleCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public SimpleCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter)
                => _canExecute?.Invoke (parameter) ?? true;

            public void Execute(object? parameter)
                => _execute (parameter);

            public event EventHandler? CanExecuteChanged;

            public void RaiseCanExecuteChanged()
                => CanExecuteChanged?.Invoke (this, EventArgs.Empty);
        }

    }
}
