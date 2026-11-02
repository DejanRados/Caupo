
using Caupo.Helpers;
using Caupo.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Caupo.Views
{
    public partial class DashboardPage : UserControl
    {
        private readonly DashboardViewModel _viewModel;
        private bool _initialized;
        private bool _switchingMode;
        private bool _refreshRunning;
        private bool _refreshPending;
        private string _periodStartTime = "00:00";
        private string _periodEndTime = "23:59";

        public DashboardPage()
        {
            InitializeComponent();

            _viewModel = new DashboardViewModel();
            DataContext = _viewModel;

            DashboardMenu.ReportSelected += DashboardMenu_ReportSelected;
            Loaded += DashboardPage_Loaded;
        }

        private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= DashboardPage_Loaded;
            _initialized = true;
            RequestRefresh();
        }

        private void DashboardMenu_ReportSelected(object? sender, string report)
        {
            _viewModel.SelectedReport = report;
            RequestRefresh();
        }

        private void DashboardMenu_ModeChanged(object? sender, bool periodic)
        {
            if (_viewModel.IsPeriodicMode == periodic)
                return;

            _switchingMode = true;

            try
            {
                if (_viewModel.IsPeriodicMode)
                {
                    _periodStartTime = _viewModel.OdVrijeme;
                    _periodEndTime = _viewModel.DoVrijeme;
                }

                _viewModel.IsPeriodicMode = periodic;
                _viewModel.DoDatuma = _viewModel.OdDatuma;

                if (periodic)
                {
                    _viewModel.OdVrijeme = _periodStartTime;
                    _viewModel.DoVrijeme = _periodEndTime;
                }
            }
            finally
            {
                _switchingMode = false;
            }

            RequestRefresh();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_initialized || _switchingMode)
                return;

            if (ReferenceEquals(sender, DpFromDate))
            {
                _viewModel.OdDatuma = DpFromDate.SelectedDate;

                if (!_viewModel.IsPeriodicMode)
                    _viewModel.DoDatuma = DpFromDate.SelectedDate;
            }
            else if (ReferenceEquals(sender, DpToDate))
            {
                _viewModel.DoDatuma = DpToDate.SelectedDate;
            }

            RequestRefresh();
        }

        private void TimeBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IsPeriodicMode && !_switchingMode)
                RequestRefresh();
        }

        private void RequestRefresh()
        {
            if (!_initialized || _switchingMode)
                return;

            _refreshPending = true;

            if (!_refreshRunning)
                _ = RefreshPendingAsync();
        }

        private async Task RefreshPendingAsync()
        {
            _refreshRunning = true;

            try
            {
                while (_refreshPending)
                {
                    _refreshPending = false;
                    await _viewModel.RefreshAsync();
                }
            }
            finally
            {
                _refreshRunning = false;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage { DataContext = new HomeViewModel() };
            PageNavigator.NavigateWithFade(page);
        }
    }
}
