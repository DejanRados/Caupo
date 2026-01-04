using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caupo.Properties;
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
