
using Caupo.Data;
using Caupo.Helpers;
using Caupo.Services;
using Caupo.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Caupo.Views
{
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
            var page = new HomePage
            {
                DataContext = new HomeViewModel()
            };

            PageNavigator.NavigateWithFade(page);
        }

        private void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is KnjigaKuhinjeViewModel vm)
                vm.OdabraniDatum = vm.OdabraniDatum.AddDays(-1);
        }

        private void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is KnjigaKuhinjeViewModel vm)
                vm.OdabraniDatum = vm.OdabraniDatum.AddDays(1);
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is KnjigaKuhinjeViewModel vm)
                vm.PrintReport();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void DpDate_SelectedDateChanged(
            object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
