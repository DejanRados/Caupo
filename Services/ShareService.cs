using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Caupo.Services
{
        public class ShareService
        {
        public bool IsShareExists(string shareName)
        {
            try
            {
                Process checkProcess = new Process ();
                checkProcess.StartInfo.FileName = "net";
                checkProcess.StartInfo.Arguments = "share";
                checkProcess.StartInfo.UseShellExecute = false;
                checkProcess.StartInfo.RedirectStandardOutput = true;
                checkProcess.StartInfo.CreateNoWindow = true;
                checkProcess.Start ();

                string output = checkProcess.StandardOutput.ReadToEnd ();
                checkProcess.WaitForExit ();

                return output.Contains (shareName);
            }
            catch
            {
                return false;
            }
        }

        public async Task CreateAndShareFolderAsync(string folderPath, string shareName)
            {
                try
                {
                    // Kreiraj folder ako ne postoji
                    if(!Directory.Exists (folderPath))
                        Directory.CreateDirectory (folderPath);

                    // Provjeri postoji li share
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
                        return; // Share već postoji, ništa se ne radi

                    // Kreiranje share (admin UAC prompt)
                    var psi = new ProcessStartInfo
                    {
                        FileName = "net",
                        Arguments = $"share {shareName}=\"{folderPath}\" /GRANT:Everyone,FULL",
                        Verb = "runas",           // UAC prompt se pojavljuje samo jednom
                        UseShellExecute = true,
                        CreateNoWindow = true
                    };

                    await Task.Run (() =>
                    {
                        var process = Process.Start (psi);
                        process.WaitForExit ();
                    });
                }
                catch(System.ComponentModel.Win32Exception)
                {
                    System.Windows.MessageBox.Show ("Operacija zahtijeva administratorske privilegije.",
                                    "Greška", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                catch(Exception ex)
                {
                    System.Windows.MessageBox.Show ($"Došlo je do greške: {ex.Message}",
                                    "Greška", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }


