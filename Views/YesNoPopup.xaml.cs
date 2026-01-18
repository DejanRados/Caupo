using Caupo.Properties;
using System.Windows;
using System.Windows.Media;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for YesNoPopup.xaml
    /// </summary>
    public partial class YesNoPopup : Window
    {
        public string ImagePath { get; set; }
        public Brush? FontColor { get; set; }
        public string Kliknuo { get; private set; } = "Ne";
        public YesNoPopup()
        {
            InitializeComponent ();
            string tema = Settings.Default.Tema;
            this.DataContext = this;
            if(tema == "Tamna")
            {
                ImagePath = "pack://application:,,,/Images/Dark/info.png";
                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
            }
            else
            {
                ImagePath = "pack://application:,,,/Images/Light/info.png";
                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
            }

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Kliknuo = "Ne";
            this.DialogResult = false;

        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Kliknuo = "Da";
            this.DialogResult = true;

        }
    }
}
