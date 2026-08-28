using Caupo.Helpers;
using Caupo.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent ();

            DataContext = new SettingsViewModel ();

            lblUlogovaniKorisnik.Content =
                Globals.ulogovaniKorisnik?.Radnik ?? string.Empty;
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var page = new HomePage
            {
                DataContext = new HomeViewModel ()
            };

            PageNavigator.NavigateWithFade (page);
        }
    }
}