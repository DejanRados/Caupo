
using Caupo.Helpers;
using Caupo.Server;
using Caupo.ViewModels;
using Caupo.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;

namespace Caupo
{
    public partial class MainWindow : Window
    {
        public static MainWindow? Instance { get; private set; }

        private readonly VirtualKeyboard _keyboard;
        private readonly List<Border> _notificationCards = new();
        private TcpIpServer? _server;

        public MainWindow()
        {
            InitializeComponent();
            Instance = this;

            _keyboard = new VirtualKeyboard();
            _keyboard.KeyPressed += Keyboard_KeyPressed;
            SetKeyboardControl(_keyboard);

            VirtualKeyboardManager.EnterPressed += HideKeyboard;
            PreviewKeyDown += MainWindow_PreviewKeyDown;

            Closed += (s, e) =>
            {
                if (ReferenceEquals(Instance, this))
                    Instance = null;
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[SERVER] Pokrećem TCP server.");

            string dbPath = Properties.Settings.Default.DbPath;
            string connectionString = $"Data Source={dbPath};Version=3;";

            try
            {
                _server = new TcpIpServer(5000, connectionString);
                _ = Task.Run(async () => await _server.StartAsync());

                ClientRegistry.LoadFromFile();
                Debug.WriteLine("[SERVER] TCP server je pokrenut iz MainWindow.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SERVER] Greška pri pokretanju TCP servera: {ex}");
            }
        }

        public void ToggleKeyboard()
        {
            KeyboardHost.Visibility = KeyboardHost.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Keyboard_KeyPressed(string key)
        {
            if (_notificationCards.Count > 0)
                return;

            if (MainContent.Content is IKeyboardInputReceiver receiver)
                receiver.ReceiveKey(key);
        }

        public void ShowKeyboard()
        {
            KeyboardHost.Visibility = Visibility.Visible;
        }

        public void HideKeyboard()
        {
            KeyboardHost.Visibility = Visibility.Collapsed;
        }

        public void SetKeyboardControl(VirtualKeyboard keyboard)
        {
            KeyboardHost.Content = keyboard;
        }

        public void SlideInContent()
        {
            if (MainContent.Content is not UIElement content)
                return;

            var transform = new TranslateTransform();
            content.RenderTransform = transform;
            transform.X = ActualWidth;

            var animation = new DoubleAnimation
            {
                From = ActualWidth,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            transform.BeginAnimation(TranslateTransform.XProperty, animation);
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_notificationCards.Count > 0)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter && KeyboardHost.Visibility == Visibility.Visible)
                HideKeyboard();
        }

        // =====================================================
        // KUHINJSKE NOTIFIKACIJE
        // =====================================================

        public void ShowKitchenNotification(string waiter, string table, int orderNumber,
            IEnumerable<string> items, IEnumerable<int> itemIds, bool isUndo = false)
        {
            var itemList = items.ToList();
            var ids = itemIds.OrderBy(id => id).ToArray();

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() =>
                    ShowKitchenNotification(waiter, table, orderNumber, itemList, ids, isUndo)));
                return;
            }

            if (ids.Length == 0)
            {
                Debug.WriteLine($"[KUHINJA] Notifikacija #{orderNumber} nema ID-ove stavki.");
                return;
            }

            var previous = _notificationCards.LastOrDefault(card =>
                card.Tag is ValueTuple<int, int[], bool> info &&
                info.Item1 == orderNumber &&
                info.Item3 != isUndo &&
                info.Item2.SequenceEqual(ids));

            if (previous != null)
            {
                previous.MouseLeftButtonUp -= KitchenNotification_Click;
                NotificationCards.Children.Remove(previous);
                _notificationCards.Remove(previous);
            }

            var card = CreateKitchenNotification(waiter, table, orderNumber, itemList, ids, isUndo);
            _notificationCards.Add(card);
            NotificationCards.Children.Add(card);

            ArrangeKitchenNotifications();
            UpdateKitchenNotificationOverlay();
        }

        private Border CreateKitchenNotification(string waiter, string table, int orderNumber,
            IReadOnlyList<string> items, int[] itemIds, bool isUndo)
        {
            var template = (DataTemplate)FindResource("KitchenNotificationTemplate");
            var card = (Border)template.LoadContent();

            card.DataContext = new
            {
                Waiter = waiter,
                Table = table,
                OrderNumber = orderNumber,
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Items = items,
                IsUndo = isUndo
            };

            card.Tag = (orderNumber, itemIds, isUndo);
            card.MaxHeight = Math.Max(255, ActualHeight - 40);
            card.MouseLeftButtonUp += KitchenNotification_Click;

            return card;
        }

        private void KitchenNotification_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border card || !ReferenceEquals(card, _notificationCards.LastOrDefault()))
                return;

            DependencyObject? current = e.OriginalSource as DependencyObject;

            while (current != null && !ReferenceEquals(current, card))
            {
                if (current is ScrollBar || current is Thumb)
                    return;

                current = current switch
                {
                    Visual or Visual3D => VisualTreeHelper.GetParent(current),
                    FrameworkContentElement element => element.Parent,
                    _ => null
                };
            }

            card.MouseLeftButtonUp -= KitchenNotification_Click;
            NotificationCards.Children.Remove(card);
            _notificationCards.Remove(card);

            ArrangeKitchenNotifications();
            UpdateKitchenNotificationOverlay();
            e.Handled = true;
        }

        private void ArrangeKitchenNotifications()
        {
            for (int i = 0; i < _notificationCards.Count; i++)
            {
                var card = _notificationCards[i];
                card.Margin = new Thickness(0, 0, 20 + i * 12, 20 + i * 5);
                card.IsHitTestVisible = i == _notificationCards.Count - 1;
                Panel.SetZIndex(card, i);
            }
        }

        private void UpdateKitchenNotificationOverlay()
        {
            bool hasNotifications = _notificationCards.Count > 0;

            NotificationHost.Visibility = hasNotifications ? Visibility.Visible : Visibility.Collapsed;
            Workspace.IsHitTestVisible = !hasNotifications;
            Workspace.Effect = hasNotifications ? new BlurEffect { Radius = 8 } : null;
        }
    }
}
