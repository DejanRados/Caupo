using Caupo.Helpers;
using Caupo.Models;
using Caupo.Properties;
using Caupo.Services;
using Caupo.ViewModels;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : UserControl
    {
        private readonly ShareService _shareService = new ShareService ();
        public SettingsPage()
        {
            InitializeComponent ();
            DataContext = new SettingsViewModel ();
            LoadNetworkInterfaces ();
            LoadSettings ();

            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
            this.DataContext = new SettingsViewModel ();
        }
        private bool _loading = true;
        private void LoadSettings()
        {
            _loading = true;

            //Firma
            txtFrima.Text = Properties.Settings.Default.Firma;
            txtAdresa.Text = Properties.Settings.Default.Adresa;
            txtMjesto.Text = Properties.Settings.Default.Mjesto;
            txtJIB.Text = Properties.Settings.Default.JIB;
            txtPDV.Text = Properties.Settings.Default.PDV;
            txtZR.Text = Properties.Settings.Default.ZR;
            txtEmail.Text = Properties.Settings.Default.Email;
            // General settings
            string DbPath = Properties.Settings.Default.DbPath;
            string tema = Properties.Settings.Default.Tema;
            cmbTema.SelectedItem = cmbTema.Items.Cast<ComboBoxItem> ().FirstOrDefault (item => item.Content.ToString () == tema);

            string serverMode = Properties.Settings.Default.ServerMod;
            if(string.IsNullOrEmpty (serverMode) || serverMode == "NE")
            {
                cmbServerskiMod.SelectedItem = cmbServerskiMod.Items.Cast<ComboBoxItem> ().FirstOrDefault (item => item.Content.ToString () == "NE");
                txtDBPath.Visibility = Visibility.Collapsed;
                DbPathButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                cmbServerskiMod.SelectedItem = cmbServerskiMod.Items.Cast<ComboBoxItem> ().FirstOrDefault (item => item.Content.ToString () == "DA");
                txtDBPath.Visibility = Visibility.Visible;
                DbPathButton.Visibility = Visibility.Visible;
                txtDBPath.Text = DbPath;
            }
            txtBackup.Text = Properties.Settings.Default.BackupUrl;
            txtLogo.Text = Properties.Settings.Default.LogoUrl;

            string blokKopija = Properties.Settings.Default.BlokKopija;
            cmbBrojKopijaBloka.SelectedItem = cmbBrojKopijaBloka.Items.Cast<ComboBoxItem> ().FirstOrDefault (item => item.Content.ToString () == blokKopija);

            string prodajaMinus = Properties.Settings.Default.ProdajaMinus;
            cmbProdajaMinus.SelectedItem = cmbProdajaMinus.Items.Cast<ComboBoxItem> ().FirstOrDefault (item => item.Content.ToString () == prodajaMinus);

            string multiUser = Properties.Settings.Default.MultiUser;
            cmbMultiUser.SelectedItem = cmbMultiUser.Items.Cast<ComboBoxItem> ().FirstOrDefault (item => item.Content.ToString () == multiUser);

            string pdvKorisnik = Properties.Settings.Default.PDVKorisnik;
            cmbPDV.SelectedItem = cmbPDV.Items.Cast<ComboBoxItem> ().FirstOrDefault (item => item.Content.ToString () == pdvKorisnik);

            //string displayKuhinja = Properties.Settings.Default.DisplayKuhinja ?? "0";
            // cmbDisplejKuhinja.SelectedItem = cmbDisplejKuhinja.Items.Cast<SettingsViewModel.MonitorInfo> ().FirstOrDefault (m => m.Index == Convert.ToInt32(displayKuhinja));
            //Debug.WriteLine ("[Settings] load cmbDisplejKuhinja.SelectedItem = " + cmbDisplejKuhinja.Items.Cast<SettingsViewModel.MonitorInfo> ().FirstOrDefault (m => m.Index == Convert.ToInt32 (displayKuhinja)));
            // Printers
            txtPrinterRacuni.Text = Properties.Settings.Default.POSPrinter;
            txtPrinterA4.Text = Properties.Settings.Default.A4Printer;
            txtPrinterKuhinja.Text = Properties.Settings.Default.KuhinjaPrinter;
            txtPrinterSank.Text = Properties.Settings.Default.SankPrinter;


            // Fiscal device settings
            txtLPFRIP.Text = Properties.Settings.Default.LPFR_IP;
            txtLPFRAPI.Text = Properties.Settings.Default.LPFR_Key;
            txtLPFRPIN.Text = Properties.Settings.Default.LPFR_Pin;

            string extPrinter = Properties.Settings.Default.ExterniPrinter;
            cmbExterniPrinter.SelectedItem = cmbExterniPrinter.Items.Cast<ComboBoxItem> ().FirstOrDefault (item => item.Content.ToString () == extPrinter);
            string sirinaTrake = Properties.Settings.Default.SirinaTrake;
            cmbSirinaTrake.SelectedItem = cmbSirinaTrake.Items.Cast<ComboBoxItem> ().FirstOrDefault (item => item.Content.ToString () == sirinaTrake);

            // Network settings
            txtIPServer.Text = Properties.Settings.Default.ServerIP;

            // Server Mod


            _loading = false;
        }

        private async Task SaveSettings()
        {
            if(DataContext is SettingsViewModel viewModel)
            {
                var selectedMonitor = cmbDisplejKuhinja.SelectedItem as SettingsViewModel.MonitorInfo;
                //Firma
                Properties.Settings.Default.Firma = txtFrima.Text;
                Properties.Settings.Default.Adresa = txtAdresa.Text;
                Properties.Settings.Default.Mjesto = txtMjesto.Text;
                Properties.Settings.Default.JIB = txtJIB.Text;
                Properties.Settings.Default.PDV = txtPDV.Text;
                Properties.Settings.Default.ZR = txtZR.Text;
                Properties.Settings.Default.Email = txtEmail.Text;
                //Global
                Properties.Settings.Default.Tema = cmbTema.SelectedItem is ComboBoxItem temaItem ? temaItem.Content.ToString () : "Svijetla";
                Properties.Settings.Default.BackupUrl = txtBackup.Text;
                Properties.Settings.Default.LogoUrl = txtLogo.Text;
                Properties.Settings.Default.BlokKopija = cmbBrojKopijaBloka.SelectedItem is ComboBoxItem brojkopija ? brojkopija.Content.ToString () : "1";
                Properties.Settings.Default.ProdajaMinus = cmbProdajaMinus.SelectedItem is ComboBoxItem minus ? minus.Content.ToString () : "NE";
                Properties.Settings.Default.MultiUser = cmbMultiUser.SelectedItem is ComboBoxItem multi ? multi.Content.ToString () : "NE";
                Properties.Settings.Default.PDVKorisnik = cmbPDV.SelectedItem is ComboBoxItem pdv ? pdv.Content.ToString () : "DA";
                Properties.Settings.Default.DisplayKuhinja = (selectedMonitor?.Index ?? 0).ToString ();


                // Printers
                Properties.Settings.Default.POSPrinter = txtPrinterRacuni.Text;
                Properties.Settings.Default.A4Printer = txtPrinterA4.Text;
                Properties.Settings.Default.KuhinjaPrinter = txtPrinterKuhinja.Text;
                Properties.Settings.Default.SankPrinter = txtPrinterSank.Text;


                // Fiscal device settings
                Properties.Settings.Default.LPFR_IP = txtLPFRIP.Text;
                Properties.Settings.Default.LPFR_Key = txtLPFRAPI.Text;
                Properties.Settings.Default.LPFR_Pin = txtLPFRPIN.Text;
                Properties.Settings.Default.ExterniPrinter = cmbExterniPrinter.SelectedItem is ComboBoxItem ext ? ext.Content.ToString () : "DA";
                Properties.Settings.Default.SirinaTrake = cmbSirinaTrake.SelectedItem is ComboBoxItem traka ? traka.Content.ToString () : "57";

                // Network settings
                Properties.Settings.Default.ServerIP = txtIPServer.Text;


                //Server Mod
                if(cmbServerskiMod.SelectedItem is ComboBoxItem selectedItem)
                {
                    string servermode = selectedItem.Content.ToString ();
                    Debug.WriteLine ($"[DEBUG] Save clicked. Server mode: {servermode}");

                    if(servermode == "DA")
                    {
                        string rootFolder = txtDBPath.Text;
                        // Ako je rootFolder fajl, uzmi njegov direktorij
                        if(File.Exists (rootFolder))
                        {
                            rootFolder = Path.GetDirectoryName (rootFolder)
                                ?? throw new InvalidOperationException ("Invalid rootFolder path");
                        }

                        string dataFolder;

                        if(string.Equals (
                                Path.GetFileName (rootFolder),
                                "DsoftData",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            dataFolder = rootFolder;
                            Debug.WriteLine ($"[DEBUG] Root folder već sadrži DsoftData: {dataFolder}");
                        }
                        else
                        {
                            dataFolder = Path.Combine (rootFolder, "DsoftData");
                            Debug.WriteLine ($"[DEBUG] Data folder set to: {dataFolder}");
                        }

                        string targetDb = Path.Combine (dataFolder, "sysFormWPF.db");
                        string sourceDb = Path.Combine (
                            AppContext.BaseDirectory,
                            "Data",
                            "sysFormWPF.db"
                        );

                        // Kreiraj folder (idempotentno)
                        Directory.CreateDirectory (dataFolder);


                        // **Provjeri da li baza postoji**
                        if(!File.Exists (targetDb))
                        {
                            // Ako baza ne postoji → kopiraj je
                            File.Copy (sourceDb, targetDb, overwrite: false);
                            Debug.WriteLine ($"[DEBUG] Database copied from {sourceDb} to {targetDb}");
                        }
                        else
                        {
                            // Baza postoji → samo koristimo putanju
                            Debug.WriteLine ($"[DEBUG] Database already exists: {targetDb}");
                        }

                        // Snimi DbPath do fajla (NE foldera)
                        Properties.Settings.Default.ServerMod = "DA";
                        Properties.Settings.Default.DbPath = targetDb;
                        Properties.Settings.Default.Save ();
                        Globals.CurrentDbPath = targetDb;
                        Debug.WriteLine ($"[DEBUG] DbPath saved: {targetDb}");


                        // 4️⃣ Pišemo ElevationState za admin restart (samo share)
                        var state = new ElevationState
                        {
                            FolderPath = dataFolder,
                            ShareName = "DsoftData"
                        };

                        string tempPath = Path.Combine (Path.GetTempPath (), "elevation_state.json");
                        File.WriteAllText (tempPath, JsonSerializer.Serialize (state));
                        Debug.WriteLine ($"[DEBUG] ElevationState written to temp: {tempPath}");

                        // 5️⃣ Restart kao admin samo za share
                        Debug.WriteLine ("[DEBUG] Restarting as admin for share...");
                        AdminHelper.RestartAsAdmin ();
                    }
                    else
                    {
                        // NE → vraćamo se na lokalnu bazu

                        string localDbFolder = Path.Combine (Directory.GetCurrentDirectory (), "Data");
                        string dbPath = Path.Combine (localDbFolder, "sysFormWPF.db");
                        txtDBPath.Visibility = Visibility.Collapsed;
                        Properties.Settings.Default.ServerMod = "NE";
                        Properties.Settings.Default.DbPath = dbPath;
                        Properties.Settings.Default.Save ();
                        Globals.CurrentDbPath = dbPath;
                        Debug.WriteLine ($"[DEBUG] Local mode selected. DbPath set to: {dbPath}");
                    }
                }


                // Čuvanje podešavanja
                Properties.Settings.Default.Save ();

                MyMessageBox myMessageBox = new MyMessageBox ();
                ////myMessageBox.Owner = this;
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                myMessageBox.MessageTitle.Text = "Obavještenje";
                myMessageBox.MessageText.Text = "Podešavanja su spremljena!";
                myMessageBox.ShowDialog ();
                MainContent.Effect = null;
                CloseButton.IsEnabled = true;
                var page = new HomePage
                {
                    DataContext = new HomeViewModel ()
                };
                PageNavigator.Navigate?.Invoke (page);


            }
        }






        private async Task CreateAndShareFolderAsync(string folderPath, string shareName)
        {
            try
            {
                Debug.WriteLine ($"CreateAndShareFolderAsync - Folder: {folderPath}, Share: {shareName}");

                // 1️⃣ Kreiraj folder ako ne postoji
                bool folderExists = Directory.Exists (folderPath);
                Debug.WriteLine ($"Folder exists: {folderExists}");

                if(!folderExists)
                {
                    Directory.CreateDirectory (folderPath);
                    Debug.WriteLine ($"✓ Created folder: {folderPath}");
                }

                // 2️⃣ POKUŠAJ RUČNO DA VIDIŠ DA LI JE VEĆ SHARE-AN
                try
                {
                    Process checkProcess = new Process ();
                    checkProcess.StartInfo.FileName = "net";
                    checkProcess.StartInfo.Arguments = "share";
                    checkProcess.StartInfo.UseShellExecute = false;
                    checkProcess.StartInfo.RedirectStandardOutput = true;
                    checkProcess.StartInfo.CreateNoWindow = true;
                    checkProcess.Start ();

                    string output = await checkProcess.StandardOutput.ReadToEndAsync ();
                    checkProcess.WaitForExit ();

                    if(output.Contains (shareName))
                    {
                        Debug.WriteLine ($"✓ Share '{shareName}' already exists");
                        return; // Već je share-an
                    }
                }
                catch { }

                // 3️⃣ POKUŠAJ SA DRUGIM PARAMETRIMA
                var psi = new ProcessStartInfo
                {
                    FileName = "net",
                    Arguments = $"share {shareName}=\"{folderPath}\" /GRANT:Everyone,READ /UNLIMITED",
                    Verb = "runas", // UAC prompt
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                Debug.WriteLine ($"Starting net share: net {psi.Arguments}");

                bool success = false;
                await Task.Run (() =>
                {
                    try
                    {
                        var process = Process.Start (psi);
                        Debug.WriteLine ($"Process started, ID: {process?.Id}");
                        process.WaitForExit ();
                        Debug.WriteLine ($"Process exited with code: {process?.ExitCode}");

                        if(process?.ExitCode == 0)
                        {
                            success = true;
                            Debug.WriteLine ($"✓ Share '{shareName}' created successfully");
                        }
                        else
                        {
                            Debug.WriteLine ($"✗ Net share failed with exit code: {process?.ExitCode}");
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine ($"Process error: {ex.Message}");
                    }
                });

                if(!success)
                {
                    // POKUŠAJ ALTERNATIVNI NAČIN
                    Debug.WriteLine ("Trying alternative method...");

                    string command = $@"net share {shareName}=""{folderPath}"" /GRANT:Everyone,FULL";
                    Debug.WriteLine ($"Running command: {command}");

                    // Pokreni kao admin preko PowerShell-a
                    var psi2 = new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-Command \"Start-Process 'net' -ArgumentList 'share {shareName}=\\\"{folderPath}\\\" /GRANT:Everyone,FULL' -Verb RunAs\"",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };

                    try
                    {
                        var process2 = Process.Start (psi2);
                        process2.WaitForExit ();
                        Debug.WriteLine ($"PowerShell process exited with code: {process2?.ExitCode}");

                        if(process2?.ExitCode == 0)
                        {
                            MessageBox.Show ($"Folder '{folderPath}' je uspješno share-an kao '{shareName}'.\n\nPutanja mreže: \\\\{Environment.MachineName}\\{shareName}",
                                            "Uspjeh", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch { }
                }
            }
            catch(System.ComponentModel.Win32Exception winEx)
            {
                Debug.WriteLine ($"Win32Exception: {winEx.Message}, ErrorCode: {winEx.ErrorCode}");
                MessageBox.Show ("Operacija zahtijeva administratorske privilegije.",
                                "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch(Exception ex)
            {
                Debug.WriteLine ($"General error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show ($"Došlo je do greške: {ex.Message}",
                                "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsFolderShared(string folderPath, string shareName)
        {
            try
            {
                // Provjeri da li je folder već share-an
                Process process = new Process ();
                process.StartInfo.FileName = "net";
                process.StartInfo.Arguments = "share";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start ();

                string output = process.StandardOutput.ReadToEnd ();
                process.WaitForExit ();

                return output.Contains (shareName) || output.Contains (folderPath);
            }
            catch
            {
                return false;
            }
        }




        private void NoviRadnikButton_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is SettingsViewModel viewModel)
            {
                MainContent.Effect = new BlurEffect { Radius = 5 };
                NewWorkerPopup noviradnik = new NewWorkerPopup (viewModel);
                noviradnik.isUpdate = false;

                noviradnik.ShowDialog ();
                MainContent.Effect = null;
            }
        }



        BrushConverter bc = new BrushConverter ();
        private async Task<bool> CheckFirma()
        {
            await Task.Delay (5);

            if(string.IsNullOrWhiteSpace (txtFrima.Text))
            {
                txtFrima.BorderBrush = Brushes.Red;
                // txtFrima.Watermark = "Naziv firme je obavezan podatak";
                FirmaTab.IsSelected = true;
                await txtFrima.Dispatcher.BeginInvoke (new Action (() => txtFrima.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }
            else
            {
                txtFrima.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");

            }


            if(string.IsNullOrWhiteSpace (txtAdresa.Text))
            {
                txtAdresa.BorderBrush = Brushes.Red;
                //  txtAdresa.Watermark = "Adresa firme je obavezan podatak";
                FirmaTab.IsSelected = true;
                await txtAdresa.Dispatcher.BeginInvoke (new Action (() => txtAdresa.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;

            }
            else
            {
                txtAdresa.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");
            }
            if(string.IsNullOrWhiteSpace (txtMjesto.Text))
            {
                txtMjesto.BorderBrush = Brushes.Red;
                //   txtMjesto.Watermark = "Naziv mjesta firme je obavezan podatak";
                FirmaTab.IsSelected = true;
                await txtMjesto.Dispatcher.BeginInvoke (new Action (() => txtMjesto.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }
            else
            {
                txtMjesto.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");
            }
            if(string.IsNullOrWhiteSpace (txtJIB.Text))
            {
                txtJIB.BorderBrush = Brushes.Red;
                //   txtJIB.Watermark = "JIB firme je obavezan podatak";
                FirmaTab.IsSelected = true;
                await txtJIB.Dispatcher.BeginInvoke (new Action (() => txtJIB.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;

            }
            else
            {
                txtJIB.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");
            }

            if(string.IsNullOrWhiteSpace (txtEmail.Text))
            {
                txtEmail.BorderBrush = Brushes.Red;
                FirmaTab.IsSelected = true;
                await txtEmail.Dispatcher.BeginInvoke (new Action (() => txtEmail.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;

            }
            else
            {
                txtEmail.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");
            }


            // Provera za ComboBox
            if(cmbPDV.SelectedItem == null)
            {
                MessageBox.Show ("Podatak da li je firma PDV obaveznik je obavezan." + Environment.NewLine + "Izaberite opciju.", "UPOYORENJE", MessageBoxButton.OK, MessageBoxImage.Error);
                FirmaTab.IsSelected = true;
                await cmbPDV.Dispatcher.BeginInvoke (new Action (() => cmbPDV.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }
            else
            {
                txtFrima.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");
            }
            return true;
        }

        private async Task<bool> CheckFiskalizacija()
        {
            await Task.Delay (5);


            if(string.IsNullOrWhiteSpace (txtLPFRIP.Text))
            {
                txtLPFRIP.BorderBrush = Brushes.Red;
                //    txtLPFRIP.Watermark = "IP adresa LPFR-a je obavezan podatak";
                FiskalizacijaRSTab.IsSelected = true;
                await txtLPFRIP.Dispatcher.BeginInvoke (new Action (() => txtLPFRIP.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }
            else
            {
                txtLPFRIP.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");
            }

            if(string.IsNullOrWhiteSpace (txtLPFRAPI.Text))
            {
                txtLPFRAPI.BorderBrush = Brushes.Red;
                //  txtLPFRAPI.Watermark = "API ključ LPFR-a je obavezan podatak";
                FiskalizacijaRSTab.IsSelected = true;
                await txtLPFRAPI.Dispatcher.BeginInvoke (new Action (() => txtLPFRAPI.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }
            else
            {
                txtLPFRAPI.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");
            }

            if(string.IsNullOrWhiteSpace (txtLPFRPIN.Text))
            {
                txtLPFRPIN.BorderBrush = Brushes.Red;
                //   txtLPFRPIN.Watermark = "PIN LPFR-a je obavezan podatak";
                FiskalizacijaRSTab.IsSelected = true;
                await txtLPFRPIN.Dispatcher.BeginInvoke (new Action (() => txtLPFRPIN.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }
            else
            {
                txtLPFRPIN.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");
            }

            if(cmbExterniPrinter.SelectedItem == null)
            {
                MessageBox.Show ("Podatak da li se koristi eksterni printer je obavezan.\nIzaberite opciju.", "UPOZORENJE", MessageBoxButton.OK, MessageBoxImage.Error);
                FiskalizacijaRSTab.IsSelected = true;
                await cmbExterniPrinter.Dispatcher.BeginInvoke (new Action (() => cmbExterniPrinter.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }

            if(cmbSirinaTrake.SelectedItem == null)
            {
                MessageBox.Show ("Podatak o širini trake printera je obavezan.\nIzaberite opciju.", "UPOZORENJE", MessageBoxButton.OK, MessageBoxImage.Error);
                FiskalizacijaRSTab.IsSelected = true;
                await cmbSirinaTrake.Dispatcher.BeginInvoke (new Action (() => cmbSirinaTrake.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }
            return true;
        }

        private async Task<bool> CheckAplikacija()
        {
            if(string.IsNullOrWhiteSpace (txtPrinterRacuni.Text))
            {
                txtPrinterRacuni.BorderBrush = Brushes.Red;
                //   txtPrinterRacuni.Watermark = "Printer koji koristite za izdavanje računa je obavezan podatak";
                AplikacijaTab.IsSelected = true;
                await txtPrinterRacuni.Dispatcher.BeginInvoke (new Action (() => txtPrinterRacuni.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }
            else
            {
                txtPrinterRacuni.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");
            }

            if(string.IsNullOrWhiteSpace (txtBackup.Text))
            {
                txtBackup.BorderBrush = Brushes.Red;
                //  txtBackup.Watermark = "Lokacija rezerne kopije baze podataka je obavezan podatak";
                AplikacijaTab.IsSelected = true;
                await txtBackup.Dispatcher.BeginInvoke (new Action (() => txtBackup.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }
            else
            {
                txtBackup.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");
            }

            if(string.IsNullOrWhiteSpace (txtDBPath.Text) && txtDBPath.Visibility == Visibility.Visible)
            {
                txtDBPath.BorderBrush = Brushes.Red;
                //   txtDBPath.Watermark = "Lokacija baze podataka je obavezan podatak ako želite koristiti serverski mod";
                AplikacijaTab.IsSelected = true;
                await txtDBPath.Dispatcher.BeginInvoke (new Action (() => txtDBPath.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }
            else
            {
                txtDBPath.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");
            }

            if(cmbBrojKopijaBloka.SelectedItem == null)
            {
                MessageBox.Show ("Broj kopija blokova koje printate je obavezan podatak.\nIzaberite opciju.", "UPOZORENJE", MessageBoxButton.OK, MessageBoxImage.Error);
                AplikacijaTab.IsSelected = true;
                await cmbBrojKopijaBloka.Dispatcher.BeginInvoke (new Action (() => cmbBrojKopijaBloka.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }

            if(cmbProdajaMinus.SelectedItem == null)
            {
                MessageBox.Show ("Podešavanje dozvole prodaje u minus je obavezno.\nIzaberite opciju.", "UPOZORENJE", MessageBoxButton.OK, MessageBoxImage.Error);
                AplikacijaTab.IsSelected = true;
                await cmbProdajaMinus.Dispatcher.BeginInvoke (new Action (() => cmbProdajaMinus.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }

            if(cmbMultiUser.SelectedItem == null)
            {
                MessageBox.Show ("Podešavanje višekorisničkog okruženja je obavezno.\nIzaberite opciju.", "UPOZORENJE", MessageBoxButton.OK, MessageBoxImage.Error);
                AplikacijaTab.IsSelected = true;
                await cmbMultiUser.Dispatcher.BeginInvoke (new Action (() => cmbMultiUser.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }

            if(string.IsNullOrWhiteSpace (txtIPServer.Text))
            {
                txtIPServer.BorderBrush = Brushes.Red;
                //   txtIPServer.Watermark = "IP adresa internog servera je obavezan podatak";
                AplikacijaTab.IsSelected = true;
                await txtIPServer.Dispatcher.BeginInvoke (new Action (() => txtIPServer.Focus ()), System.Windows.Threading.DispatcherPriority.Background);
                return false;
            }
            else
            {
                txtIPServer.BorderBrush = (Brush)bc.ConvertFrom ("#FFABADB3");
            }

            return true;
        }


        private async void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            CloseButton.IsEnabled = false;
            bool firmaChecked = await CheckFirma ();
            if(!firmaChecked)
                return;
            bool fiskalizacijaChecked = await CheckFiskalizacija ();
            if(!fiskalizacijaChecked)
                return;
            bool aplikacijaChecked = await CheckAplikacija ();
            if(!aplikacijaChecked)
                return;
            MainContent.Effect = new BlurEffect { Radius = 8 };
            await SaveSettings ();

        }



        private void LogoButton_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Effect = new BlurEffect { Radius = 5 };
            // Kreiraj instancu OpenFileDialog-a
            OpenFileDialog openFileDialog = new OpenFileDialog ();

            // Postavi filtere za tipove slika
            openFileDialog.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";

            // Prikazivanje dijaloga i proveravanje da li je fajl odabran
            if(openFileDialog.ShowDialog () == true)
            {
                // Dodela odabrane slike u TextBox
                txtLogo.Text = openFileDialog.FileName;
            }
            MainContent.Effect = null;
        }

        private void SankButton_Click(object sender, RoutedEventArgs e)
        {
            PrinterDialog printerDialog = new PrinterDialog ();
            printerDialog.MessageTitle.Text = "Izaberite printer za šank";
            MainContent.Effect = new BlurEffect { Radius = 5 };
            if(printerDialog.ShowDialog () == true)
            {
                txtPrinterSank.Text = printerDialog.SelectedPrinter;
            }
            MainContent.Effect = null;
        }

        private void KuhinjaButton_Click(object sender, RoutedEventArgs e)
        {
            PrinterDialog printDialog = new PrinterDialog ();
            printDialog.MessageTitle.Text = "Izaberite printer za kuhinju";
            MainContent.Effect = new BlurEffect { Radius = 5 };
            // Prikazivanje dijaloga za odabir štampača
            if(printDialog.ShowDialog () == true)
            {
                // Dodela imena odabranog štampača u TextBox
                txtPrinterKuhinja.Text = printDialog.SelectedPrinter;
            }
            MainContent.Effect = null;
        }

        private void RacuniButton_Click(object sender, RoutedEventArgs e)
        {
            PrinterDialog printDialog = new PrinterDialog ();
            printDialog.MessageTitle.Text = "Izaberite printer za fiskalne račune";
            MainContent.Effect = new BlurEffect { Radius = 5 };
            // Prikazivanje dijaloga za odabir štampača
            if(printDialog.ShowDialog () == true)
            {
                // Dodela imena odabranog štampača u TextBox
                txtPrinterRacuni.Text = printDialog.SelectedPrinter;
            }
            MainContent.Effect = null;
        }

        private void A4Button_Click(object sender, RoutedEventArgs e)
        {
            PrinterDialog printDialog = new PrinterDialog ();
            printDialog.MessageTitle.Text = "Izaberite A4 priner ";
            MainContent.Effect = new BlurEffect { Radius = 5 };
            // Prikazivanje dijaloga za odabir štampača
            if(printDialog.ShowDialog () == true)
            {
                // Dodela imena odabranog štampača u TextBox
                txtPrinterA4.Text = printDialog.SelectedPrinter;
            }
            MainContent.Effect = null;
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Effect = new BlurEffect { Radius = 5 };
            var folderDialog = new OpenFolderDialog
            {
                Title = "Izaberite folder za rezervnu kopiju.",
                InitialDirectory = Environment.GetFolderPath (Environment.SpecialFolder.Desktop)
            };

            if(folderDialog.ShowDialog () == true)
            {
                txtBackup.Text = folderDialog.FolderName;

            }
            MainContent.Effect = null;
        }

        private void cmbInterfaces_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(cmbInterfaces.SelectedItem is NetworkInterface selectedInterface)
            {
                var ipAddress = selectedInterface.GetIPProperties ().UnicastAddresses
                    .Where (ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select (ip => ip.Address.ToString ())
                    .FirstOrDefault ();

                txtIPServer.Text = ipAddress ?? "Nema IP adrese";
            }
        }


        private void LoadNetworkInterfaces()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces ()
                .Where (ni => ni.OperationalStatus == OperationalStatus.Up)
                .ToList ();

            cmbInterfaces.ItemsSource = interfaces;
            cmbInterfaces.DisplayMemberPath = "Name";
        }

        private async void cmbTema_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine ("------------------ cmbTema_SelectionChanged --------------------");
            if(cmbTema.SelectedItem is ComboBoxItem selectedItem)
            {
                string tema = selectedItem.Content.ToString ();

                // Ažuriraj temu u aplikaciji
                App.CurrentTheme = tema;
                App.ApplyTheme (tema);

                // Spremi odabranu temu u postavke
                Settings.Default.Tema = tema;
                Settings.Default.Save ();


                if(tema == "Tamna")
                {
                    Application.Current.Resources["GlobalFontColor"] = new SolidColorBrush (System.Windows.Media.Color.FromRgb (244, 244, 244));
                    Application.Current.Resources["GlobalBackgroundColor"] = new SolidColorBrush (System.Windows.Media.Color.FromRgb (45, 45, 48));
                }
                else
                {
                    Application.Current.Resources["GlobalFontColor"] = new SolidColorBrush (System.Windows.Media.Color.FromRgb (45, 45, 48));
                    Application.Current.Resources["GlobalBackgroundColor"] = new SolidColorBrush (System.Windows.Media.Color.FromRgb (244, 244, 244));

                }
                // Ažuriraj slike u ViewModel-u
                if(DataContext is SettingsViewModel viewModel)
                {
                    viewModel.SetImage ();
                }
            }
            else
            {
                Debug.WriteLine ("------------------ Nije cmbTema.SelectedItem is ComboBoxItem selectedItem --------------------");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var page = new HomePage ();
            page.DataContext = new HomeViewModel ();
            PageNavigator.NavigateWithFade (page);

        }

        private void cmbServerskiMod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(_loading)
                return;

            if(cmbServerskiMod.SelectedItem is ComboBoxItem selectedItem)
            {
                string servermode = selectedItem.Content.ToString ();

                if(servermode == "DA")
                {
                    MainContent.Effect = new BlurEffect { Radius = 5 };
                    MyMessageBox myMessageBox = new MyMessageBox ();
                    // //myMessageBox.Owner = this;
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                    myMessageBox.MessageTitle.Text = "Obavještenje";
                    myMessageBox.MessageText.Text = "Izaberite folder u kome će se nalaziti baza podataka";
                    myMessageBox.ShowDialog ();
                    MainContent.Effect = null;
                    txtDBPath.Visibility = Visibility.Visible;
                    DbPathButton.Visibility = Visibility.Visible;
                }
                else
                {
                    txtDBPath.Visibility = Visibility.Collapsed;
                    DbPathButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void DbPathButton_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Effect = new BlurEffect { Radius = 5 };
            var folderDialog = new OpenFolderDialog
            {
                Title = "Izaberite folder za bazu podataka.",
                InitialDirectory = Environment.GetFolderPath (Environment.SpecialFolder.Desktop)
            };

            if(folderDialog.ShowDialog () == true)
            {
                txtDBPath.Text = folderDialog.FolderName;

            }
            MainContent.Effect = null;
        }

        private void Button_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Pronađi parent ListViewItem
            var button = (Button)sender;
            var listViewItem = FindAncestor<ListViewItem> (button);

            if(listViewItem != null && !listViewItem.IsSelected)
            {
                listViewItem.IsSelected = true; // selektuje red
            }

            // Ne postavljaj e.Handled = true !!!
            // Ostavlja događaj za dugme da izvrši Command
        }

        // Helper metoda za pronalaženje roditelja
        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while(current != null)
            {
                if(current is T)
                    return (T)current;
                current = VisualTreeHelper.GetParent (current);
            }
            return null;
        }

        private void Button_PreviewMouseLeftButtonDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Pronađi parent ListViewItem
            var button = (Button)sender;
            var listViewItem = FindAncestor<ListViewItem> (button);

            if(listViewItem != null && !listViewItem.IsSelected)
            {
                listViewItem.IsSelected = true; // selektuje red
            }

            // Ne postavljaj e.Handled = true !!!
            // Ostavlja događaj za dugme da izvrši Command
        }
    }
}

