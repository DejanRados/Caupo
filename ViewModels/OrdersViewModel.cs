using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Properties;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class OrdersViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<FiskalniRacun.Item> _stavkeRacuna = new ObservableCollection<FiskalniRacun.Item> ();
        public ObservableCollection<FiskalniRacun.Item> StavkeRacuna
        {
            get => _stavkeRacuna;
            set
            {
                _stavkeRacuna = value;
                OnPropertyChanged (nameof (StavkeRacuna));


            }
        }

        private ObservableCollection<DatabaseTables.TblNarudzbe> _narudzbe = new ObservableCollection<TblNarudzbe> ();

        public ObservableCollection<DatabaseTables.TblNarudzbe> Narudzbe
        {
            get => _narudzbe;
            set
            {
                _narudzbe = value;
                OnPropertyChanged (nameof (Narudzbe));


            }
        }

        private ObservableCollection<DatabaseTables.TblNarudzbeStavke> _narudzbeStavke = new ObservableCollection<TblNarudzbeStavke> ();

        public ObservableCollection<DatabaseTables.TblNarudzbeStavke> NarudzbeStavke
        {
            get => _narudzbeStavke;
            set
            {
                _narudzbeStavke = value;
                OnPropertyChanged (nameof (NarudzbeStavke));


            }
        }


        private string? _imagePathSaveButton;
        public string? ImagePathSaveButton
        {
            get { return _imagePathSaveButton; }
            set
            {
                _imagePathSaveButton = value;
                OnPropertyChanged (nameof (ImagePathSaveButton));
            }
        }

        private string? _imagePathDeleteButton;
        public string? ImagePathDeleteButton
        {
            get { return _imagePathDeleteButton; }
            set
            {
                _imagePathDeleteButton = value;
                OnPropertyChanged (nameof (ImagePathDeleteButton));
            }
        }

        private Brush? _fontColor;
        public Brush? FontColor
        {
            get { return _fontColor; }
            set
            {
                if(_fontColor != value)
                {
                    _fontColor = value;
                    OnPropertyChanged (nameof (FontColor));
                }
            }
        }

        private Brush? _backColor;
        public Brush? BackColor
        {
            get { return _backColor; }
            set
            {
                if(_backColor != value)
                {
                    _backColor = value;
                    OnPropertyChanged (nameof (BackColor));
                }
            }
        }

        private int? _idStola;
        public int? IdStola
        {
            get { return _idStola; }
            set
            {
                if(_idStola != value)
                {
                    _idStola = value;
                    OnPropertyChanged (nameof (IdStola));
                }
            }
        }
        private string? _imeStola;
        public string? ImeStola
        {
            get { return _imeStola; }
            set
            {
                if(_imeStola != value)
                {
                    _imeStola = value;
                    OnPropertyChanged (nameof (ImeStola));
                }
            }
        }

        private string? _sala;
        public string? Sala
        {
            get { return _sala; }
            set
            {
                if(_sala != value)
                {
                    _sala = value;
                    OnPropertyChanged (nameof (Sala));
                }
            }
        }



        private KasaViewModel _kasaViewModel;
        public OrdersViewModel(KasaViewModel kasaViewModel)
        {

            _kasaViewModel = kasaViewModel;
            if(kasaViewModel != null)
            {
                StavkeRacuna = _kasaViewModel.StavkeRacuna ?? new ObservableCollection<FiskalniRacun.Item> ();

                Debug.WriteLine (" Prenesene StavkeRacuna iz kase --- " + StavkeRacuna.Count);
            }
            else
            {

                StavkeRacuna = new ObservableCollection<FiskalniRacun.Item> ();
            }
            SetColors ();

        }




        public void SetColors()
        {

            string tema = Settings.Default.Tema;
            Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema);
            if(tema == "Tamna")
            {
                ImagePathSaveButton = "pack://application:,,,/Images/Dark/save.png";
                ImagePathDeleteButton = "pack://application:,,,/Images/Dark/delete.png";
                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
                BackColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));


            }
            else
            {
                ImagePathSaveButton = "pack://application:,,,/Images/Light/save.png";
                ImagePathDeleteButton = "pack://application:,,,/Images/Light/delete.png";
                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
                BackColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));
                //FontColorAdv = new System.Windows.Media.Color();
                //FontColorAdv = System.Windows.Media.Color.FromRgb(50, 50, 50);
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }
    }


}
