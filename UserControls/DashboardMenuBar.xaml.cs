
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Caupo.UserControls
{
    public partial class DashboardMenuBar : UserControl
    {
        public event EventHandler<string>? ReportSelected;
        public event EventHandler<bool>? ModeChanged;

        private Button? _selectedButton;
        private string _selectedReport = "DnevniPromet";
        private bool _periodicMode;

        public DashboardMenuBar()
        {
            InitializeComponent();

            DailyPanel.Visibility = Visibility.Visible;
            DailyArrowTransform.Angle = 90;

            _selectedButton = DailyPanel.Children.OfType<Button>()
                .FirstOrDefault(b => (string?)b.Tag == _selectedReport);

            HighlightSelectedButton();
        }

        private void DailyButton_Click(object sender, RoutedEventArgs e)
        {
            SelectReport("DnevniPromet");
        }

        private void PeriodicButton_Click(object sender, RoutedEventArgs e)
        {
            SelectReport("PeriodicniPromet");
        }

        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string report)
                SelectReport(report);
        }

        private void SelectReport(string report)
        {
            bool? periodic = report.StartsWith("Dnevni", StringComparison.Ordinal) ? false
                : report.StartsWith("Periodicni", StringComparison.Ordinal) ? true
                : null;

            if (periodic.HasValue)
            {
                bool modeChanged = _periodicMode != periodic.Value;
                _periodicMode = periodic.Value;

                if (modeChanged)
                    ModeChanged?.Invoke(this, _periodicMode);

                OpenMenu(_periodicMode ? PeriodicPanel : DailyPanel,
                    _periodicMode ? PeriodicArrowTransform : DailyArrowTransform);
            }

            if (_selectedReport == report && _selectedButton != null)
                return;

            _selectedReport = report;
            HighlightSelectedButton();
            ReportSelected?.Invoke(this, report);
        }


        private void HighlightSelectedButton()
        {
            if (_selectedButton != null)
            {
                _selectedButton.ClearValue(Button.BackgroundProperty);
                _selectedButton.ClearValue(Button.ForegroundProperty);
                _selectedButton.ClearValue(Button.FontWeightProperty);
            }

            _selectedButton = DailyPanel.Children.OfType<Button>()
                .Concat(PeriodicPanel.Children.OfType<Button>())
                .FirstOrDefault(b => (string?)b.Tag == _selectedReport);

            if (_selectedButton == null)
                return;

            _selectedButton.SetResourceReference(Button.BackgroundProperty, "ThemeSelectionBrush");
            _selectedButton.SetResourceReference(Button.ForegroundProperty, "GlobalFontColor");
            _selectedButton.FontWeight = FontWeights.SemiBold;
        }


        private void OpenMenu(StackPanel panel, RotateTransform arrow)
        {
            CloseAllMenus();
            panel.Visibility = Visibility.Visible;
            arrow.Angle = 90;
        }

        public void CloseAllMenus()
        {
            DailyPanel.Visibility = Visibility.Collapsed;
            PeriodicPanel.Visibility = Visibility.Collapsed;
            DailyArrowTransform.Angle = 0;
            PeriodicArrowTransform.Angle = 0;
        }
    }
}
