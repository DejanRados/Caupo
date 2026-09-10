using System.Windows;

namespace Caupo.Views
{
    public partial class YesNoPopup : Window
    {
        public string PopupTitle
        {
            get => MessageTitle.Text;
            set => MessageTitle.Text = value;
        }

        public string PopupMessage
        {
            get => MessageText.Text;
            set => MessageText.Text = value;
        }

        public string ConfirmText
        {
            get => ConfirmButtonText.Text;
            set => ConfirmButtonText.Text = value;
        }

        public string CancelText
        {
            get => CancelButtonText.Text;
            set => CancelButtonText.Text = value;
        }

        public string Kliknuo { get; private set; } = "Ne";

        public YesNoPopup()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Kliknuo = "Ne";
            DialogResult = false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Kliknuo = "Da";
            DialogResult = true;
        }
    }
}