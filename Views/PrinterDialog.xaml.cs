using Caupo.Properties;
using System.Diagnostics;
using System.Printing;
using System.Windows;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for PrinterDialog.xaml
    /// </summary>
    public partial class PrinterDialog : Window
    {
        public string SelectedPrinter { get; private set; }
        public string ImagePath { get; set; }

        public PrinterDialog()
        {
            InitializeComponent ();
            this.DataContext = this;
            var printQueueCollection = new LocalPrintServer ().GetPrintQueues ();
            foreach(var printQueue in printQueueCollection)
            {
                cmbPrinters.Items.Add (printQueue.FullName);
            }
            string tema = Settings.Default.Tema;
            Debug.WriteLine ("Aktivna tema je : " + tema);
            if(tema == "Tamna")
            {
                ImagePath = "pack://application:,,,/Images/Dark/printer.png";

            }
            else
            {
                ImagePath = "pack://application:,,,/Images/Light/printer.png";

            }
            Debug.WriteLine ("Putanja slike: " + ImagePath);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close ();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if(cmbPrinters.SelectedItem != null)
            {
                SelectedPrinter = cmbPrinters.SelectedItem.ToString ();
                DialogResult = true;
                Close ();
            }
            else
            {
                MessageBox.Show ("Molimo odaberite štampač.", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
