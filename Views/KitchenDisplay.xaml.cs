using Caupo.Services;
using Caupo.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Caupo.Views
{
    public partial class KitchenDisplay : Window
    {
        private readonly KitchenKeyboardService _keyboardService;

        public KitchenDisplay()
        {
            InitializeComponent();

            WindowState = WindowState.Normal;
            ShowActivated = false;

            AddHandler(
                Button.LoadedEvent,
                new RoutedEventHandler((sender, e) =>
                {
                    if (e.OriginalSource is not Button button)
                        return;

                    if (button.DataContext is not KitchenDisplayViewModel.OrderItem item)
                        return;

                    Debug.WriteLine(
                        $"[KUHINJA DIJAGNOSTIKA] Stavka={item.Name}, " +
                        $"IdStavke={item.IdStavke}, " +
                        $"IsFinished={item.IsFinished}, " +
                        $"IsBusy={item.IsBusy}, " +
                        $"CanFinish={item.CanFinish}, " +
                        $"Button.IsEnabled={button.IsEnabled}");
                }),
                true);

            DataContext = App.GlobalKitchenVM;

            _keyboardService = new KitchenKeyboardService();

            _keyboardService.CommandReceived +=
                KeyboardService_CommandReceived;

            SourceInitialized += KitchenDisplay_SourceInitialized;
            Closed += KitchenDisplay_Closed;
        }

        // =====================================================
        // RAW INPUT - POKRETANJE
        // =====================================================

        private void KitchenDisplay_SourceInitialized(
            object? sender,
            EventArgs e)
        {
            try
            {
                _keyboardService.Start(this);

                Debug.WriteLine(
                    "[KUHINJA TASTATURA] Raw Input servis pokrenut.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[KUHINJA TASTATURA] Greška pri pokretanju: {ex}");
            }
        }

        // =====================================================
        // RAW INPUT - KOMANDE F1-F5
        // =====================================================

        private async void KeyboardService_CommandReceived(
            object? sender,
            KitchenKeyCommand command)
        {
            if (DataContext is not KitchenDisplayViewModel vm)
                return;

            try
            {
                switch (command)
                {
                    case KitchenKeyCommand.PreviousOrder:
                        vm.SelectPreviousOrder();
                        break;

                    case KitchenKeyCommand.NextOrder:
                        vm.SelectNextOrder();
                        break;

                    case KitchenKeyCommand.PreviousItem:
                        vm.SelectPreviousItem();
                        break;

                    case KitchenKeyCommand.NextItem:
                        vm.SelectNextItem();
                        break;

                    case KitchenKeyCommand.Finish:
                        await vm.FinishKeyboardSelectionAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[KUHINJA TASTATURA] Greška pri obradi komande {command}: {ex}");
            }
        }

        // =====================================================
        // RAW INPUT - GAŠENJE
        // =====================================================

        private void KitchenDisplay_Closed(
            object? sender,
            EventArgs e)
        {
            _keyboardService.CommandReceived -=
                KeyboardService_CommandReceived;

            _keyboardService.Dispose();

            Debug.WriteLine(
                "[KUHINJA TASTATURA] Raw Input servis zaustavljen.");
        }

        // =====================================================
        // DIJAGNOSTIKA STAVKE
        // =====================================================

        private void Item_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is not KitchenDisplayViewModel.OrderItem item)
                return;

            Debug.WriteLine(
                $"[KUHINJA STAVKA] Naziv={item.Name}, " +
                $"Id={item.IdStavke}, " +
                $"Finished={item.IsFinished}, " +
                $"Busy={item.IsBusy}, " +
                $"CanFinish={item.CanFinish}, " +
                $"IsEnabled={button.IsEnabled}");
        }

        // =====================================================
        // GOTOVO - CIJELA NARUDŽBA
        // =====================================================

        private async void Order_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is not KitchenDisplayViewModel.DisplayOrder order)
                return;

            if (DataContext is not KitchenDisplayViewModel vm)
                return;

            await vm.FinishOrderAsync(order);
        }

        // =====================================================
        // GOTOVO - POJEDINAČNA STAVKA
        // =====================================================

        private async void Item_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            if (button.DataContext is not KitchenDisplayViewModel.OrderItem item)
                return;

            if (DataContext is not KitchenDisplayViewModel vm)
                return;

            var order = FindParentOrder(button);

            if (order == null)
                return;

            await vm.FinishItemAsync(item, order);
        }

        // =====================================================
        // PRONALAŽENJE RODITELJSKE NARUDŽBE
        // =====================================================

        private static KitchenDisplayViewModel.DisplayOrder?
            FindParentOrder(DependencyObject element)
        {
            DependencyObject? current = element;

            while (current != null)
            {
                if (current is FrameworkElement frameworkElement &&
                    frameworkElement.DataContext is
                        KitchenDisplayViewModel.DisplayOrder order)
                {
                    return order;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}