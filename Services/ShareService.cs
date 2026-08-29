using System.Diagnostics;
using System.IO;

namespace Caupo.Services
{
    public class ShareService
    {
        public bool IsShareExists(string shareName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "net.exe",
                    Arguments = $"share \"{shareName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start (psi);

                if(process == null)
                    return false;

                process.WaitForExit ();

                return process.ExitCode == 0;
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[SHARE] Greška provjere share-a: {ex}");

                return false;
            }
        }


        public async Task CreateAndShareFolderAsync(
            string folderPath,
            string shareName)
        {
            if(string.IsNullOrWhiteSpace (folderPath))
                throw new ArgumentException (
                    "Folder nije definisan.",
                    nameof (folderPath));

            if(string.IsNullOrWhiteSpace (shareName))
                throw new ArgumentException (
                    "Share name nije definisan.",
                    nameof (shareName));


            folderPath = folderPath.Trim ();
            shareName = shareName.Trim ();


            Debug.WriteLine (
                $"[SHARE] Priprema foldera: {folderPath}");

            Debug.WriteLine (
                $"[SHARE] Naziv share-a: {shareName}");


            // =================================================
            // 1. KREIRAJ FOLDER AKO NE POSTOJI
            // =================================================

            if(!Directory.Exists (folderPath))
            {
                Directory.CreateDirectory (folderPath);

                Debug.WriteLine (
                    $"[SHARE] Folder kreiran: {folderPath}");
            }
            else
            {
                Debug.WriteLine (
                    $"[SHARE] Folder već postoji: {folderPath}");
            }


            // =================================================
            // 2. NTFS PRAVA
            // =================================================
            //
            // S-1-1-0 = Everyone
            //
            // Koristimo SID umjesto naziva "Everyone"
            // zato što naziv zavisi od jezika Windowsa.
            //
            // (OI) = fajlovi nasljeđuju prava
            // (CI) = folderi nasljeđuju prava
            // F    = Full Control
            //
            // SQLite mora moći kreirati i brisati
            // journal/WAL/SHM fajlove u ovom folderu.
            // =================================================

            Debug.WriteLine (
                "[SHARE] Postavljam NTFS prava...");


            await RunProcessAsync (
                "icacls.exe",
                $"\"{folderPath}\" /grant *S-1-1-0:(OI)(CI)F /T /C");


            Debug.WriteLine (
                "[SHARE] NTFS prava postavljena.");


            // =================================================
            // 3. AKO SHARE VEĆ POSTOJI
            // =================================================

            if(IsShareExists (shareName))
            {
                Debug.WriteLine (
                    $"[SHARE] Share već postoji: {shareName}");

                return;
            }


            // =================================================
            // 4. KREIRAJ WINDOWS SHARE
            // =================================================

            Debug.WriteLine (
                "[SHARE] Kreiram Windows share...");


            /*
             * OVDJE NEMA runas.
             *
             * App.xaml.cs je već restartovao Caupo
             * kao administrator prije ulaska u ovu metodu.
             */

            await RunProcessAsync (
                "net.exe",
                $"share \"{shareName}\"=\"{folderPath}\" /GRANT:Everyone,FULL /UNLIMITED");


            // =================================================
            // 5. PROVJERI REZULTAT
            // =================================================

            if(!IsShareExists (shareName))
            {
                throw new InvalidOperationException (
                    $"Windows share '{shareName}' nije kreiran.");
            }


            string networkPath =
                $@"\\{Environment.MachineName}\{shareName}";


            Debug.WriteLine (
                $"[SHARE] Share uspješno kreiran.");

            Debug.WriteLine (
                $"[SHARE] Mrežna putanja: {networkPath}");
        }


        // =====================================================
        // POKRETANJE WINDOWS KOMANDE
        // =====================================================

        private static async Task RunProcessAsync(
            string fileName,
            string arguments)
        {
            Debug.WriteLine (
                $"[SHARE CMD] {fileName} {arguments}");


            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,

                UseShellExecute = false,

                RedirectStandardOutput = true,
                RedirectStandardError = true,

                CreateNoWindow = true
            };


            using var process =
                new Process
                {
                    StartInfo = psi
                };


            process.Start ();


            Task<string> outputTask =
                process.StandardOutput.ReadToEndAsync ();

            Task<string> errorTask =
                process.StandardError.ReadToEndAsync ();


            await process.WaitForExitAsync ();


            string output =
                await outputTask;

            string error =
                await errorTask;


            if(!string.IsNullOrWhiteSpace (output))
            {
                Debug.WriteLine (
                    "[SHARE CMD OUTPUT] " +
                    output.Trim ());
            }


            if(!string.IsNullOrWhiteSpace (error))
            {
                Debug.WriteLine (
                    "[SHARE CMD ERROR] " +
                    error.Trim ());
            }


            Debug.WriteLine (
                $"[SHARE CMD] ExitCode = {process.ExitCode}");


            if(process.ExitCode != 0)
            {
                throw new InvalidOperationException (
                    $"Komanda nije uspješno izvršena.\n\n" +
                    $"{fileName} {arguments}\n\n" +
                    $"Exit code: {process.ExitCode}\n\n" +
                    (!string.IsNullOrWhiteSpace (error)
                        ? error
                        : output));
            }
        }
    }
}