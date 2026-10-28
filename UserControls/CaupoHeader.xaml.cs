
using System.Windows;
using System.Windows.Controls;

namespace Caupo.UserControls
{
    public partial class CaupoHeader : UserControl
    {
        public CaupoHeader()
        {
            InitializeComponent();

            Loaded += CaupoHeader_Loaded;
            Unloaded += CaupoHeader_Unloaded;

            RefreshLoggedInUser();
        }

        private void CaupoHeader_Loaded(object sender, RoutedEventArgs e)
        {
            Globals.UlogovaniKorisnikChanged -= Globals_UlogovaniKorisnikChanged;
            Globals.UlogovaniKorisnikChanged += Globals_UlogovaniKorisnikChanged;

            RefreshLoggedInUser();

            var source = DependencyPropertyHelper.GetValueSource(lblUlogovaniKorisnik, Control.ForegroundProperty);

            System.Diagnostics.Debug.WriteLine(
                $"[HEADER] Text='{lblUlogovaniKorisnik.Content}', " +
                $"Width={lblUlogovaniKorisnik.ActualWidth}, " +
                $"Height={lblUlogovaniKorisnik.ActualHeight}, " +
                $"Visibility={lblUlogovaniKorisnik.Visibility}, " +
                $"Opacity={lblUlogovaniKorisnik.Opacity}, " +
                $"Foreground={lblUlogovaniKorisnik.Foreground}");

            System.Diagnostics.Debug.WriteLine(
                $"[HEADER] Foreground izvor: {source.BaseValueSource}, " +
                $"Expression: {source.IsExpression}, " +
                $"Animated: {source.IsAnimated}, " +
                $"Coerced: {source.IsCoerced}");
        }

        private void CaupoHeader_Unloaded(object sender, RoutedEventArgs e)
        {
            Globals.UlogovaniKorisnikChanged -= Globals_UlogovaniKorisnikChanged;
        }

        private void Globals_UlogovaniKorisnikChanged(object? sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess())
                RefreshLoggedInUser();
            else
                Dispatcher.BeginInvoke(RefreshLoggedInUser);
        }

        private void RefreshLoggedInUser()
        {
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik?.Radnik ?? string.Empty;
        }

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(CaupoHeader), new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(CaupoHeader), new PropertyMetadata(string.Empty));

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public event RoutedEventHandler? Close;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close?.Invoke(this, e);
        }
    }
}
