using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;


namespace Caupo.UserControls
{
    public partial class SidebarMenuItem : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register ("Header", typeof (string), typeof (SidebarMenuItem), new PropertyMetadata (""));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register ("Icon", typeof (string), typeof (SidebarMenuItem), new PropertyMetadata (""));

        public static readonly DependencyProperty IconFontFamilyProperty =
            DependencyProperty.Register ("IconFontFamily", typeof (FontFamily), typeof (SidebarMenuItem),
                new PropertyMetadata (new FontFamily ("Segoe MDL2 Assets")));

        public static readonly DependencyProperty SubItemsProperty =
            DependencyProperty.Register ("SubItems", typeof (ObservableCollection<SidebarMenuItem>), typeof (SidebarMenuItem),
                new PropertyMetadata (null)); // Vratimo na null kao default

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register ("IsExpanded", typeof (bool), typeof (SidebarMenuItem),
                new PropertyMetadata (false, OnIsExpandedChanged));

        public static readonly DependencyProperty CommandProperty =
    DependencyProperty.Register ("Command", typeof (ICommand), typeof (SidebarMenuItem), new PropertyMetadata (null));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register ("CommandParameter", typeof (object), typeof (SidebarMenuItem), new PropertyMetadata (null));

        public ICommand Command
        {
            get { return (ICommand)GetValue (CommandProperty); }
            set { SetValue (CommandProperty, value); }
        }

        public object CommandParameter
        {
            get { return GetValue (CommandParameterProperty); }
            set { SetValue (CommandParameterProperty, value); }
        }

        private bool _hasSubItems;
        private bool _subItemsInitialized = false;

        public string Header
        {
            get { return (string)GetValue (HeaderProperty); }
            set { SetValue (HeaderProperty, value); }
        }

        public string Icon
        {
            get { return (string)GetValue (IconProperty); }
            set { SetValue (IconProperty, value); }
        }

        public FontFamily IconFontFamily
        {
            get { return (FontFamily)GetValue (IconFontFamilyProperty); }
            set { SetValue (IconFontFamilyProperty, value); }
        }

        public ObservableCollection<SidebarMenuItem> SubItems
        {
            get
            {
                var items = (ObservableCollection<SidebarMenuItem>)GetValue (SubItemsProperty);
                return items;
            }
            set { SetValue (SubItemsProperty, value); }
        }

        public bool IsExpanded
        {
            get { return (bool)GetValue (IsExpandedProperty); }
            set { SetValue (IsExpandedProperty, value); }
        }

        public bool HasSubItems
        {
            get { return _hasSubItems; }
            set
            {
                if(_hasSubItems != value)
                {
                    _hasSubItems = value;
                    OnPropertyChanged (nameof (HasSubItems));
                }
            }
        }

        public ICommand ToggleCommand { get; private set; }

        public event EventHandler<SidebarMenuItem> ItemExpanded;
        public event EventHandler<SidebarMenuItem> ItemCollapsed;
        public event PropertyChangedEventHandler PropertyChanged;

        public SidebarMenuItem()
        {
            InitializeComponent ();


            ToggleCommand = new RelayCommand (ToggleSubmenu);
            DataContext = this;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized (e);
            UpdateSubItems ();
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SidebarMenuItem)d;
            if((bool)e.NewValue)
            {
                control.ExpandSubmenu ();
                control.ItemExpanded?.Invoke (control, control);
            }
            else
            {
                control.CollapseSubmenu ();
            }
        }

        private void UpdateSubItems()
        {
            // Koristimo SubItems direktno bez automatske inicijalizacije
            var hasItems = SubItems != null && SubItems.Count > 0;
            HasSubItems = hasItems;

            if(hasItems)
            {
                SubItemsControl.ItemsSource = SubItems;
            }


            ArrowIcon.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;
        }

        public void AddSubItem(SidebarMenuItem item)
        {
            // Inicijaliziraj SubItems samo kada se prvi put dodaje stavka
            if(SubItems == null)
            {
                SubItems = new ObservableCollection<SidebarMenuItem> ();
                _subItemsInitialized = true;
            }

            SubItems.Add (item);
            UpdateSubItems ();
        }


        private void ToggleSubmenu()
        {
            if(HasSubItems)
            {
                IsExpanded = !IsExpanded;
            }
            else
            {
                if(Command != null && Command.CanExecute (CommandParameter))
                {
                    Command.Execute (CommandParameter);

                }
            }
        }



        private void ExpandSubmenu()
        {
            if(!HasSubItems)
                return;

            SubItemsPanel.Visibility = Visibility.Visible;
            var heightAnimation = new DoubleAnimation
            {
                From = 0,
                To = SubItems.Count * 45,
                Duration = TimeSpan.FromSeconds (0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            SubItemsPanel.BeginAnimation (HeightProperty, heightAnimation);

            var rotateAnimation = new DoubleAnimation
            {
                To = 180,
                Duration = TimeSpan.FromSeconds (0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            ArrowTransform.BeginAnimation (RotateTransform.AngleProperty, rotateAnimation);
        }

        private void CollapseSubmenu()
        {
            if(!HasSubItems)
                return;

            var heightAnimation = new DoubleAnimation
            {
                From = SubItems.Count * 45,
                To = 0,
                Duration = TimeSpan.FromSeconds (0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            heightAnimation.Completed += (s, e) =>
            {
                if(!IsExpanded)
                    SubItemsPanel.Visibility = Visibility.Collapsed;
                ItemCollapsed?.Invoke (this, this);
            };
            SubItemsPanel.BeginAnimation (HeightProperty, heightAnimation);

            var rotateAnimation = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromSeconds (0.3),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            ArrowTransform.BeginAnimation (RotateTransform.AngleProperty, rotateAnimation);
        }

        public void Collapse()
        {
            if(IsExpanded)
                IsExpanded = false;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute ();
        }

        public void Execute(object parameter)
        {
            _execute ();
        }
    }
}