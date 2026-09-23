using Caupo.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.UserControls
{
    public class WaiterSelectedEventArgs : EventArgs
    {
        public int WaiterId { get; }
        public string WaiterName { get; }

        public WaiterSelectedEventArgs(int waiterId, string waiterName)
        {
            WaiterId = waiterId;
            WaiterName = waiterName;
        }
    }

    public partial class TableOrderMenuBar : UserControl
    {
        public event EventHandler? MoveTableRequested;
        public event EventHandler? MergeTablesRequested;
        public event EventHandler? ChangeWaiterStarted;
        public event EventHandler? ChangeWaiterCancelled;
        public event EventHandler<WaiterSelectedEventArgs>? ChangeWaiterRequested;
        public event EventHandler? FinishRequested;

        public TableOrderMenuBar()
        {
            InitializeComponent();
        }

        public void SetTable(string tableName)
        {
            TableNameText.Text = string.IsNullOrWhiteSpace(tableName) ? "Sto" : tableName;
            CloseAllMenus();
        }

        public void SetWaiters(IEnumerable<TblRadnici> waiters)
        {
            WaitersItemsControl.ItemsSource = waiters;
        }

        private void MoveTableButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllMenus();
            MoveTableRequested?.Invoke(this, EventArgs.Empty);
        }

        private void MergeTablesButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllMenus();
            MergeTablesRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ChangeWaiterButton_Click(object sender, RoutedEventArgs e)
        {
            bool opening = ChangeWaiterPanel.Visibility != Visibility.Visible;

            ToggleMenu(ChangeWaiterPanel, ChangeWaiterArrowTransform);

            if (opening)
                ChangeWaiterStarted?.Invoke(this, EventArgs.Empty);
            else
                ChangeWaiterCancelled?.Invoke(this, EventArgs.Empty);
        }

        private void WaiterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not TblRadnici radnik)
                return;

            ChangeWaiterRequested?.Invoke(this, new WaiterSelectedEventArgs(radnik.IdRadnika, radnik.Radnik ?? string.Empty));
        }

        private void CloseSubMenuButton_Click(object sender, RoutedEventArgs e)
        {
            bool changeWaiterWasOpen = ChangeWaiterPanel.Visibility == Visibility.Visible;

            CloseAllMenus();

            if (changeWaiterWasOpen)
                ChangeWaiterCancelled?.Invoke(this, EventArgs.Empty);
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
            MoveTablePanel.Visibility = Visibility.Collapsed;
            MergeTablesPanel.Visibility = Visibility.Collapsed;
            ChangeWaiterPanel.Visibility = Visibility.Collapsed;

            MoveTableArrowTransform.Angle = 0;
            MergeTablesArrowTransform.Angle = 0;
            ChangeWaiterArrowTransform.Angle = 0;
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllMenus();
            FinishRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
