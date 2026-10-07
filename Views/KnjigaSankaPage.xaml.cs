using Caupo.Data;
using Caupo.Helpers;
using Caupo.Services;
using Caupo.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for KnjigaSankaPage.xaml
    /// </summary>
    public partial class KnjigaSankaPage : UserControl
    {
        public KnjigaSankaPage()
        {
            InitializeComponent();

            var db = new AppDbContext();
            var service = new KnjigaSankaService(db);

            DataContext = new KnjigaSankaViewModel(service);
        }


        #region NAVIGACIJA

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage();
            page.DataContext = new HomeViewModel();

            PageNavigator.NavigateWithFade(page);
        }

        #endregion


        #region UI

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           
        }

        #endregion
    }
}