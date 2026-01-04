
using Caupo.Data;
using Caupo.Helpers;
using Caupo.Models;
using Caupo.Services;
using Caupo.ViewModels;
using Microsoft.EntityFrameworkCore;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static Caupo.Data.DatabaseTables;
using static System.Net.Mime.MediaTypeNames;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for KasaPage.xaml
    /// </summary>
    public partial class KnjigaKuhinjePage : UserControl
    {
        public KnjigaKuhinjePage()
        {
            InitializeComponent();
            var db = new AppDbContext();
            var service = new KnjigaKuhinjeService(db);
            DataContext = new KnjigaKuhinjeViewModel(service);

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
            myMessageBox.ShowDialog();
        }

        private async void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is KnjigaKuhinjeViewModel viewModel)
            {
                viewModel.OdabraniDatum = viewModel.OdabraniDatum.AddDays(-1);
                await viewModel.GetJelaZaOdabraniDatumAsync();
            }
        }

        private async void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is KnjigaKuhinjeViewModel viewModel)
            {
                viewModel.OdabraniDatum = viewModel.OdabraniDatum.AddDays(1);
               await viewModel.GetJelaZaOdabraniDatumAsync();
            }
        }


        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is KnjigaKuhinjeViewModel viewModel)
            {
                viewModel.PrintReport(viewModel.Knjiga, viewModel.Firma.NazivFirme, viewModel.Firma.Adresa, viewModel.Firma.Grad, viewModel.Firma.JIB, viewModel.Firma.PDV, viewModel.OdabraniDatum, viewModel.Total);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
        }

        private async void DpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is KnjigaKuhinjeViewModel viewModel)
            {
                //viewModel.OdabraniDatum = viewModel.OdabraniDatum.AddDays (1);
                await viewModel.GetJelaZaOdabraniDatumAsync ();
            }
        }
    }
}
