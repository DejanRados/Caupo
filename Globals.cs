using static Caupo.Data.DatabaseTables;

namespace Caupo
{
    public class Globals
    {
        public static TblRadnici? ulogovaniKorisnik;
        public static bool treba_blok_za_kuhinju = false;
        public static string CurrentDbPath { get; set; } = Properties.Settings.Default.DbPath;
        public static string? forma = null;
    }
}
