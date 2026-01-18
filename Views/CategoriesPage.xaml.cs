using Caupo.Helpers;
using Caupo.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for CategoriesPage.xaml
    /// </summary>
    public partial class CategoriesPage : UserControl
    {
        public CategoriesPage()
        {
            InitializeComponent ();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new ArticlesPage ();
            page.DataContext = new ArticlesViewModel ();
            PageNavigator.NavigateWithFade (page);
        }
    }
}
