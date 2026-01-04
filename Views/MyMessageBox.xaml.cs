using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Caupo.Properties;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for MyMessageBox.xaml
    /// </summary>
    public partial class MyMessageBox : Window
    {
        public string ImagePath { get; set; }
        public Brush? FontColor { get; set; }

        public Brush? BackColor { get; set; }

        public MyMessageBox()
        {
            InitializeComponent();
            string tema = Settings.Default.Tema;
            this.DataContext = this;
            if (tema == "Tamna")
            {
                ImagePath = "pack://application:,,,/Images/Dark/info.png";
                FontColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212));   Application.Current.Resources["GlobalFontColor"] =    FontColor;
                BackColor = new SolidColorBrush(Color.FromArgb(0xFF, 0x28, 0x28, 0x28));
            }
            else
            {
                ImagePath = "pack://application:,,,/Images/Light/info.png";
                FontColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));  Application.Current.Resources["GlobalFontColor"] =    FontColor;
                BackColor = new SolidColorBrush(Color.FromArgb(0xFF, 0xf8, 0xf8, 0xf8));
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
