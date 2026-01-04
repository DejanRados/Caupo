
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Caupo.Data;
using Caupo.ViewModels;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for MyMessageBox.xaml
    /// </summary>
    public partial class NewWorkerPopup : Window
    {
        public bool isUpdate = false;
        private readonly SettingsViewModel _viewModel;
        public NewWorkerPopup(SettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.DataContext = _viewModel;
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtRadnik.Text))
            {
                txtRadnik.BorderBrush = Brushes.Red;
               
              


                MyMessageBox myMessageBox = new MyMessageBox();
                //myMessageBox.Owner = this;
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
                myMessageBox.MessageTitle.Text = "UPOZORENJE";
                myMessageBox.MessageText.Text = "Ime i prezime radnika je obavezan podatak." + Environment.NewLine + "Unesite ime i prezime radnika.";
                myMessageBox.ShowDialog();
                txtRadnik.Focus();
                return;
            }
            else
            {
                txtRadnik.BorderBrush = Brushes.LightGray;
            }

            if (string.IsNullOrEmpty(txtLozinka.Text))
            {
                txtLozinka.BorderBrush = Brushes.Red;

        


                MyMessageBox myMessageBox = new MyMessageBox();
                //myMessageBox.Owner = this;
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
              
                myMessageBox.MessageTitle.Text = "UPOZORENJE";
                myMessageBox.MessageText.Text = "Lozinka za radnika je obavezan podatak." + Environment.NewLine + "Unesite lozinku za radnika.";
                myMessageBox.ShowDialog();
                txtLozinka.Focus();
                return;
            }
            else
            {
                txtLozinka.BorderBrush = Brushes.LightGray;
            }

            if (cmbDozvole.SelectedIndex == -1)
            {
                cmbDozvole.BorderBrush = Brushes.Red;

       


                MyMessageBox myMessageBox = new MyMessageBox();
                //myMessageBox.Owner = this;
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
      
                myMessageBox.MessageTitle.Text = "UPOZORENJE";
                myMessageBox.MessageText.Text = "Morate izabrati dopuštenja za radnika.";
                myMessageBox.ShowDialog();
                cmbDozvole.Focus();
                return;
            }
            else
            {
                cmbDozvole.BorderBrush = Brushes.LightGray;
            }

            DatabaseTables.TblRadnici radnik = new DatabaseTables.TblRadnici();
           
            radnik.Radnik = txtRadnik.Text;
            radnik.Lozinka = txtLozinka.Text;
            radnik.Dozvole = cmbDozvole.Text;
       
                if (isUpdate) {
                    radnik.IdRadnika = (int)lblid.Content;
                    Debug.WriteLine("Radi update");
                    await _viewModel.UpdateRadnik(radnik);
                    await _viewModel.LoadRadniciAsync();
                }
                else
                {
                    Debug.WriteLine("Radi novog");
                    await _viewModel.NewRadnik(radnik);
                }
             
            
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close ();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
          
            if (isUpdate)
            {


                if (DataContext is SettingsViewModel viewModel)
                {
                    if (viewModel.SelectedRadnik != null)
                    {
                        Debug.WriteLine(viewModel.SelectedRadnik.Radnik);
                        txtRadnik.Text = viewModel.SelectedRadnik.Radnik;
                        Debug.WriteLine(viewModel.SelectedRadnik.Lozinka);
                        txtLozinka.Text = viewModel.SelectedRadnik.Lozinka;
                        Debug.WriteLine(viewModel.SelectedRadnik.Dozvole);
                        cmbDozvole.Text = viewModel.SelectedRadnik.Dozvole;


                    }
                    else
                    {
                        Debug.WriteLine("SelectedRadnik is null");
                    }

                }
            
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtRadnik.Focus();
                Keyboard.Focus(txtRadnik);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }
}
