using System.Windows;
using System.Windows.Controls;

namespace Caupo.UserControls
{
    public partial class CaupoHeader : UserControl
    {
        public CaupoHeader()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                var source = DependencyPropertyHelper.GetValueSource(
                    lblUlogovaniKorisnik,
                    Control.ForegroundProperty);
                lblUlogovaniKorisnik.Text = "TEST HEADER 123";
                lblUlogovaniKorisnik.Background = System.Windows.Media.Brushes.Yellow;
                lblUlogovaniKorisnik.Foreground = System.Windows.Media.Brushes.Red;

                System.Diagnostics.Debug.WriteLine(
    $"[HEADER] Text='{lblUlogovaniKorisnik.Text}', " +
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
            };

            lblUlogovaniKorisnik.Text = Globals.ulogovaniKorisnik?.Radnik ?? string.Empty;
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