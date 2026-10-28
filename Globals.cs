
using static Caupo.Data.DatabaseTables;

namespace Caupo
{
    public class Globals
    {
        private static TblRadnici? _ulogovaniKorisnik;

        public static event EventHandler? UlogovaniKorisnikChanged;

        public static TblRadnici? ulogovaniKorisnik
        {
            get => _ulogovaniKorisnik;
            set
            {
                if (ReferenceEquals(_ulogovaniKorisnik, value))
                    return;

                _ulogovaniKorisnik = value;
                UlogovaniKorisnikChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static bool treba_blok_za_kuhinju = false;
        public static string CurrentDbPath { get; set; } = Properties.Settings.Default.DbPath;
    }
}
