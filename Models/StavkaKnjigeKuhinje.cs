using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Caupo.Models
{
    public class StavkaKnjigeKuhinje
    {
       public int? RedniBroj { get; set; }
        public string? Namirnica { get; set; }
        public string? JedinicaMjere { get; set; }
        public decimal? OstatakOdJuce { get; set; }
        public decimal? NabavljenoDanas { get; set; }
        public string? Dobavljac { get; set; }
        public string? Dokument { get; set; }
        public decimal? NaStanju { get; set; }
        public decimal? UtrosenoDanas { get; set; }
        public decimal? OstatakZaSutra { get; set; }
        public decimal? NabavljenoDoDanas { get; set; }
        public decimal? ReklamiranoDoDanas { get; set; }
        public decimal? UtrosenoDoDanas { get; set; }
        public DateTime? Datum { get; set; }

        public bool IsPromet { get; set; }

        public StavkaKnjigeKuhinje()
        {
            RedniBroj = 1;
            OstatakOdJuce = 0;
            NabavljenoDanas = 0;
            NabavljenoDoDanas = 0;
            ReklamiranoDoDanas = 0;
            NaStanju = 0;
            Dobavljac = "";
            Dokument = "";
            UtrosenoDanas = 0;
            UtrosenoDoDanas = 0;
            OstatakZaSutra = 0;
           
            IsPromet = false;
        }
        public  StavkaKnjigeKuhinje(int? redniBroj, string? namirnica, string? jedinicaMjere, decimal? ostatakOdJuce, decimal? nabavljenoDanas, string? dobavljac, string? dokument, decimal? naStanju, decimal? utrosenoDanas, decimal? ostatakZaSutra, decimal? nabavljenoDoDanas, decimal? reklamiranoDoDanas, decimal? utrosenoDoDanas, DateTime? datum, bool ispromet)
        {
            RedniBroj = redniBroj;
            Namirnica = namirnica;
            JedinicaMjere = jedinicaMjere;
            OstatakOdJuce = ostatakOdJuce;
            NabavljenoDanas = nabavljenoDanas;
            Dobavljac = dobavljac;
            Dokument = dokument;
            NaStanju = naStanju;
            UtrosenoDanas = utrosenoDanas;
            OstatakZaSutra = ostatakZaSutra;
            NabavljenoDoDanas = nabavljenoDoDanas;
            ReklamiranoDoDanas = reklamiranoDoDanas;
            UtrosenoDoDanas = utrosenoDoDanas;
            Datum = datum;
            IsPromet = ispromet;

        }
    }
}
