using System.Windows;
using System.Windows.Controls;

namespace Caupo.UserControls
{
    public partial class CaupoHeader : UserControl
    {
        public CaupoHeader()
        {
            InitializeComponent();
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