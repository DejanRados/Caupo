
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Caupo.UserControls
{
    public partial class DashboardMenuBar : UserControl
    {
        public event EventHandler<string>? ReportSelected;

        public DashboardMenuBar()
        {
            InitializeComponent();
        }

        private void DailyButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu(DailyPanel, DailyArrowTransform);
        }

        private void PeriodicButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMenu(PeriodicPanel, PeriodicArrowTransform);
        }

        private void ReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string report)
                return;

            ReportSelected?.Invoke(this, report);
        }

        private void ToggleMenu(StackPanel panel, RotateTransform arrow)
        {
            bool open = panel.Visibility != Visibility.Visible;
            CloseAllMenus();

            if (!open)
                return;

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
