using Caupo.Properties;
using Caupo.Views;
using QRCoder;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;



namespace Caupo.Fiscal
{
    public class racun_hr
    {
        private FiskalniRacun fiskalniRacun;

        public bool isR1 = false;
        string JIR;
        string ZIK;
        string QR;

        string BrojRacuna;
        string Datum;
        string Vrijeme;
        string Kupac;
        int VrstaPlacanja;
        string Radnik;
        bool Fiskalizovan;
        bool Sank;
        string BrojFiskalnog;

        ObservableCollection<FiskalniRacun.Item> StavkeRacuna;

        public string jir
        {

            set { this.JIR = value; }

            get { return this.JIR; }
        }

        public string zik
        {

            set { this.ZIK = value; }

            get { return this.ZIK; }
        }

        public string qr
        {

            set { this.QR = value; }

            get { return this.QR; }
        }

        public string brojracuna
        {

            set { this.BrojRacuna = value; }

            get { return this.BrojRacuna; }
        }
        public string datum
        {

            set { this.Datum = value; }
            //
            get { return this.Datum; }
        }

        public string vrijeme
        {

            set { this.Vrijeme = value; }
            //
            get { return this.Vrijeme; }
        }
        public string kupac
        {
            //
            set { this.Kupac = value; }
            //
            get { return this.Kupac; }
        }
        public int vrstaplacanja
        {
            //
            set { this.VrstaPlacanja = value; }
            //
            get { return this.VrstaPlacanja; }
        }
        public string radnik
        {
            //
            set { this.Radnik = value; }
            //
            get { return this.Radnik; }
        }
        public bool fiskalizovan
        {
            //
            set { this.Fiskalizovan = value; }
            //
            get { return this.Fiskalizovan; }
        }
        public bool sank
        {
            //
            set { this.Sank = value; }
            //
            get { return this.Sank; }
        }
        public ObservableCollection<FiskalniRacun.Item> stavkeracuna
        {
            //
            set { this.StavkeRacuna = value; }
            //
            get { return this.StavkeRacuna; }
        }
      
        public string brojfiskalnog
        {
            //
            set { this.BrojFiskalnog = value; }
            //
            get { return this.BrojFiskalnog; }
        }

        public racun_hr(FiskalniRacun fiskalniRacun)
        {
            this.fiskalniRacun = fiskalniRacun;
        }
        public racun_hr(string BrojRacuna, string Datum, string Vrijeme, string Kupac, string Radnik, int VrstaPlacanja, bool Fiskalizovan, bool Sank, ObservableCollection<FiskalniRacun.Item> StavkeRacuna, string BrojFiskalnog, string ZIK, string JIR, string QR)
        {
            this.BrojRacuna = BrojRacuna;
            this.Datum = Datum;
            this.Vrijeme = Vrijeme;
            this.Kupac = Kupac;
            this.Radnik = Radnik;
            this.VrstaPlacanja = VrstaPlacanja;
            this.Fiskalizovan = Fiskalizovan;
            this.Sank = Sank;
            this.StavkeRacuna = StavkeRacuna;
            this.BrojFiskalnog = BrojFiskalnog;
            this.ZIK = ZIK;
            this.JIR = JIR;
            this.QR = QR;

        }

        Image qr_image()
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qr, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);
            Image img = (Image)qrCodeImage;
            return img;

        }

        public string pos_printer()
        {
            string printer = Settings.Default.POSPrinter;

            return printer;
        }

        public string logo()
        {
            string logo = Settings.Default.LogoUrl;

            return logo;
        }

      

        public string poslovniProstor()
        {
            string oznaka_pos = Settings.Default.PoslovniProstor;
            return oznaka_pos;
        }

        public string naplatniUredjaj()
        {
            string oznaka_napl_uredj = Settings.Default.NaplatniUredjaj;
            return oznaka_napl_uredj;
        }

        string nacin_placanja(int index)
        {
            return index switch
            {
                0 => "GOTOVINA(Novčanice)",
                1 => "KARTICA",
                2 => "ČEK",
                3 => "Transakcijski račun",
                4 => "OSTALO",
                5 => "Reprezentacija",
                _ => "Nepoznat način plaćanja"
            };
        }



        string firma = Settings.Default.Firma;
        string adresa =  Settings.Default.Adresa;
        string mjesto = Settings.Default.Mjesto;
        string jib = Settings.Default.JIB;
       


        public void print()
        {

            if (isR1)
            {
              //  print_R1();
            }
            else
            {


                if (!string.IsNullOrEmpty(pos_printer()))
                {

                    try
                    {

                        Font printFont = new Font("Arial", 12);
                        PrintDocument pd = new PrintDocument();
                        PrintController printController = new StandardPrintController();
                        pd.PrintController = printController;
                        pd.PrintPage += new PrintPageEventHandler(pdoc_PrintPage);
                        pd.PrinterSettings.PrinterName = pos_printer();
                        pd.DocumentName = "Račun_" + brojracuna;


                        pd.Print();

                    }
                    catch (Exception ex)
                    {

                        MyMessageBox frm = new MyMessageBox ();
                        frm.MessageText.Text = "Greška prilikom printanja računa: " + ex.Message;
                        frm.MessageTitle.Text = "GREŠKA";
                        frm.ShowDialog ();
                    }

                }
            }
        }
/*
        DataTable table_stavke;
        DataTable stavke_racuna()
        {
            table_stavke = new DataTable();

            table_stavke.Columns.Add("naziv");
            table_stavke.Columns.Add("kolicina");
            table_stavke.Columns.Add("cijena");
            table_stavke.Columns.Add("jedmjere");
            table_stavke.Columns.Add("iznosPDV");
            table_stavke.Columns.Add("cijenaBezPDV");
            table_stavke.Columns.Add("ukupnoPDV");
            table_stavke.Columns.Add("iznos");
            table_stavke.Columns.Add("idStavke");
   
            foreach (var stavka in racun)
            {
                porez porez = new porez();
                porez.stopapdv = Convert.ToDouble(stavka.poreskastopa);
                
                double kolicina = Convert.ToDouble(stavka.kolicina);
                double cijena_sa_pdv = Convert.ToDouble(stavka.cijena);
                double poreska_stopa = porez.ps();
                string jedmj = "kom";
                double cijena_bez_pdv = cijena_sa_pdv - Convert.ToDouble(stavka.iznospdv);
                double iznos_pdv = Convert.ToDouble(stavka.iznospdv);
                double ukupno_pdv = iznos_pdv * kolicina;
                double iznos = cijena_sa_pdv * kolicina;
                table_stavke.Rows.Add(stavka.artikl, stavka.kolicina, stavka.cijena, jedmj,iznos_pdv.ToString("N3"),cijena_bez_pdv.ToString("N3"), ukupno_pdv.ToString("N3"), iznos.ToString("N2"), poreska_stopa.ToString());
            }
            return table_stavke;
        }

        DataTable table_stope;
        DataTable table_ps;
        DataTable stope_poreza()
        {
            table_stope = new DataTable();
            table_ps = new DataTable();
            table_stope.Columns.Add("procenat");
            table_stope.Columns.Add("naziv");
            table_stope.Columns.Add("sistem");
  

            foreach (var stavka in racun)
            {

                porez porez = new porez();
                porez.stopapdv = Convert.ToDouble(stavka.poreskastopa);
                porez.stopapnp = Convert.ToDouble(stavka.poreznapotrosnju);

                if (Convert.ToInt32(stavka.poreznapotrosnju) != 0)
                {
                    stavka.osnovicapnp = Convert.ToString(Convert.ToDouble(stavka.cijena) - Convert.ToDouble(stavka.iznospnp));
                }
                else
                {
                    stavka.osnovicapnp = "0";
                }
                double kolicina = Convert.ToDouble(stavka.kolicina);
                double cijena_sa_pdv = Convert.ToDouble(stavka.cijena);
                double poreska_stopa = porez.ps();
                double iznos_pdv = Convert.ToDouble(stavka.iznospdv);
                double cijena_bez_pdv = cijena_sa_pdv - iznos_pdv;
                double ukupno_pdv = iznos_pdv * kolicina;
                double iznos = cijena_sa_pdv * kolicina;
             
                table_stope.Rows.Add(poreska_stopa.ToString(), (cijena_bez_pdv*kolicina).ToString("N3"),ukupno_pdv.ToString("N3") );
            }

            var slaganje = table_stope.AsEnumerable()

              .Select(x =>
              new
              {
                  procenat = x["procenat"],
                  naziv = x["naziv"],
                  sistem = x["sistem"]
      
              }
              )
              .GroupBy(s => new { s.procenat })
              .Select(g =>
                  new
                  {
                      g.Key.procenat,
                
                      naziv = g.Sum(x => Math.Round(Convert.ToDecimal(x.naziv), 3)),
                      sistem = g.Sum(x => Math.Round(Convert.ToDecimal(x.sistem), 3))
                  }
              ).ToList();

            table_ps.Columns.Add("procenat");
            table_ps.Columns.Add("naziv");
            table_ps.Columns.Add("sistem");
  

            List<DataRow> noviRedovi = new List<DataRow>();
            if (slaganje != null)
            {
                foreach (var element in slaganje)
                {
                    var row = table_stope.NewRow();
                    row["procenat"] = element.procenat;
                    row["naziv"] = element.naziv;
                    row["sistem"] = element.sistem;
            

                    noviRedovi.Add(row);
                }
                table_ps = noviRedovi.CopyToDataTable();
            }

            return table_ps;
        }


        DataTable table_firma;
        DataTable podaci_o_firmi()
        {
            table_firma = new DataTable();
            //DataSet1 podaci o firmi
            OleDbConnection conn = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + frmLogin.putanja_do_baze + ";Persist Security Info=True;Jet OLEDB:Database Password=RotoRd1Bog");
            conn.Open();
            string upit = @"select * from tblFirma";
            OleDbCommand cmd = new OleDbCommand(upit);
            cmd.Connection = conn;

            OleDbDataAdapter da = new OleDbDataAdapter();

            da.SelectCommand = cmd;
            da.Fill(table_firma);
            conn.Close();
            return table_firma;
        }

        DataTable table_racun;
        DataTable podaci_racun()
        {
            table_racun = new DataTable();
   
            table_racun.Columns.Add("BrojIzlaza");
            table_racun.Columns.Add("Datum");
            table_racun.Columns.Add("Radnik");
            table_racun.Columns.Add("NacinPlacanja");
            table_racun.Columns.Add("zki");
            table_racun.Columns.Add("jir");
            table_racun.Rows.Add(brojracuna + "/" + pos() + "/" + napuredj(), datum, konobar(radnik), nacin_placanja(vrstaplacanja), zik, jir);
            return table_racun;
        }

        DataTable table_kupac;
        DataTable podaci_kupac(string kupac)
        {
            table_kupac = new DataTable();
            //DataSet3 podaci o kupcu
            OleDbConnection conn = new OleDbConnection(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + frmLogin.putanja_do_baze + ";Persist Security Info=True;Jet OLEDB:Database Password=RotoRd1Bog");
            conn.Open();
            string upit = @"select Kupac, Adresa, Mjesto, PDV, JIB from tblKupci where Kupac = @Kupac";
            OleDbCommand cmd = new OleDbCommand(upit);
            cmd.Connection = conn;

            OleDbDataAdapter da = new OleDbDataAdapter();

            da.SelectCommand = cmd;
            cmd.Parameters.Add(new OleDbParameter("@Kupac", kupac));
            da.Fill(table_kupac);
            conn.Close();
            return table_kupac;
        }

        void print_R1()
        {

            Microsoft.Reporting.WinForms.LocalReport report = new LocalReport();
            report.EnableExternalImages = true;
            Microsoft.Reporting.WinForms.ReportParameter[] parms = new ReportParameter[1];

            if (File.Exists(logo()))
            {

                parms[0] = new Microsoft.Reporting.WinForms.ReportParameter("image", @"file:///" + logo());
            }
            ReportParameter dospijece = new ReportParameter("dospijece");
            dospijece.Values.Add(Convert.ToDateTime(datum).AddDays(15).ToShortDateString());
            ReportParameter time = new ReportParameter("vrijeme");
            time.Values.Add(vrijeme);

            report.ReportEmbeddedResource = "Fiscal_Companion_5._0._1.rptR1.rdlc";

            report.DataSources.Add(new ReportDataSource("DataSetFirma", podaci_o_firmi()));
            report.DataSources.Add(new ReportDataSource("DataSetRacun", podaci_racun()));
            report.DataSources.Add(new ReportDataSource("DataSetKupac", podaci_kupac(kupac)));
            report.DataSources.Add(new ReportDataSource("DataSetStavke", stavke_racuna()));
            report.DataSources.Add(new ReportDataSource("DataSetPoreskeStope", stope_poreza()));


            if (!string.IsNullOrEmpty(logo()) & File.Exists(logo()))
            {
                report.SetParameters(parms);
            }

            report.SetParameters(dospijece);
            report.SetParameters(time);


            for (int p = 0; p < fiskalniRacun.kopijaR1; p++)
            {
                Console.WriteLine("Print to printer");
                report.PrintToPrinter();

            }
            fiskalniRacun.isR1 = false;
        }
        */
        Image img;
        decimal iznos;
        decimal ukupno;
        void pdoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            
            iznos = 0;
            ukupno = 0;
        
            
            if (File.Exists(logo()))
            {
                Console.WriteLine("Ima logo : " + logo());
                img = Image.FromFile(logo());
            }

            Graphics graphics = e.Graphics;
            Font font = new Font("Arial", 12);

            int startX = 5;
            int startY = 20;
            int Offset = 30;

            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Far;

            Font drawFont = new Font("Arial", 8);
            SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Black);

            StringFormat sf2 = new StringFormat();
            sf2.Alignment = StringAlignment.Center;

            StringFormat sf1 = new StringFormat();
            sf1.Alignment = StringAlignment.Near;

            Font drawFont2 = new Font("Arial", 10);
            Font drawFont3 = new Font("Arial", 8);
            SolidBrush drawBrush2 = new SolidBrush(Color.Black);

            Rectangle m = e.PageBounds;

            if ((double)img.Width / (double)img.Height > (double)m.Width / (double)m.Height)
            {
                m.Height = (int)(((double)img.Height / (double)img.Width * (double)m.Width)/2);
                m.Width = m.Width/2 ;
  
            }
            else
            {
                m.Width = (int)(((double)img.Width / (double)img.Height * (double)m.Height)/2);
                m.Height = m.Height/2 ;
            }

            m.X = e.MarginBounds.Width + ((m.Width / 2) - e.MarginBounds.Width);
            Console.WriteLine("X: " + m.X + " - Y: " + m.Y + " - Width: " + m.Width + " - Height: " + m.Height);

            if (File.Exists(logo()))
            {
      
                graphics.DrawImage(img,m);

                Offset = Offset + (m.Height)-20;
            }


            RectangleF rect9 = new RectangleF(startX, startY + Offset, e.PageBounds.Width, 20);
            graphics.DrawString(firma, drawFont2, drawBrush2, rect9, sf2);
            Offset = Offset + 20;

            RectangleF rect6 = new RectangleF(startX, startY + Offset, e.PageBounds.Width, 20);
            graphics.DrawString(adresa, drawFont2, drawBrush2, rect6, sf2);
            Offset = Offset + 20;

            RectangleF rect7 = new RectangleF(startX, startY + Offset, e.PageBounds.Width, 20);
            graphics.DrawString(mjesto, drawFont2, drawBrush2, rect7, sf2);
            Offset = Offset + 20;

            RectangleF rect8 = new RectangleF(startX, startY + Offset, e.PageBounds.Width, 20);
            graphics.DrawString("OIB: " + jib, drawFont2, drawBrush2, rect8, sf2);
            Offset = Offset + 25;

            RectangleF rect10 = new RectangleF(startX, startY + Offset, e.PageBounds.Width, 20);
            if (FiskalniRacun.reklamirani_racun)
            {
                graphics.DrawString("Broj storno računa:" + brojracuna + "/" + poslovniProstor() + "/" + naplatniUredjaj(), drawFont2, drawBrush2, rect10, sf2);
            }
            else
            {
                graphics.DrawString("Broj računa:" + brojracuna + "/" + poslovniProstor () + "/" + naplatniUredjaj (), drawFont2, drawBrush2, rect10, sf2);
            }
           
            Offset = Offset + 25;

            graphics.DrawString("Datum: " + datum,
                     new Font("Arial", 8),
                     new SolidBrush(Color.Black), startX, startY + Offset);
            RectangleF rect11 = new RectangleF(startX + e.PageBounds.Width - 110, startY + Offset, 100, 20);
            graphics.DrawString("Vrijeme: " + vrijeme, drawFont, drawBrush, rect11, sf);
            Offset = Offset + 20;

            graphics.DrawString("Konobar:" + Globals.ulogovaniKorisnik.Radnik, new Font("Arial", 8),
                 new SolidBrush(Color.Black), startX, startY + Offset);
          
            Offset = Offset + 20;
            graphics.DrawLine (Pens.Black,   startX, startY + Offset,  startX + e.PageBounds.Width,  startY + Offset );
       
            Offset = Offset + 10;

            graphics.DrawString("Naziv   " , new Font("Arial", 8),
                new SolidBrush(Color.Black), startX, startY + Offset);
            graphics.DrawString(" Kol.", new Font("Arial", 8),
                new SolidBrush(Color.Black), startX + 5, startY + Offset+10);
            graphics.DrawString("Cijena", new Font("Arial", 8),
                 new SolidBrush(Color.Black), startX + e.PageBounds.Width / 4, startY + Offset+10);
            graphics.DrawString("Por", new Font("Arial", 8),
                 new SolidBrush(Color.Black), startX + e.PageBounds.Width / 2 , startY + Offset+10);
            graphics.DrawString("Iznos", new Font("Arial", 8),
                 new SolidBrush(Color.Black), startX + e.PageBounds.Width - e.PageBounds.Width / 4, startY + Offset+10);
            Offset = Offset + 25;


            graphics.DrawLine (Pens.Black, startX, startY + Offset, startX + e.PageBounds.Width, startY + Offset);
            Offset = Offset + 10;

            foreach (var stavkaracuna in stavkeracuna)
            {
                iznos = 0;
                iznos = Convert.ToDecimal(stavkaracuna.Quantity) * Convert.ToDecimal(stavkaracuna.UnitPrice);
                graphics.DrawString(stavkaracuna.Name, new Font("Arial", 10),
                         new SolidBrush(Color.Black), startX, startY + Offset);

                RectangleF rect = new RectangleF(startX, startY + Offset + 20, e.PageBounds.Width / 4, 20);
                graphics.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", Convert.ToDecimal(stavkaracuna.Quantity)) + " X", drawFont, drawBrush, rect, sf);

                RectangleF rect2 = new RectangleF(startX + e.PageBounds.Width / 4, startY + Offset + 20, e.PageBounds.Width / 4, 20);
                graphics.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", Convert.ToDecimal(stavkaracuna.UnitPrice)) + " €", drawFont, drawBrush, rect2, sf);

                RectangleF rect22 = new RectangleF(startX + e.PageBounds.Width / 2, startY + Offset + 20, e.PageBounds.Width / 4, 20);
                graphics.DrawString (stavkaracuna.Labels[0], drawFont, drawBrush, rect22, sf2);

                RectangleF rect3 = new RectangleF(startX + e.PageBounds.Width - e.PageBounds.Width / 4, startY + Offset + 20, e.PageBounds.Width / 4, 20);
                graphics.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", iznos) + " €", drawFont, drawBrush, rect3, sf);

                ukupno += iznos;
                Offset = Offset + 40;
            }

            graphics.DrawLine (Pens.Black, startX, startY + Offset, startX + e.PageBounds.Width, startY + Offset);
            Offset = Offset + 10;

            graphics.DrawString(nacin_placanja(vrstaplacanja), new Font("Arial", 10),
                     new SolidBrush(Color.Black), startX, startY + Offset);
            RectangleF rect4 = new RectangleF(startX +e.PageBounds.Width-80, startY + Offset , 70, 20);
            graphics.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", ukupno) + "  €", drawFont, drawBrush, rect4, sf);
            Offset = Offset + 20;

            graphics.DrawLine (Pens.Black, startX, startY + Offset, startX + e.PageBounds.Width, startY + Offset);
            Offset = Offset + 10;

         

            graphics.DrawString("Opis", new Font("Arial", 8),
              new SolidBrush(Color.Black), startX + 5, startY + Offset + 10);
            graphics.DrawString("Stopa", new Font("Arial", 8),
                 new SolidBrush(Color.Black), startX + e.PageBounds.Width / 4, startY + Offset + 10);
            graphics.DrawString("Osnovica", new Font("Arial", 8),
                 new SolidBrush(Color.Black), startX + e.PageBounds.Width / 2, startY + Offset + 10);
            graphics.DrawString("Iznos", new Font("Arial", 8),
                 new SolidBrush(Color.Black), startX + e.PageBounds.Width - e.PageBounds.Width / 5, startY + Offset + 10);
            Offset = Offset + 25;


            graphics.DrawLine (Pens.Black, startX, startY + Offset, startX + e.PageBounds.Width, startY + Offset);
            Offset = Offset + 10;
            if (fiskalniRacun.pdv13iznos != 0)
            {
                RectangleF rect = new RectangleF(startX, startY + Offset , e.PageBounds.Width / 4, 20);
                graphics.DrawString("PDV", drawFont, drawBrush, rect, sf1);

                RectangleF rect2 = new RectangleF(startX + e.PageBounds.Width / 5, startY + Offset , e.PageBounds.Width / 5, 20);
                graphics.DrawString("13%", drawFont, drawBrush, rect2, sf);

                RectangleF rect22 = new RectangleF(startX + e.PageBounds.Width / 2, startY + Offset , e.PageBounds.Width / 4, 20);
                graphics.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", fiskalniRacun.pdv13osnovica) + " €", drawFont, drawBrush, rect22, sf2);

                RectangleF rect3 = new RectangleF(startX + e.PageBounds.Width - e.PageBounds.Width / 4, startY + Offset , e.PageBounds.Width / 4, 20);
                graphics.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", fiskalniRacun.pdv13iznos) + " €", drawFont, drawBrush, rect3, sf);

                Offset = Offset + 20;
            }
            if (fiskalniRacun.pdv25iznos != 0)
            {
                RectangleF rect = new RectangleF(startX, startY + Offset , e.PageBounds.Width / 4, 20);
                graphics.DrawString("PDV", drawFont, drawBrush, rect, sf1);

                RectangleF rect2 = new RectangleF(startX + e.PageBounds.Width / 5, startY + Offset , e.PageBounds.Width / 5, 20);
                graphics.DrawString("25%", drawFont, drawBrush, rect2, sf);

                RectangleF rect22 = new RectangleF(startX + e.PageBounds.Width / 2, startY + Offset, e.PageBounds.Width / 4, 20);
                graphics.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", fiskalniRacun.pdv25osnovica) + " €", drawFont, drawBrush, rect22, sf2);

                RectangleF rect3 = new RectangleF(startX + e.PageBounds.Width - e.PageBounds.Width / 4, startY + Offset , e.PageBounds.Width / 4, 20);
                graphics.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", fiskalniRacun.pdv25iznos) + " €", drawFont, drawBrush, rect3, sf);

                Offset = Offset + 20;
            }

            if (fiskalniRacun.pnpiznos != 0)
            {
                RectangleF rect = new RectangleF(startX, startY + Offset , e.PageBounds.Width / 4, 20);
                graphics.DrawString("PNP", drawFont, drawBrush, rect, sf1);

                RectangleF rect2 = new RectangleF(startX + e.PageBounds.Width / 5, startY + Offset , e.PageBounds.Width / 5, 20);
                graphics.DrawString(ConfigurationManager.AppSettings["PNP"]+"%", drawFont, drawBrush, rect2, sf);

                RectangleF rect22 = new RectangleF(startX + e.PageBounds.Width / 2, startY + Offset , e.PageBounds.Width / 4, 20);
                graphics.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", fiskalniRacun.pnposnovica) + " €", drawFont, drawBrush, rect22, sf2);

                RectangleF rect3 = new RectangleF(startX + e.PageBounds.Width - e.PageBounds.Width / 4, startY + Offset , e.PageBounds.Width / 4, 20);
                graphics.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0.00}", fiskalniRacun.pnpiznos) + " €", drawFont, drawBrush, rect3, sf);

                Offset = Offset + 20;
            }

            graphics.DrawLine (Pens.Black, startX, startY + Offset, startX + e.PageBounds.Width, startY + Offset);
            Offset = Offset + 30;

            graphics.DrawImage(qr_image(), 10, Offset, (e.PageBounds.Width / 2), (e.PageBounds.Width/2));
            Offset = Offset + e.PageBounds.Width / 2 + 20;

            graphics.DrawLine (Pens.Black, startX, startY + Offset, startX + e.PageBounds.Width, startY + Offset);
            Offset = Offset + 5;

            RectangleF rect55 = new RectangleF(startX, startY + Offset, e.PageBounds.Width, 30);
            graphics.DrawString("JIR:" + jir, drawFont3, drawBrush2, rect55, sf2);
            Offset = Offset + 30;

            graphics.DrawLine (Pens.Black, startX, startY + Offset, startX + e.PageBounds.Width, startY + Offset);
            Offset = Offset + 5;

            RectangleF rect56 = new RectangleF(startX, startY + Offset, e.PageBounds.Width, 30);
            graphics.DrawString("ZIK:" + zik, drawFont3, drawBrush2, rect56, sf2);
            Offset = Offset + 30;

            graphics.DrawLine (Pens.Black, startX, startY + Offset, startX + e.PageBounds.Width, startY + Offset);
            Offset = Offset + 15;

            RectangleF rect57 = new RectangleF(startX, startY + Offset, e.PageBounds.Width, 20);
            graphics.DrawString(".", drawFont3, drawBrush2, rect56, sf2);
           

        }

     

    }
}






