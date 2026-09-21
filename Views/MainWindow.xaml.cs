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
    public partial class MainWindow : Window
    {
        // =====================================================
        // INSTANCE
        // =====================================================

        public static MainWindow? Instance { get; private set; }


        // =====================================================
        // FIELDS
        // =====================================================

        private readonly VirtualKeyboard _keyboard;

        private TcpIpServer? _server;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public MainWindow()
        {
            InitializeComponent ();


            Instance =
                this;


            // =================================================
            // VIRTUALNA TASTATURA
            // =================================================

            _keyboard =
                new VirtualKeyboard ();


            _keyboard.KeyPressed +=
                Keyboard_KeyPressed;


            SetKeyboardControl (
                _keyboard);
        }


        // =====================================================
        // WINDOW LOADED
        // =====================================================

        private void Window_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Debug.WriteLine (
                "[SERVER] Pokrećem TCP server.");


            string dbPath =
                Properties.Settings.Default.DbPath;


            string connectionString =
                $"Data Source={dbPath};Version=3;";


            try
            {
                _server =
                    new TcpIpServer (
                        5000,
                        connectionString);


                _ = Task.Run (
                    async () =>
                    {
                        await _server.StartAsync ();
                    });


                ClientRegistry.LoadFromFile ();


                Debug.WriteLine (
                    "[SERVER] TCP server je pokrenut iz MainWindow.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[SERVER] Greška pri pokretanju TCP servera: {ex}");
            }
        }


        // =====================================================
        // TOGGLE KEYBOARD
        // =====================================================

        public void ToggleKeyboard()
        {
            KeyboardHost.Visibility =
                KeyboardHost.Visibility ==
                Visibility.Visible

                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }


        // =====================================================
        // KEYBOARD KEY PRESSED
        // =====================================================

        private void Keyboard_KeyPressed(string key)
        {
            if (MainContent.Content is IKeyboardInputReceiver receiver)
            {
                receiver.ReceiveKey(key);
            }
        }


        // =====================================================
        // SHOW KEYBOARD
        // =====================================================

        public void ShowKeyboard()
        {
            KeyboardHost.Visibility =
                Visibility.Visible;
        }


        // =====================================================
        // HIDE KEYBOARD
        // =====================================================

        public void HideKeyboard()
        {
            KeyboardHost.Visibility =
                Visibility.Collapsed;
        }


        // =====================================================
        // SET KEYBOARD CONTROL
        // =====================================================

        public void SetKeyboardControl(
            VirtualKeyboard keyboard)
        {
            KeyboardHost.Content =
                keyboard;
        }


        // =====================================================
        // SLIDE ANIMATION
        // =====================================================

        public void SlideInContent()
        {
            if(MainContent.Content
               is not UIElement content)
            {
                return;
            }


            var transform =
                new TranslateTransform ();


            content.RenderTransform =
                transform;


            transform.X =
                ActualWidth;


            var animation =
                new DoubleAnimation
                {
                    From =
                        ActualWidth,

                    To =
                        0,

                    Duration =
                        TimeSpan.FromMilliseconds (
                            800),

                    EasingFunction =
                        new QuadraticEase
                        {
                            EasingMode =
                                EasingMode.EaseInOut
                        }
                };


            transform.BeginAnimation (
                TranslateTransform.XProperty,
                animation);
        }
    }
}

