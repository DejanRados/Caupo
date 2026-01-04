using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{


    public class BuyerPopupViewModel : IDataErrorInfo, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }

        private string? _kupac;
        public string? Kupac
        {
            get => _kupac;
            set { _kupac = value; OnPropertyChanged (); }
        }

        private string? _adresa;
        public string? Adresa
        {
            get => _adresa;
            set { _adresa = value; OnPropertyChanged (); }
        }

        private string? _mjesto;
        public string? Mjesto
        {
            get => _mjesto;
            set { _mjesto = value; OnPropertyChanged (); }
        }

        private string? _pdv;
        public string? PDV
        {
            get => _pdv;
            set { _pdv = value; OnPropertyChanged (); }
        }

        private string? _jib;
        public string? JIB
        {
            get => _jib;
            set { _jib = value; OnPropertyChanged (); }
        }

        public bool IsValid => string.IsNullOrWhiteSpace (Error);

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                return columnName switch
                {
                    nameof (Kupac) => string.IsNullOrWhiteSpace (Kupac) ? "Ime kupca je obavezno" : null,
                    _ => null
                };
            }
        }

        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public TblKupci? Model { get; private set; }

        public BuyerPopupViewModel(TblKupci? kupac = null)
        {
            if (kupac != null)
            {
                Model = kupac;
                Kupac = kupac.Kupac;
                Adresa = kupac.Adresa;
                Mjesto = kupac.Mjesto;
                PDV = kupac.PDV;
                JIB = kupac.JIB;
            }

            SaveCommand = new RelayCommand<object> (o => Save (), o => IsValid);
            CancelCommand = new RelayCommand<object> (o => Cancel ());
        }

        private void Save()
        {
            if (Model == null) Model = new TblKupci ();
            Model.Kupac = Kupac;
            Model.Adresa = Adresa;
            Model.Mjesto = Mjesto;
            Model.PDV = PDV;
            Model.JIB = JIB;

            CloseRequested?.Invoke (this, true);
        }

        private void Cancel()
        {
            CloseRequested?.Invoke (this, false);
        }

        public event EventHandler<bool>? CloseRequested;
    }

}
