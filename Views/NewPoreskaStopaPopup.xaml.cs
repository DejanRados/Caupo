using Caupo.ViewModels;
using System.Globalization;
using System.Windows;

namespace Caupo.Views
{
    public partial class NewPoreskaStopaPopup : Window
    {
        private readonly SettingsViewModel _viewModel;

        public bool isUpdate = false;


        public NewPoreskaStopaPopup(
            SettingsViewModel viewModel)
        {
            InitializeComponent();

            _viewModel =
                viewModel;
        }


        private async void Save_Click(
            object sender,
            RoutedEventArgs e)
        {
            string opis =
                txtOpis.Text?.Trim()
                ?? string.Empty;

            string oznaka =
                txtOznaka.Text?.Trim()
                ?? string.Empty;


            if (string.IsNullOrWhiteSpace(opis))
            {
                MessageBox.Show(
                    "Unesite opis poreske stope.",
                    "Poreska stopa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                txtOpis.Focus();

                return;
            }


            if (!decimal.TryParse(
                txtPostotak.Text?.Trim(),
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal postotak))
            {
                MessageBox.Show(
                    "Unesite ispravan postotak poreske stope.",
                    "Poreska stopa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                txtPostotak.Focus();

                return;
            }


            if (postotak < 0 ||
               postotak > 100)
            {
                MessageBox.Show(
                    "Postotak mora biti između 0 i 100.",
                    "Poreska stopa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                txtPostotak.Focus();

                return;
            }


            if (string.IsNullOrWhiteSpace(oznaka))
            {
                MessageBox.Show(
                    "Unesite oznaku poreske stope.",
                    "Poreska stopa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                txtOznaka.Focus();

                return;
            }


            if (isUpdate)
            {
                if (!int.TryParse(
                    lblid.Content?.ToString(),
                    out int idStope))
                {
                    MessageBox.Show(
                        "Neispravan ID poreske stope.",
                        "Poreska stopa",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }


                await _viewModel
                    .UpdatePoreskaStopaAsync(
                        idStope,
                        opis,
                        postotak,
                        oznaka);
            }
            else
            {
                await _viewModel
                    .SaveNewPoreskaStopaAsync(
                        opis,
                        postotak,
                        oznaka);
            }


            DialogResult =
                true;

            Close();
        }


        private void Cancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult =
                false;

            Close();
        }
    }
}