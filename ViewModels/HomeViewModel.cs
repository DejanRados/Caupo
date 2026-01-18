using Caupo.Properties;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace Caupo.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
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

        private string? imagePathCashRegisterButton;
        public string? ImagePathCashRegisterButton
        {
            get { return imagePathCashRegisterButton; }
            set
            {
                imagePathCashRegisterButton = value;
                OnPropertyChanged (nameof (ImagePathCashRegisterButton));
            }
        }

        private string? imagePathSuppliersButton;
        public string? ImagePathSuppliersButton
        {
            get { return imagePathSuppliersButton; }
            set
            {
                imagePathSuppliersButton = value;
                OnPropertyChanged (nameof (ImagePathSuppliersButton));
            }
        }

        private string? imagePathOrdersButton;
        public string? ImagePathOrdersButton
        {
            get { return imagePathOrdersButton; }
            set
            {
                imagePathOrdersButton = value;
                OnPropertyChanged (nameof (ImagePathOrdersButton));
            }
        }
        private string? imagePathReceiptsButton;
        public string? ImagePathReceiptsButton
        {
            get { return imagePathReceiptsButton; }
            set
            {
                imagePathReceiptsButton = value;
                OnPropertyChanged (nameof (ImagePathReceiptsButton));
            }
        }

        private string? imagePathSetupButton;
        public string? ImagePathSetupButton
        {
            get { return imagePathSetupButton; }
            set
            {
                imagePathSetupButton = value;
                OnPropertyChanged (nameof (ImagePathSetupButton));
            }
        }
        private string? imagePathIngredientsButton;
        public string? ImagePathIngredientsButton
        {
            get { return imagePathIngredientsButton; }
            set
            {
                imagePathIngredientsButton = value;
                OnPropertyChanged (nameof (ImagePathIngredientsButton));
            }
        }

        public HomeViewModel()
        {
            _ = InitializeAsync ();
        }

        private async Task InitializeAsync()
        {
            await SetImage ();
        }
        public async Task SetImage()
        {
            await Task.Delay (5);
            string tema = Settings.Default.Tema;
            Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema);
            if(tema == "Tamna")
            {
                Debug.WriteLine (" tema == Tamna ");
                ImagePathSuppliersButton = "pack://application:,,,/Images/Dark/supplier.svg";
                ImagePathCashRegisterButton = "pack://application:,,,/Images/Dark/cashregister.svg";
                ImagePathOrdersButton = "pack://application:,,,/Images/Dark/orders.svg";
                ImagePathReceiptsButton = "pack://application:,,,/Images/Dark/receipt.svg";
                ImagePathSetupButton = "pack://application:,,,/Images/Dark/setup.svg";
                ImagePathIngredientsButton = "pack://application:,,,/Images/Dark/ingredients.svg";
                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
                BackColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));


            }
            else
            {
                Debug.WriteLine (" tema == Svijetla ");
                ImagePathSuppliersButton = "pack://application:,,,/Images/Light/supplier.svg";
                ImagePathCashRegisterButton = "pack://application:,,,/Images/Light/cashregister.svg";
                ImagePathOrdersButton = "pack://application:,,,/Images/Light/orders.svg";
                ImagePathReceiptsButton = "pack://application:,,,/Images/Light/receipt.svg";
                ImagePathSetupButton = "pack://application:,,,/Images/Light/setup.svg";
                ImagePathIngredientsButton = "pack://application:,,,/Images/Light/ingredients.svg";
                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
                BackColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));

            }
            var brush = (SolidColorBrush)BackColor;
            Debug.WriteLine ($"BackColor: R={brush.Color.R}, G={brush.Color.G}, B={brush.Color.B}");
            Debug.WriteLine (" ImagePathIngredientsButton je : " + ImagePathIngredientsButton);
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? propertyName)
        {
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }

    }
}
