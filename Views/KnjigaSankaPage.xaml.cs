
using Caupo.Data;
using Caupo.Helpers;
using Caupo.Services;
using Caupo.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for KasaPage.xaml
    /// </summary>
    public partial class KnjigaSankaPage : UserControl
    {
        public KnjigaSankaPage()
        {
            InitializeComponent ();
            var db = new AppDbContext ();
            var service = new KnjigaSankaService (db);
            DataContext = new KnjigaSankaViewModel (service);

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage ();
            page.DataContext = new HomeViewModel ();
            PageNavigator.NavigateWithFade (page);
        }




        private void ShowMessage(string title, string message)
        {
            var myMessageBox = new MyMessageBox
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            myMessageBox.MessageTitle.Text = title;
            myMessageBox.MessageText.Text = message;
            myMessageBox.ShowDialog ();
        }

        private void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is KnjigaSankaViewModel viewModel)
            {
                viewModel.OdabraniDatum = viewModel.OdabraniDatum.AddDays (-1);
            }
        }

        private void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is KnjigaSankaViewModel viewModel)
            {
                viewModel.OdabraniDatum = viewModel.OdabraniDatum.AddDays (1);
            }
        }


        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is KnjigaSankaViewModel viewModel)
            {
                viewModel.PrintReport (viewModel.Knjiga, viewModel.Firma.NazivFirme, viewModel.Firma.Adresa, viewModel.Firma.Grad, viewModel.Firma.JIB, viewModel.Firma.PDV, viewModel.OdabraniDatum, viewModel.Total);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
        }
    }
}
