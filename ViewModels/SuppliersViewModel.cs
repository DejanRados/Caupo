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
using System.Windows.Input;
using static Caupo.Data.DatabaseTables;


namespace Caupo.ViewModels
{
    public class SuppliersViewModel : INotifyPropertyChanged
    {

        public class StockInList
        {
            public int BrojUlaza { get; set; }
            public DateTime Datum { get; set; }
            public string Tip { get; set; }
            public string BrojFakture { get; set; }
            public string Radnik { get; set; }
            public string RadnikName { get; set; }
        }


        public ObservableCollection<DatabaseTables.TblDobavljaci> Suppliers { get; set; } = new ObservableCollection<DatabaseTables.TblDobavljaci> ();
        public ObservableCollection<StockInList> StockIn { get; set; } = new ObservableCollection<StockInList> ();


        private StockInList? _selectedStockIn;
        public StockInList? SelectedStockIn
        {
            get => _selectedStockIn;
            set
            {
                if(_selectedStockIn != value)
                {
                    _selectedStockIn = value;
                    OnPropertyChanged (nameof (SelectedStockIn));

                }
            }
        }

        private DatabaseTables.TblDobavljaci? _selectedSupplier;
        public DatabaseTables.TblDobavljaci? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                if(_selectedSupplier != value)
                {
                    _selectedSupplier = value;
                    OnPropertyChanged (nameof (SelectedSupplier));
                    _ = LoadStockInSupplier (value);
                    //  (EditCommand as RelayCommand)?.NotifyCanExecuteChanged();
                    //  (DeleteCommand as RelayCommand)?.NotifyCanExecuteChanged();

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



        /*   private ObservableCollection<TblDobavljaci>? suppliersFilter;
           public ObservableCollection<TblDobavljaci>? SuppliersFilter
           {
               get => suppliersFilter;
               set
               {
                   suppliersFilter = value;
                   OnPropertyChanged(nameof(SuppliersFilter));
               }
           }*/

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

        private TblDobavljaci _newSupplier;
        public TblDobavljaci NewSupplier
        {
            get => _newSupplier;
            set
            {
                _newSupplier = value;
                OnPropertyChanged ();
            }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public ICollectionView SuppliersFilter { get; private set; }

        public event Func<TblDobavljaci?, TblDobavljaci?>? OpenSupplierPopupRequested;
        public SuppliersViewModel()
        {


            Suppliers = new ObservableCollection<TblDobavljaci> ();
            //SuppliersFilter = new ObservableCollection<TblDobavljaci>();
            SuppliersFilter = CollectionViewSource.GetDefaultView (Suppliers);
            StockIn = new ObservableCollection<StockInList> ();

            AddCommand = new RelayCommand (AddDobavljac);
            EditCommand = new RelayCommand (EditDobavljac, () => SelectedSupplier != null);
            DeleteCommand = new RelayCommand (DeleteDobavljac, () => SelectedSupplier != null);
            Start ();
            Debug.WriteLine ("SearchText = " + SearchText);
        }

        // ADD
        private async void AddDobavljac()
        {
            //var popup = new SupplierPopup ();
            //var novi = popup.Open ();

            var novi = OpenSupplierPopupRequested?.Invoke (null);
            if(novi != null)
            {
                Debug.WriteLine ("Popup vratio novog dobavljaca: " + novi.Dobavljac);
                using var db = new AppDbContext ();
                await db.Dobavljaci.AddAsync (novi);
                await db.SaveChangesAsync ();
                Debug.WriteLine ("await db.Dobavljaci.AddAsync(novi); " + novi.Dobavljac);
                Suppliers.Add (novi);
                Debug.WriteLine (" Suppliers.Add(novi); " + novi.Dobavljac);
                SelectedSupplier = novi;
                NewSupplier = novi;
            }
        }

        // EDIT
        private async void EditDobavljac()
        {
            if(SelectedSupplier == null )
                return;

            //var popup = new SupplierPopup (SelectedSupplier);
            //var editovani = popup.Open ();

            var editovani = OpenSupplierPopupRequested?.Invoke (SelectedSupplier);
            //Debug.WriteLine("Popup vratio editovanog dobavljaca: " + editovani.IdDobavljaca);
            if(editovani != null)
            {
                using var db = new AppDbContext ();
                db.Dobavljaci.Update (editovani);
                await db.SaveChangesAsync ();
                Debug.WriteLine ("db.Dobavljaci.Update(editovani);: " + editovani.Dobavljac);
                var item = Suppliers.First (x => x.IdDobavljaca == editovani.IdDobavljaca);
                var index = Suppliers.IndexOf (item);
                Suppliers[index] = editovani;

            }
        }

        // DELETE
        private async void DeleteDobavljac()
        {
            if(SelectedSupplier == null)
                return;
            if(StockIn.Any ())
            {
                MyMessageBox myMessage = new MyMessageBox ();
                myMessage.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                myMessage.MessageTitle.Text = "Brisanje nije dozvoljeno";
                myMessage.MessageText.Text = "Dobavljač " + SelectedSupplier.Dobavljac + Environment.NewLine + " se ne može obrisati jer postoje ulazi povezani s njim.";
                myMessage.ShowDialog ();

                return;
            }
            var myMessageBox = new YesNoPopup ();
            myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
            myMessageBox.MessageText.Text = "Da li ste sigurni da želite brisati dobavljača" + Environment.NewLine + SelectedSupplier.Dobavljac;
            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            bool? result = myMessageBox.ShowDialog ();

            if(myMessageBox.Kliknuo == "Da")
            {
                using var db = new AppDbContext ();
                // Load the entity fresh from the database
                var entity = await db.Dobavljaci.FirstOrDefaultAsync (x => x.IdDobavljaca == SelectedSupplier.IdDobavljaca);
                if(entity != null)
                {
                    db.Dobavljaci.Remove (entity);
                    await db.SaveChangesAsync ();

                    var toRemove = Suppliers.FirstOrDefault (x => x.IdDobavljaca == SelectedSupplier.IdDobavljaca);
                    if(toRemove != null)
                    {
                        Suppliers.Remove (toRemove);
                        SelectedSupplier = Suppliers.FirstOrDefault ();
                    }
                }
            }

        }
        private async void Start()
        {

            await LoadSuppliersAsync ();
            await LoadFirmaAsync ();

        }

        public async Task LoadStockInSupplier(TblDobavljaci selected)
        {
            StockIn.Clear ();
            Debug.WriteLine ("LoadStockInSupplier started");

            if(selected == null)
            {
                Debug.WriteLine ("Selected supplier is null");
                return;
            }

            using var db = new AppDbContext ();

            // Ulazi pića
            var pica = await db.Ulaz
                .Where (x => x.Dobavljac == selected.Dobavljac)
                .Select (p => new StockInList
                {
                    BrojUlaza = p.BrojUlaza,
                    Datum = p.Datum,
                    Tip = "Piće",
                    BrojFakture = p.BrojFakture,
                    RadnikName = db.Radnici
                        .Where (rad => rad.IdRadnika.ToString () == p.Radnik)
                        .Select (rad => rad.Radnik)
                        .FirstOrDefault () ?? string.Empty
                })
                .ToListAsync ();

            foreach(var p in pica)
            {
                StockIn.Add (p);
                Debug.WriteLine ($"Piće: BrojUlaza={p.BrojUlaza}, RadnikName={p.RadnikName}");
            }

            // Ulazi namirnica
            var namirnice = await db.UlazRepromaterijal
                .Where (x => x.Dobavljac == selected.Dobavljac)
                .Select (n => new StockInList
                {
                    BrojUlaza = n.BrojUlaza,
                    Datum = n.Datum,
                    Tip = "Namirnice",
                    BrojFakture = n.BrojFakture,
                    RadnikName = db.Radnici
                        .Where (rad => rad.IdRadnika.ToString () == n.Radnik)
                        .Select (rad => rad.Radnik)
                        .FirstOrDefault () ?? string.Empty
                })
                .ToListAsync ();

            foreach(var n in namirnice)
            {
                StockIn.Add (n);
                Debug.WriteLine ($"Namirnice: BrojUlaza={n.BrojUlaza}, RadnikName={n.RadnikName}");
            }

            Debug.WriteLine ($"LoadStockInSupplier finished. Total items: {StockIn.Count}");
        }



        public async Task<decimal> IznosRacuna(int brojRacuna)
        {
            using var db = new AppDbContext ();

            return await db.RacunStavka
                .Where (x => x.BrojRacuna == brojRacuna)
                .SumAsync (x => x.Kolicina * x.Cijena) ?? 0;
        }





        public event EventHandler<string?>? ErrorOccurred;
        protected virtual void OnErrorOccurred(string? message)
        {
            ErrorOccurred?.Invoke (this, message);
        }


        public async Task LoadFirmaAsync()
        {
            try
            {
                using var db = new AppDbContext ();
                Firma = await db.Firma.FirstOrDefaultAsync ();
                Debug.WriteLine ("Firma učitana: " + (Firma?.NazivFirme ?? "null"));
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex);
            }
        }
        public async Task LoadSuppliersAsync()
        {
            try
            {

                using(var db = new AppDbContext ())
                {
                    var buyers = await db.Dobavljaci.ToListAsync ();
                    Suppliers.Clear ();
                    //SuppliersFilter?.Clear();
                    foreach(var buyer in buyers)
                    {

                        Suppliers.Add (buyer);
                        //SuppliersFilter?.Add(buyer);

                    }
                    SelectedSupplier = Suppliers?.FirstOrDefault ();
                    await LoadStockInSupplier (SelectedSupplier);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }
        public void FilterItems(string? searchtext)
        {
            Debug.WriteLine ("Okida search");

            var lowerSearch = (searchtext ?? string.Empty).ToLower ();

            SuppliersFilter.Filter = obj =>
            {
                if(obj is TblDobavljaci d)
                {
                    return (d.JIB?.ToLower ().Contains (lowerSearch) ?? false) ||
                           (d.Dobavljac?.ToLower ().Contains (lowerSearch) ?? false) ||
                           (d.PDV?.ToLower ().Contains (lowerSearch) ?? false) ||
                           (d.Mjesto?.ToLower ().Contains (lowerSearch) ?? false);
                }
                return false;
            };

            SuppliersFilter.Refresh (); // osvježava view
        }



        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }
    }
}
