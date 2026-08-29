using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace Caupo.Helpers
{
    public static class AdminHelper
    {
        public static bool IsAdministrator()
        {
            using WindowsIdentity identity =
                WindowsIdentity.GetCurrent ();

            WindowsPrincipal principal =
                new WindowsPrincipal (identity);

            return principal.IsInRole (
                WindowsBuiltInRole.Administrator);
        }


        public static void RestartAsAdmin()
        {
            string? exePath =
                Environment.ProcessPath;

            if(string.IsNullOrWhiteSpace (exePath))
            {
                throw new InvalidOperationException (
                    "Nije moguće pronaći Caupo.exe.");
            }


            var psi =
                new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    Verb = "runas",
                    WorkingDirectory =
                        AppContext.BaseDirectory
                };


            Process.Start (psi);

            Application.Current.Shutdown ();
        }


        public static void RestartAsUser()
        {
            string? exePath =
                Environment.ProcessPath;

            if(string.IsNullOrWhiteSpace (exePath))
            {
                throw new InvalidOperationException (
                    "Nije moguće pronaći Caupo.exe.");
            }


            var psi =
                new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true,
                    WorkingDirectory =
                        AppContext.BaseDirectory
                };


            Process.Start (psi);

            Application.Current.Shutdown ();
        }
    }
}