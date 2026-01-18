using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace Caupo.Helpers
{


    public static class AdminHelper
    {
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent ();
            var principal = new WindowsPrincipal (identity);
            return principal.IsInRole (WindowsBuiltInRole.Administrator);
        }

        public static void RestartAsAdmin()
        {
            var psi = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess ().MainModule.FileName,
                UseShellExecute = true,
                Verb = "runas" // UAC prompt
            };
            Process.Start (psi);
            Application.Current.Shutdown ();
        }

        public static void RestartAsUser()
        {
            var psi = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess ().MainModule.FileName,
                UseShellExecute = true
            };
            Process.Start (psi);
            Application.Current.Shutdown ();
        }
    }
}


