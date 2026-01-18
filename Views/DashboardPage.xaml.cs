using Caupo.Helpers;
using Caupo.UserControls;
using Caupo.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Caupo.ViewModels.DashboardViewModel;

namespace Caupo.Views
{

    public partial class DashboardPage : UserControl
    {
        private List<SidebarMenuItem> _menuItemsWithSubmenus;
        private DashboardViewModel _viewModel;
        public DashboardViewModel ViewModel
        {
            get => _viewModel;
            set => _viewModel = value;
        }
        public DashboardPage()
        {
           

            InitializeComponent();
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
            _menuItemsWithSubmenus = new List<SidebarMenuItem>();
            DataContext = new DashboardViewModel ();
            ViewModel = DataContext as DashboardViewModel;


        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage ();
            page.DataContext = new HomeViewModel ();
            PageNavigator.NavigateWithFade (page);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {



            var dnevniPromet = CreateMenuItemWithSubitems("Dnevni promet ", "\xEC59", "#FF2FA4A9 ",
            new [] {
                   ("Dnevni promet ukupno", "\xE9A6", new Action(() => ViewModel.IzracunajDnevniPromet())),
                   ("Dnevni promet po radniku", "\xE787", new Action(() => ViewModel.IzracunajDnevniPrometPoRadniku())),
                   ("Dnevni promet po artiklima", "\xE8FD", new Action(() => ViewModel.IzracunajDnevniPrometPoArtiklima()))
               });

            var periodicniPromet = CreateMenuItemWithSubitems("Periodični promet", "\xE787", "#FF2FA4A9",
            new[] {
                     (Header: "Periodični promet ukupno", Icon: "\xE9A6", new Action(() => ViewModel.IzracunajPeriodicniPromet())),
                     (Header: "Periodični promet po radniku", Icon: "", new Action(() => ViewModel.IzracunajPeriodicniPrometPoRadniku())),
                     (Header: "Periodični promet po artiklima", Icon: "", new Action(() => ViewModel.IzracunajPeriodicniPrometPoArtiklima()))
                  });


            var sankPromet = new SidebarMenuItem
            {
                Header = "Promet šanka",
                Background =  (Brush)new BrushConverter().ConvertFrom("#FF2FA4A9"),
                Icon = "\xEC32",
                Command = new RelayCommand(() => ViewModel.IzracunajDnevniPrometSanka())

            };

            var kuhinjaPromet = new SidebarMenuItem
            {
                Header = "Promet kuhinje",
                Background = (Brush)new BrushConverter().ConvertFrom("#FF2FA4A9"),
                Icon = "\xED56",
                Command = new RelayCommand(() => ViewModel.IzracunajDnevniPrometKuhinje())
            };

            var ostaloPromet = new SidebarMenuItem
            {
                Header = "Promet ostalo",
                Background = (Brush)new BrushConverter().ConvertFrom("#FF2FA4A9"),
                Icon = "\xE8EC", 
                Command = new RelayCommand(() => ViewModel.IzracunajDnevniPrometOstalo())
            };



            var namirnice = CreateMenuItemWithSubitems("Utrošak namirnica", "\xE719", "#FF2FA4A9",
                  new[] {
                     (Header: "Dnevni utrošak", Icon: "\xE9A6", new Action(() => ViewModel.IzracunajDnevniUtrosakNamirnica())),
                     (Header: "Periodični utrošak", Icon: "\xE787", new Action(() => ViewModel.IzracunajPeriodicniUtrosakNamirnica())),
                     (Header: "Zaliha namirnica", Icon: "\xE9F9",  new Action(() => ViewModel.IzracunajZalihuNamirnica()))
                  });



            var rekapitulacija = new SidebarMenuItem
            {
                Header = "Rekapitulacija prometa",
                Background = (Brush)new BrushConverter().ConvertFrom("#FF2FA4A9"),
                Icon = "\xE713",
                Command = new RelayCommand(async () => await ViewModel.Print(ViewModel.OdDatuma))

            };

            periodicniPromet.ItemExpanded += PeriodicniMenuOpened;
            periodicniPromet.ItemCollapsed += PeriodicniMenuClosed;

            dnevniPromet.ItemExpanded += DnevniMenuOpened;
            dnevniPromet.ItemCollapsed += DnevniMenuClosed;

            namirnice.ItemExpanded += NamirniceMenuOpened;
            namirnice.ItemCollapsed += NamirniceMenuClosed;

            // Dodavanje stavki u sidebar
            SidebarPanel.Children.Add(dnevniPromet);
            SidebarPanel.Children.Add(periodicniPromet);
            SidebarPanel.Children.Add(sankPromet);
            SidebarPanel.Children.Add(kuhinjaPromet);
            SidebarPanel.Children.Add(ostaloPromet);
            SidebarPanel.Children.Add(namirnice);
            SidebarPanel.Children.Add(rekapitulacija);
        }

        private void NamirniceMenuOpened(object sender, SidebarMenuItem item)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.PeriodicniPrometAktivan = false;
                vm.IsRadnik = false;
                vm.CurrentView = null;
            }
        }

        private void NamirniceMenuClosed(object sender, SidebarMenuItem item)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.CurrentView = null;
                vm.IsRadnik = false;
            }

        }
        private void DnevniMenuOpened(object sender, SidebarMenuItem item)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.PeriodicniPrometAktivan = false;
                vm.IsRadnik = false;
                vm.CurrentView = null;
            }
        }

        private void DnevniMenuClosed(object sender, SidebarMenuItem item)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.CurrentView = null;
                vm.IsRadnik = false;
            }

        }

        private void PeriodicniMenuOpened(object sender, SidebarMenuItem item)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.PeriodicniPrometAktivan = true;
                vm.IsRadnik = false;
                vm.CurrentView = null;
            }
        }

        private void PeriodicniMenuClosed(object sender, SidebarMenuItem item)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.PeriodicniPrometAktivan = false;
                vm.IsRadnik = false;
                vm.CurrentView = null;
            }
               
        }


        private SidebarMenuItem CreateMenuItemWithSubitems(string header, string icon, string color,
      (string Header, string Icon, Action Command)[] subitems)
        {
            var menuItem = new SidebarMenuItem
            {
                Header = header,
                Background = (Brush)new BrushConverter().ConvertFrom(color),
                Icon = icon
            };

            if (menuItem.SubItems == null)
                menuItem.SubItems = new ObservableCollection<SidebarMenuItem>();

            foreach (var subitem in subitems)
            {
                menuItem.AddSubItem(new SidebarMenuItem
                {
                    Header = subitem.Header,
                    Icon = subitem.Icon,
                    Command = new RelayCommand(subitem.Command) 
                });
            }

            menuItem.ItemExpanded += OnMenuItemExpanded;
            _menuItemsWithSubmenus.Add(menuItem);

            return menuItem;
        }

        private void OnMenuItemExpanded(object sender, SidebarMenuItem expandedItem)
        {
            // Zatvori sve ostale stavke osim one koja je expandirana
            foreach (var menuItem in _menuItemsWithSubmenus)
            {
                if (menuItem != expandedItem && menuItem.IsExpanded)
                {
                    menuItem.Collapse();
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var vm = DataContext as DashboardViewModel;
            if (vm == null) return;

            if (vm.TrenutniIzvjestaj == TipIzvjestaja.Dnevni)
            {
                
                vm.IzracunajDnevniPrometPoRadniku();
            }
            else if (vm.TrenutniIzvjestaj == TipIzvjestaja.Periodicni)
            {
               
                vm.IzracunajPeriodicniPrometPoRadniku();
            }
        }

        private void DpOdDate_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var vm = DataContext as DashboardViewModel;
            vm.CurrentView = null;
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel viewModel)
            {
                viewModel.PrintDashboardReport(viewModel.ReportType, viewModel.ProdaniArtikli, viewModel.ReklamiraniArtikli,  viewModel.OdDatuma, viewModel.DoDatuma);
            }
        }
    }
}
