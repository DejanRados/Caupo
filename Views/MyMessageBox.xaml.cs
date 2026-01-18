using System.Windows;
using System.Windows.Media;

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
            InitializeComponent ();

            this.DataContext = this;

        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close ();
        }
    }
}
