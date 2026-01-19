using Caupo.Helpers;
using Caupo.Server;
using Caupo.ViewModels;
using Caupo.Views;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;


namespace Caupo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {

        public static MainWindow Instance;
        private VirtualKeyboard keyboard;
        public MainWindow()
        {
            // SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NMaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWX5dcHVWQ2JdU0NyWEo=");
            //  Settings.Default.Tema = "Office2019Black";
            //  Settings.Default.Save();

            InitializeComponent ();

            this.DataContext = new MainViewModel ();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            PageNavigator.Navigate = page =>
            {
                FadeTransition (page);
            };
            Instance = this;
            keyboard = new VirtualKeyboard ();
            keyboard.KeyPressed += Keyboard_KeyPressed;

            SetKeyboardControl (keyboard);

        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine ("Treba da pokrene server.");
            var dbPath = Properties.Settings.Default.DbPath;
            var connectionString = $"Data Source={dbPath};Version=3;";

            try
            {
                var server = new TcpIpServer (5000, connectionString);
                _ = Task.Run (async () => await server.StartAsync ());
                ClientRegistry.LoadFromFile ();

                Debug.WriteLine ("Server je uspješno pokrenut.");



            }
            catch(Exception ex)
            {
                // Ako je došlo do greške prilikom pokretanja servera
                Debug.WriteLine ("Greška prilikom pokretanja servera: " + ex.Message);


            }
        }



        public void ToggleKeyboard()
        {
            if(KeyboardHost.Visibility == Visibility.Visible)
                KeyboardHost.Visibility = Visibility.Collapsed;
            else
                KeyboardHost.Visibility = Visibility.Visible;
        }

        private void Keyboard_KeyPressed(string key)
        {
            if(Application.Current.Windows.OfType<MyInputBox> ().FirstOrDefault (w => w.IsActive) is MyInputBox inputBox)
            {
                if(inputBox.FocusedTextBox != null)
                {
                    inputBox.ReceiveKey (key);
                    return;
                }
            }
            if(MainContent.Content is IKeyboardInputReceiver receiver)
                receiver.ReceiveKey (key);
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


        private void FadeTransition(UserControl newPage)
        {
            newPage.Opacity = 0; // počni od nevidljive
            MainContent.Content = newPage;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds (0.5) // trajanje fadea
            };

            newPage.BeginAnimation (UIElement.OpacityProperty, fadeIn);
        }



        public void SlideInContent()
        {
            if(MainContent.Content is UIElement content)
            {
                // Postavi transformaciju
                TranslateTransform trans = new TranslateTransform ();
                content.RenderTransform = trans;

                // Postavi početnu poziciju (sa desne strane)
                trans.X = this.ActualWidth;

                // Animacija: X od širine prozora do 0
                DoubleAnimation slide = new DoubleAnimation
                {
                    From = this.ActualWidth,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds (800),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                trans.BeginAnimation (TranslateTransform.XProperty, slide);
            }
        }


    }
}