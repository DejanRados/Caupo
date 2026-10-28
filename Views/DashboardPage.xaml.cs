
using Caupo.Helpers;
using Caupo.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Caupo.Views
{
    public partial class DashboardPage : UserControl
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardPage()
        {
            InitializeComponent();
            _viewModel = new DashboardViewModel();
            DataContext = _viewModel;
            Loaded += DashboardPage_Loaded;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= DashboardPage_Loaded;
            await _viewModel.RefreshAsync();
        }

      

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage { DataContext = new HomeViewModel() };
            PageNavigator.NavigateWithFade(page);
        }
    }
}
