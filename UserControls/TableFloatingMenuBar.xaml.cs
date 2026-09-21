using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Caupo.UserControls
{
    public partial class TableFloatingMenuBar : UserControl
    {
        private double tableWidth;
        private double tableHeight;

        public event EventHandler<string>? NameSaved;
        public event EventHandler<TableSizeChangedEventArgs>? SizeSaved;
        public event EventHandler? DeleteConfirmed;
        public event EventHandler? FinishRequested;
        public event MouseButtonEventHandler? DragStarted;
        public event MouseEventHandler? DragMoved;
        public event MouseButtonEventHandler? DragFinished;

        public TableFloatingMenuBar()
        {
            InitializeComponent();
        }

        public void SetTable(string tableName, double width, double height, bool canDelete)
        {
            TableNameText.Text = string.IsNullOrWhiteSpace(tableName) ? "Sto" : tableName;
            tableWidth = width;
            tableHeight = height;
            DeleteButton.IsEnabled = canDelete;
            DeleteButton.ToolTip = canDelete ? null : "Sto ima otvorenu narudžbu i ne može biti obrisan.";
            CloseAllMenus();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TitleBar.CaptureMouse();
            DragStarted?.Invoke(this, e);
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            DragMoved?.Invoke(this, e);
        }

        private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TitleBar.ReleaseMouseCapture();
            DragFinished?.Invoke(this, e);
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            bool open = RenamePanel.Visibility != Visibility.Visible;
            ToggleMenu(RenamePanel, RenameArrowTransform);

            if (!open)
                return;

            TableNameInput.Text = TableNameText.Text;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                TableNameInput.Focus();
                TableNameInput.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void ResizeButton_Click(object sender, RoutedEventArgs e)
        {
            bool open = ResizePanel.Visibility != Visibility.Visible;
            ToggleMenu(ResizePanel, ResizeArrowTransform);

            if (!open)
                return;

            WidthInput.Text = tableWidth.ToString("0.##", CultureInfo.CurrentCulture);
            HeightInput.Text = tableHeight.ToString("0.##", CultureInfo.CurrentCulture);
            ResizeErrorText.Visibility = Visibility.Collapsed;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                WidthInput.Focus();
                WidthInput.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!DeleteButton.IsEnabled)
                return;

            bool open = DeletePanel.Visibility != Visibility.Visible;
            ToggleMenu(DeletePanel, DeleteArrowTransform);

            if (open)
                DeleteTableNameText.Text = TableNameText.Text;
        }

        private void SaveNameButton_Click(object sender, RoutedEventArgs e)
        {
            string name = TableNameInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                TableNameInput.Focus();
                return;
            }

            TableNameText.Text = name;
            NameSaved?.Invoke(this, name);
            CloseAllMenus();
        }

        private void SaveSizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(WidthInput.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out double width) ||
                !double.TryParse(HeightInput.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out double height) ||
                width < 40 || height < 40 || width > 500 || height > 500)
            {
                ResizeErrorText.Visibility = Visibility.Visible;
                return;
            }

            tableWidth = width;
            tableHeight = height;
            ResizeErrorText.Visibility = Visibility.Collapsed;
            SizeSaved?.Invoke(this, new TableSizeChangedEventArgs(width, height));
            CloseAllMenus();
        }

        private void ConfirmDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmed?.Invoke(this, EventArgs.Empty);
        }

        private void CloseSubMenuButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllMenus();
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
            RenamePanel.Visibility = Visibility.Collapsed;
            ResizePanel.Visibility = Visibility.Collapsed;
            DeletePanel.Visibility = Visibility.Collapsed;

            RenameArrowTransform.Angle = 0;
            ResizeArrowTransform.Angle = 0;
            DeleteArrowTransform.Angle = 0;
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllMenus();
            FinishRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public sealed class TableSizeChangedEventArgs : EventArgs
    {
        public double Width { get; }
        public double Height { get; }

        public TableSizeChangedEventArgs(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }
}
