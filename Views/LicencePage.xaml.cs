using ControlzEx.Standard;
using Caupo.Properties;
using Caupo.ViewModels;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
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
using Caupo.Helpers;

namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for LicencePage.xaml
    /// </summary>
    public partial class LicencePage : UserControl
    {
        public LicencePage()
        {
            InitializeComponent();
        }

        // Code-behind
        private void TextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PasteFromClipboard (sender as TextBox);
        }

        private void TextBox_TouchDown(object sender, TouchEventArgs e)
        {
            PasteFromClipboard (sender as TextBox);
        }

        private void PasteFromClipboard(TextBox textBox)
        {
            if (textBox == null) return;

            if (Clipboard.ContainsText ())
            {
                textBox.Text = Clipboard.GetText ();
                textBox.CaretIndex = textBox.Text.Length; // postavi kursor na kraj
            }
        }


        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lblId.Content == null || txtKey.Text == null || txtKey.Text == "")
                {
                    MyMessageBox myMessageBox = new MyMessageBox();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "GREŠKA";
                    myMessageBox.MessageText.Text = "Morate upisati licencni ključ!";
                    myMessageBox.ShowDialog();
                }
                else
                {

                    if (!ValidateLicenseKey(txtKey.Text))
                    {
                        txtKey.Text = "";
                        txtKey.Focus();
                        return;
                        // Application.Current.Shutdown();
                    }
                    else
                    {
                        Settings.Default.Key = txtKey.Text;
                        Settings.Default.Save();
                        MyMessageBox myMessageBox = new MyMessageBox();
                        ////myMessageBox.Owner = this;
                        myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                        myMessageBox.MessageTitle.Text = "LICENCNI KLJUČ";
                        myMessageBox.MessageText.Text = "Vaša licencni ključ je ispravan" + Environment.NewLine + "Uspješno ste licencirali aplikaciju.";
                        myMessageBox.ShowDialog();
                        var page = new LoginPage();
                        page.DataContext = new LoginPageViewModel();
                        PageNavigator.NavigateWithFade (page);
                    }




                }
            }
            catch (Exception ex) { 
             Debug.WriteLine("Klik na OK exception:" + ex.Message + ", " + ex.StackTrace);
            
            }
        }

        private static readonly byte[] Key = Encoding.UTF8.GetBytes("12345678901234567890123456789000"); // 32 bytes for AES-256
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("1234567890123456"); // 16 bytes for AES

        string Decrypt(string cipherText)
        {
            try
            {
                Debug.WriteLine($"  string Decrypt(string cipherText): {cipherText}");
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
                    using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                    using var reader = new StreamReader(cs);
                    string result = reader.ReadToEnd();
                    Debug.WriteLine($"Decrypted Data: {result}");
                    return result;
                }
            }
            catch (FormatException fe)
            {
                Debug.WriteLine($"FormatException fe.Message: " + fe.Message + Environment.NewLine  + "FormatException fe.StackTrace: "+ fe.StackTrace);
                MyMessageBox myMessageBox = new MyMessageBox();
                ////myMessageBox.Owner = this;
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                myMessageBox.MessageTitle.Text = "LICENCNI KLJUČ";
                myMessageBox.MessageText.Text = "Vaša licencni ključ je pogrešan" + Environment.NewLine + "Naručite novi licencni ključ ukoliko želite dalje koristiti aplikaciju.";
                myMessageBox.ShowDialog();
                return string.Empty;
            }
            catch (CryptographicException ce)
            {
                Debug.WriteLine($"CryptographicException ce.Message: " + ce.Message + Environment.NewLine + "CryptographicException ce.StackTrace: " + ce.StackTrace);
                MyMessageBox myMessageBox = new MyMessageBox();
                ////myMessageBox.Owner = this;
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                myMessageBox.MessageTitle.Text = "LICENCNI KLJUČ";
                myMessageBox.MessageText.Text = "Vaša licencni ključ ne odgovara za ovaj kompjuter" + Environment.NewLine + "Naručite novi licencni ključ ukoliko želite dalje koristiti aplikaciju.";
                myMessageBox.ShowDialog();

                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Neočekivana greška: {ex.Message}",
                                               "Greška",
                                               MessageBoxButton.OK,
                                               MessageBoxImage.Error);
                return string.Empty;
            }
        }

        bool isHardwareCorrect;
        bool isYearLicence;
        bool isTrialLicence;
        DateTime installationDate;
        TimeSpan installPeriod;

        private bool ValidateLicenseKey(string licenseKey)
        {
            string licenseType;
            string hardwareFingerprint;
            string date;
            try
            {
                Debug.WriteLine(" ValidateLicenseKey(string licenseKey) " + licenseKey);
                string decryptedData = Decrypt(licenseKey);
                string[] parts = decryptedData.Split('-');
                if (parts.Length != 3)
                {
                    return false;
                }
                licenseType = parts[0];
                hardwareFingerprint = parts[1];
                date = parts[2];
                // - Verifying the hardware fingerprint matches the current device
                Debug.WriteLine("------------------------------------------------ hardwareFingerprint ---- " + hardwareFingerprint);
                string fingerprint = GetHardwareInfo("Win32_Processor", "ProcessorId");


                string localHardwareFingerprint = fingerprint.ToString();
                if (localHardwareFingerprint != hardwareFingerprint)
                {



                    MyMessageBox myMessageBox = new MyMessageBox();
                    ////myMessageBox.Owner = this;
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                    myMessageBox.MessageTitle.Text = "LICENCNI KLJUČ";
                    myMessageBox.MessageText.Text = "Vaša licencni ključ ne odgovara za ovaj kompjuter" + Environment.NewLine + "Naručite novi licencni ključ ukoliko želite dalje koristiti aplikaciju.";
                    myMessageBox.ShowDialog();
                    return false;
                }
                // - Checking if the date is within the valid range
                Debug.WriteLine("------------------------------------------------ date---- " + date);
                // - Ensuring the license type is valid
                installationDate = DateTime.ParseExact(date, "dd.MM.yy", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
                DateTime currentDate = DateTime.Now;
                installPeriod = currentDate - installationDate;
                switch (licenseType)
                {
                    case "Permanentna":
                        return true;
                    case "Godišnja":
                        if (installPeriod.TotalDays < 365)
                        {
                            return true;
                        }
                        else
                        {

                            MyMessageBox myMessageBox = new MyMessageBox();
                            ////myMessageBox.Owner = this;
                            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                            myMessageBox.MessageTitle.Text = "GODIŠNJA LICENCA";
                            myMessageBox.MessageText.Text = "Vaša licenca je istekla " + Environment.NewLine + "Vrijedila je 365 dana od dana  " + installationDate + Environment.NewLine + "Naručite novi licencni ključ ukoliko želite dalje koristiti aplikaciju.";
                            myMessageBox.ShowDialog();

                            return false;
                        }
                    case "Probna":
                        if (installPeriod.TotalDays < 30)
                        {
                            return true;
                        }
                        else
                        {

                            MyMessageBox myMessageBox = new MyMessageBox();
                            ////myMessageBox.Owner = this;
                            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                            myMessageBox.MessageTitle.Text = "PROBNA LICENCA";
                            myMessageBox.MessageText.Text = "Vaša licenca je istekla " + Environment.NewLine + "Vrijedila je 30 dana od dana  " + installationDate + Environment.NewLine + "Naručite novi licencni ključ ukoliko želite dalje koristiti aplikaciju.";
                            myMessageBox.ShowDialog();

                            return false;
                        }
                    default:
                        throw new InvalidOperationException("Unknown license type.");
                }

            }
            catch
            {
                return false; // Decryption or validation failed
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
           
            Application.Current.Shutdown();
        }

     
        private static string GetHardwareInfo(string wmiClass, string property)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj[property] != null)
                    {
                        return obj[property].ToString();
                    }
                }
            }
            return string.Empty;
        }

     

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string fingerprint = GetHardwareInfo("Win32_Processor", "ProcessorId");
            lblId.Content = fingerprint.ToString();
        }

        private void lblId_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Label label)
            {
                // Kopiraj tekst u clipboard
                Clipboard.SetText(label.Content.ToString());



                MyMessageBox myMessageBox = new MyMessageBox();
                ////myMessageBox.Owner = this;
                myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        
                myMessageBox.MessageTitle.Text = "Obavještenje";
                myMessageBox.MessageText.Text = "Tekst je kopiran!";
                myMessageBox.ShowDialog();
            }
        }

        private void txtKey_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
