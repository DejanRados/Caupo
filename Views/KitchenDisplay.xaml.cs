using Caupo.Services;
using Caupo.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Caupo.Views
{
    public partial class KitchenDisplay : Window
    {
        private readonly KitchenKeyboardService _keyboardService;
        private bool _f6Held;
        private bool _f7Held;
        private int _scrollDirection;
        private TimeSpan _lastScrollFrame;
        private bool _scrollRenderingActive;
        private const double ScrollSpeed = 1300;

        public KitchenDisplay()
        {
            InitializeComponent();
            WindowState = WindowState.Normal;
            ShowActivated = false;
            DataContext = App.GlobalKitchenVM;

            _keyboardService = new KitchenKeyboardService();
            _keyboardService.CommandReceived += KeyboardService_CommandReceived;
            _keyboardService.ScrollKeyChanged += KeyboardService_ScrollKeyChanged;

            SourceInitialized += KitchenDisplay_SourceInitialized;
            Closed += KitchenDisplay_Closed;
            Loaded += KitchenDisplay_Loaded;
        }

        // Samo infrastruktura prozora; poslovne akcije su ICommand u ViewModelu.
        private void KitchenDisplay_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.KitchenKeyboardId))
                return;

            var popup = new MyMessageBox();
            popup.MessageTitle.Text = "Kuhinjska tastatura";
            popup.MessageText.Text = "Tastatura za kuhinjski ekran nije podešena.\n\nKuhinjski ekran nastavlja normalno raditi bez tastature.";
            popup.ShowDialog();
        }

        private void KitchenDisplay_SourceInitialized(object? sender, EventArgs e)
        {
            try
            {
                _keyboardService.Start(this);
                Debug.WriteLine("[KUHINJA TASTATURA] Raw Input servis pokrenut.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[KUHINJA TASTATURA] Greška pri pokretanju: {ex}");
            }
        }

        // Raw Input prevodi tipke u iste komande koje koriste miš i touch.
        private void KeyboardService_CommandReceived(object? sender, KitchenKeyCommand key)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => KeyboardService_CommandReceived(sender, key)));
                return;
            }

            if (DataContext is not KitchenDisplayViewModel vm)
                return;

            ICommand? command = key switch
            {
                KitchenKeyCommand.PreviousOrder => vm.PreviousOrderCommand,
                KitchenKeyCommand.NextOrder => vm.NextOrderCommand,
                KitchenKeyCommand.PreviousItem => vm.PreviousItemCommand,
                KitchenKeyCommand.NextItem => vm.NextItemCommand,
                KitchenKeyCommand.Finish => vm.FinishSelectionCommand,
                KitchenKeyCommand.Undo => vm.UndoCommand,
                KitchenKeyCommand.ConfirmNewOrder => vm.ConfirmNewOrderCommand,
                _ => null
            };

            try
            {
                if (command?.CanExecute(null) == true)
                {
                    command.Execute(null);

                    if (key is KitchenKeyCommand.PreviousOrder or KitchenKeyCommand.NextOrder)
                    {
                        Dispatcher.BeginInvoke(new Action(ScrollToSelectedOrder), DispatcherPriority.Loaded);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[KUHINJA TASTATURA] Greška pri obradi komande {key}: {ex}");
            }
        }

        // F6/F7: kratak pritisak pomjera sadržaj, držanje omogućava kontinuirano skrolanje.
        private void KeyboardService_ScrollKeyChanged(int direction, bool pressed)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => KeyboardService_ScrollKeyChanged(direction, pressed)));
                return;
            }

            if (direction == 1)
                _f6Held = pressed;
            else if (direction == -1)
                _f7Held = pressed;
            else
                return;

            if (pressed)
            {
                _scrollDirection = direction;
                OrdersScrollViewer.ScrollToVerticalOffset(OrdersScrollViewer.VerticalOffset + direction * 40);
            }
            else if (_scrollDirection == direction)
            {
                _scrollDirection = _f6Held ? 1 : _f7Held ? -1 : 0;
            }

            if (_scrollDirection == 0)
            {
                StopScrollRendering();
            }
            else if (!_scrollRenderingActive)
            {
                _lastScrollFrame = TimeSpan.Zero;
                CompositionTarget.Rendering += ScrollRendering;
                _scrollRenderingActive = true;
            }
        }

        // Skrolanje je sinhronizovano s WPF iscrtavanjem.
        private void ScrollRendering(object? sender, EventArgs e)
        {
            if (_scrollDirection == 0 || e is not RenderingEventArgs args)
                return;

            if (_lastScrollFrame == TimeSpan.Zero)
            {
                _lastScrollFrame = args.RenderingTime;
                return;
            }

            double seconds = (args.RenderingTime - _lastScrollFrame).TotalSeconds;
            _lastScrollFrame = args.RenderingTime;

            if (seconds <= 0)
                return;

            seconds = Math.Min(seconds, 0.05);

            OrdersScrollViewer.ScrollToVerticalOffset(OrdersScrollViewer.VerticalOffset + _scrollDirection * ScrollSpeed * seconds);
        }

        private void StopScrollRendering()
        {
            if (_scrollRenderingActive)
            {
                CompositionTarget.Rendering -= ScrollRendering;
                _scrollRenderingActive = false;
            }

            _lastScrollFrame = TimeSpan.Zero;
        }

        // VM bira narudžbu, a View je po potrebi dovodi u vidljivi dio.
        private void ScrollToSelectedOrder()
        {
            if (DataContext is not KitchenDisplayViewModel vm)
                return;

            var selected = vm.Orders.FirstOrDefault(order =>
                order.IsKeyboardSelected ||
                order.Items.Any(item => item.IsKeyboardSelected));

            if (selected == null)
                return;

            var card = FindOrderCard(OrdersScrollViewer, selected);

            if (card == null)
                return;

            OrdersScrollViewer.UpdateLayout();

            Rect bounds = card.TransformToAncestor(OrdersScrollViewer)
                              .TransformBounds(new Rect(card.RenderSize));

            double viewportHeight = OrdersScrollViewer.ViewportHeight;

            if (bounds.Top < 0)
            {
                OrdersScrollViewer.ScrollToVerticalOffset(
                    OrdersScrollViewer.VerticalOffset + bounds.Top);
            }
            else if (bounds.Bottom > viewportHeight)
            {
                OrdersScrollViewer.ScrollToVerticalOffset(
                    OrdersScrollViewer.VerticalOffset + bounds.Bottom - viewportHeight);
            }
        }

        private static Border? FindOrderCard(
            DependencyObject root,
            KitchenDisplayViewModel.DisplayOrder order)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);

                if (child is Border border &&  ReferenceEquals(border.DataContext, order))
                {
                    return border;
                }

                var result = FindOrderCard(child, order);

                if (result != null)
                    return result;
            }

            return null;
        }

        private static T? FindAncestor<T>(DependencyObject element)
            where T : DependencyObject
        {
            for (var parent = VisualTreeHelper.GetParent(element);
                 parent != null;
                 parent = VisualTreeHelper.GetParent(parent))
            {
                if (parent is T match)
                    return match;
            }

            return null;
        }

        private void KitchenDisplay_Closed(object? sender, EventArgs e)
        {
            _scrollDirection = 0;
            StopScrollRendering();

            _keyboardService.ScrollKeyChanged -= KeyboardService_ScrollKeyChanged;
            _keyboardService.CommandReceived -= KeyboardService_CommandReceived;
            _keyboardService.Dispose();

            Debug.WriteLine("[KUHINJA TASTATURA] Raw Input servis zaustavljen.");
        }
    }
}