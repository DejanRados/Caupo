using System;

namespace Caupo.Models
{
    public class StavkaKnjigeSanka
    {
        public int? RedniBroj { get; set; }
        public string Namirnica { get; set; }
        public string Naziv { get; set; }
        public string JedinicaMjere { get; set; }
        public decimal Cijena { get; set; }
        public decimal OstatakOdJuce { get; set; }
        public decimal NabavljenoDanas { get; set; }
        public decimal NabavljenoDoDanas { get; set; }
        public decimal ReklamiranoDoDanas { get; set; }
        public decimal NaStanju { get; set; }
        public decimal UtrosenoDanas { get; set; }
        public decimal UtrosenoDoDanas { get; set; }
        public decimal OstatakZaSutra { get; set; }
        public decimal Promet { get; set; }
        public decimal? Normativ { get; set; }
        public DateTime? Datum { get; set; }
        public string Dobavljac { get; set; }
        public string Dokument { get; set; }

        public bool IsPromet { get; set; }

        public StavkaKnjigeSanka()
        {
            RedniBroj = 1;
            Cijena = 0;
            OstatakOdJuce = 0;
            NabavljenoDanas = 0;
            NabavljenoDoDanas = 0;
            ReklamiranoDoDanas = 0;
            NaStanju = 0;
            UtrosenoDanas = 0;
            UtrosenoDoDanas = 0;
            OstatakZaSutra = 0;
            Promet = 0;
            Normativ = 1;
            IsPromet = false;
        }

        public StavkaKnjigeSanka(int? rednibroj, DateTime? datum, string namirnica, string naziv, string jedinicaMjere,
                            decimal cijena, decimal ostatakOdJuce, decimal nabavljenoDanas,
                            decimal nabavljenoDoDanas, decimal reklamiranoDoDanas, decimal naStanju,
                            decimal utrosenoDanas, decimal utrosenoDoDanas, decimal ostatakZaSutra,
                            decimal promet, string dobavljac, string dokument, bool isPromet, decimal normativ)
        {
            RedniBroj = rednibroj;
            Datum = datum;
            Namirnica = namirnica;
            Naziv = naziv;
            JedinicaMjere = jedinicaMjere;
            Cijena = cijena;
            OstatakOdJuce = ostatakOdJuce;
            NabavljenoDanas = nabavljenoDanas;
            NabavljenoDoDanas = nabavljenoDoDanas;
            ReklamiranoDoDanas = reklamiranoDoDanas;
            NaStanju = naStanju;
            UtrosenoDanas = utrosenoDanas;
            UtrosenoDoDanas = utrosenoDoDanas;
            OstatakZaSutra = ostatakZaSutra;
            Promet = promet;
            Dobavljac = dobavljac;
            Dokument = dokument;
            IsPromet = isPromet;
            Normativ = normativ;
        }
    }
}
 
