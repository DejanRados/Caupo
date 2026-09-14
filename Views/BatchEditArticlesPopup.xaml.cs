using System.Windows;
using System.Windows.Controls;

namespace Caupo.Views
{
    public partial class BatchEditArticlesPopup : Window
    {
        public BatchEditArticlesPopup(int selectedCount)
        {
            InitializeComponent();

            SelectedCountTextBlock.Text = $"Označeno: {selectedCount} artikala";
        }

        private void PriceOperationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ValueLabel == null || PriceOperationComboBox.SelectedIndex < 0)
                return;

            ValueLabel.Text = PriceOperationComboBox.SelectedIndex == 0 ? "Nova cijena" : "Postotak";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}