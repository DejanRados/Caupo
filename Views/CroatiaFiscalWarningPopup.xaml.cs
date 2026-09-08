using Caupo.Data;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Input;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Views
{
    public partial class CroatiaFiscalWarningPopup : Window
    {
        public string WarningText { get; }
        public TblRadnici? ConfirmedWorker { get; private set; }

        public CroatiaFiscalWarningPopup(string warningText)
        {
            InitializeComponent();

            WarningText = warningText;
            DataContext = this;

            Loaded += (_, _) => txtPassword.Focus();
        }

        private async void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            await ConfirmAsync();
        }

        private async void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await ConfirmAsync();
        }

        private async Task ConfirmAsync()
        {
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(password))
            {
                txtError.Text = "Unesite šifru.";
                txtPassword.Focus();
                return;
            }

            try
            {
                await using var db = new AppDbContext();

                TblRadnici? worker = await db.Radnici
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Lozinka == password);

                if (worker == null)
                {
                    txtError.Text = "Pogrešna šifra.";
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                if (!string.Equals(worker.Dozvole, "Administrator", StringComparison.OrdinalIgnoreCase))
                {
                    txtError.Text = "Za potvrdu ovog upozorenja potrebna je administratorska šifra.";
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                ConfirmedWorker = worker;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                txtError.Text = "Nije moguće provjeriti šifru: " + ex.Message;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (DialogResult != true)
                e.Cancel = true;

            base.OnClosing(e);
        }
    }
}