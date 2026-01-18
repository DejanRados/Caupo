using Caupo.Properties;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class SupplierPopupViewModel : INotifyPropertyChanged
    {
        public TblDobavljaci Dobavljac { get; set; }
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

        public SupplierPopupViewModel()
        {
            SetImage ();
            Dobavljac = new TblDobavljaci ();

        }

        public SupplierPopupViewModel(TblDobavljaci d)
        {
            SetImage ();
            Dobavljac = d;
            /*  Dobavljac = new TblDobavljaci
              {
                  IdDobavljaca = d.IdDobavljaca,
                  Dobavljac = d.Dobavljac,
                  Adresa = d.Adresa,
                  Mjesto = d.Mjesto,
                  JIB = d.JIB,
                  PDV = d.PDV
              };*/

        }

        public async Task SetImage()
        {
            await Task.Delay (1);

            string tema = Settings.Default.Tema;
            Debug.WriteLine ("Aktivna tema koju vidi viewmodel popup dobavljac je : " + tema);
            if(tema == "Tamna")
            {

                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
                Debug.WriteLine ("Tema tamna, FontColor  je : " + FontColor.ToString ());
            }
            else
            {
                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
                Debug.WriteLine ("Tema svijetla, FontColor  je : " + FontColor.ToString ());
            }

        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }

    }

}
