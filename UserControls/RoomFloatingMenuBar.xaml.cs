using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Caupo.UserControls
{
    public partial class RoomFloatingMenuBar : UserControl
    {
        public event EventHandler<string>? NameSaved;
        public event EventHandler? DeleteConfirmed;
        public event EventHandler? FinishRequested;
        public event MouseButtonEventHandler? DragStarted;
        public event MouseEventHandler? DragMoved;
        public event MouseButtonEventHandler? DragFinished;

        public RoomFloatingMenuBar()
        {
            InitializeComponent();
        }

        public void SetRoom(string roomName, bool canDelete)
        {
            RoomNameText.Text = string.IsNullOrWhiteSpace(roomName) ? "Sala" : roomName;
            DeleteButton.IsEnabled = canDelete;
            DeleteButton.ToolTip = canDelete ? null : "Sala sadrži otvorene narudžbe i ne može biti obrisana.";
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

            RoomNameInput.Text = RoomNameText.Text;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                RoomNameInput.Focus();
                RoomNameInput.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!DeleteButton.IsEnabled)
                return;

            bool open = DeletePanel.Visibility != Visibility.Visible;
            ToggleMenu(DeletePanel, DeleteArrowTransform);

            if (open)
                DeleteRoomNameText.Text = RoomNameText.Text;
        }

        private void SaveNameButton_Click(object sender, RoutedEventArgs e)
        {
            string name = RoomNameInput.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                RoomNameInput.Focus();
                return;
            }

            RoomNameText.Text = name;
            NameSaved?.Invoke(this, name);
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
            DeletePanel.Visibility = Visibility.Collapsed;

            RenameArrowTransform.Angle = 0;
            DeleteArrowTransform.Angle = 0;
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            CloseAllMenus();
            FinishRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
