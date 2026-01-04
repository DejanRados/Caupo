using Caupo.Data;
using Caupo.Models;
using Caupo.Properties;
using Caupo.ViewModels;
using Caupo.Views;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2021.PowerPoint.Comment;
using MahApps.Metro.Controls;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows;
using static Caupo.Data.DatabaseTables;


namespace Caupo.Fiscal
{


    public class FiskalniRacun
    {


        [JsonProperty ("print", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Print { get; set; }

        [JsonProperty ("renderReceiptImage", NullValueHandling = NullValueHandling.Ignore)]
        public bool? RenderReceiptImage { get; set; }

        [JsonProperty ("receiptImageFormat", NullValueHandling = NullValueHandling.Ignore)]
        public string? ReceiptImageFormat { get; set; }

        [JsonProperty ("receiptSlipWidth", NullValueHandling = NullValueHandling.Ignore)]
        public int? ReceiptSlipWidth { get; set; }

        [JsonProperty ("receiptSlipFontSizeNormal", NullValueHandling = NullValueHandling.Ignore)]
        public int? ReceiptSlipFontSizeNormal { get; set; }

        [JsonProperty ("receiptSlipFontSizeLarge", NullValueHandling = NullValueHandling.Ignore)]
        public int? ReceiptSlipFontSizeLarge { get; set; }

        [JsonProperty ("invoiceRequest", NullValueHandling = NullValueHandling.Ignore)]
        public InvoiceRequest? IR { get; set; }

        public class InvoiceRequest
        {

            [JsonProperty ("invoiceType", NullValueHandling = NullValueHandling.Ignore)]
            public string? InvoiceType { get; set; }

            [JsonProperty ("transactionType", NullValueHandling = NullValueHandling.Ignore)]
            public string? TransactionType { get; set; }

            [JsonProperty ("buyerId", NullValueHandling = NullValueHandling.Ignore)]
            public string? BuyerId { get; set; }

            [JsonProperty ("buyerCostCenterId", NullValueHandling = NullValueHandling.Ignore)]
            public string? BuyerCostCenterId { get; set; }

            [JsonProperty ("referentDocumentNumber", NullValueHandling = NullValueHandling.Ignore)]
            public string? ReferentDocumentNumber { get; set; }

            [JsonProperty ("referentDocumentDT", NullValueHandling = NullValueHandling.Ignore)]
            public string? ReferentDocumentDT { get; set; }


            [JsonProperty ("payment", NullValueHandling = NullValueHandling.Ignore)]
            public List<Payment>? Payments { get; set; }

            [JsonProperty ("items", NullValueHandling = NullValueHandling.Ignore)]
            public List<Item>? Items { get; set; }

            [JsonProperty ("cashier", NullValueHandling = NullValueHandling.Ignore)]
            public string? Cashier { get; set; }

        }

        public class Item : INotifyPropertyChanged
        {

            [JsonProperty ("name", NullValueHandling = NullValueHandling.Ignore)]

            public string? Name { get; set; }

            [JsonProperty ("labels", NullValueHandling = NullValueHandling.Ignore)]

            public List<string> Labels { get; set; } = new List<string> ();

            [JsonProperty ("totalAmount", NullValueHandling = NullValueHandling.Ignore)]

            public decimal? TotalAmount => Quantity * UnitPrice;

            [JsonProperty ("unitPrice", NullValueHandling = NullValueHandling.Ignore)]

            public decimal? UnitPrice { get; set; }

            [JsonProperty ("quantity", NullValueHandling = NullValueHandling.Ignore)]

            private decimal? _quantity;
            public decimal? Quantity
            {
                get => _quantity;
                set
                {
                    if (_quantity != value)
                    {
                        _quantity = value;
                        OnPropertyChanged (nameof (Quantity));
                        OnPropertyChanged (nameof (TotalAmount)); // Ako se Quantity promeni, ukupan iznos se mora ažurirati
                    }
                }
            }
            [JsonIgnore]
            //   [JsonProperty("RedniBroj", NullValueHandling = NullValueHandling.Ignore)]
            public int? RedniBroj { get; set; }
            [JsonIgnore]
            //  [JsonProperty("BrojRacuna", NullValueHandling = NullValueHandling.Ignore)]
            public int? BrojRacuna { get; set; }
            [JsonIgnore]
            //  [JsonProperty("Sifra", NullValueHandling = NullValueHandling.Ignore)]
            public string? Sifra { get; set; }
            [JsonIgnore]
            //  [JsonProperty("Proizvod", NullValueHandling = NullValueHandling.Ignore)]
            public int? Proizvod { get; set; }
            [JsonIgnore]
            //[JsonProperty("JedinicaMjere", NullValueHandling = NullValueHandling.Ignore)]
            public int? JedinicaMjere { get; set; }
            [JsonIgnore]
            // [JsonProperty("Naziv", NullValueHandling = NullValueHandling.Ignore)]
            public string? Naziv { get; set; }
            [JsonIgnore]
            //  [JsonProperty("Printed", NullValueHandling = NullValueHandling.Ignore)]
            public string? Printed { get; set; }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected void OnPropertyChanged(string? propertyName) =>
                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));




        }

        public class Payment
        {


            [JsonProperty ("amount", NullValueHandling = NullValueHandling.Ignore)]

            public decimal? Amount { get; set; }

            [JsonProperty ("paymentType", NullValueHandling = NullValueHandling.Ignore)]

            public string? PaymentType { get; set; }


        }
        [JsonIgnore]
        public bool kopijaracuna = false;

        string? vrijeme;
        string? brojfiskalnog;
        string? brojfiskalnogshort;

        string extprinter = Settings.Default.ExterniPrinter;
        string printerwidth = Settings.Default.SirinaTrake;
        string IPLPFR = Settings.Default.LPFR_IP;
        string APILPFR = Settings.Default.LPFR_Key;
        bool success;



        private async Task<bool> UpdateRacunStornoAsync(string brojfiskalnog)
        {
            Debug.WriteLine ("--------------------------------------------------");
            Debug.WriteLine ("UpdateRacunStornoAsync POZVANA");
            Debug.WriteLine ($"Ulazni broj fiskalnog računa: '{brojfiskalnog}'");

            if (string.IsNullOrWhiteSpace (brojfiskalnog))
            {
                Debug.WriteLine ("❌ brojfiskalnog je NULL ili PRAZAN");
                return false;
            }

            try
            {
                using (var db = new AppDbContext ())
                {
                    Debug.WriteLine ("✔ AppDbContext kreiran");

                    // Provjera da li račun postoji prije update-a
                    var postoji = await db.Racuni
                        .AnyAsync (r => r.BrojFiskalnogRacuna == brojfiskalnog);

                    Debug.WriteLine ($"Postoji račun sa tim brojem: {postoji}");

                    if (!postoji)
                    {
                        Debug.WriteLine ("❌ NIJEDAN RAČUN NIJE PRONAĐEN");
                        return false;
                    }

                    int affectedRows = await db.Racuni
                        .Where (r => r.BrojFiskalnogRacuna == brojfiskalnog)
                        .ExecuteUpdateAsync (r => r
                            .SetProperty (x => x.Reklamiran, "DA"));

                    Debug.WriteLine ($"ExecuteUpdateAsync završio. AffectedRows = {affectedRows}");

                    bool uspjeh = affectedRows > 0;
                    Debug.WriteLine ($"Rezultat metode (return): {uspjeh}");

                    return uspjeh;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine ("🔥 EXCEPTION u UpdateRacunStornoAsync");
                Debug.WriteLine (ex.ToString ());
                return false;
            }
            finally
            {
                Debug.WriteLine ("UpdateRacunStornoAsync ZAVRŠENA");
                Debug.WriteLine ("--------------------------------------------------");
            }
        }

        int MmToHundredthsInch(float mm)
        {
            return (int)(mm / 25.4f * 100f);
        }
        private string _journalText;
        private Bitmap _qrBitmap;
        public void PrintJournalFromJson(string jsonResponse)
        {
            try
            {
                var obj = JObject.Parse (jsonResponse);

                var journal = obj["journal"]?.ToString ();
                if (string.IsNullOrWhiteSpace (journal))
                    throw new Exception ("Journal nije pronađen u JSON-u.");

                var verificationUrl = obj["verificationUrl"]?.ToString ();
                if (string.IsNullOrWhiteSpace (verificationUrl))
                    throw new Exception ("verificationUrl nije pronađen u JSON-u.");

                // Generiranje QR koda u Bitmap (bez spremanja u datoteku)
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator ())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode (verificationUrl, QRCodeGenerator.ECCLevel.Q);
                    using (QRCode qrCode = new QRCode (qrCodeData))
                    {
                        _qrBitmap = qrCode.GetGraphic (20);
                    }
                }

                _journalText = journal;

                PrintDocument pd = new PrintDocument ();
                float sirinaMm = float.Parse (Settings.Default.SirinaTrake);

             //   int paperWidth = MmToHundredthsInch (sirinaMm);

                // Visina može biti velika (roll printer)
                //int paperHeight = 2000;

              //  pd.DefaultPageSettings.PaperSize =
               //     new PaperSize ("POS", paperWidth, paperHeight);

                // Obavezno bez margina
                pd.DefaultPageSettings.Margins = new Margins (0, 0, 0, 0);
                pd.PrintPage += Pd_PrintPage;


                string printerName = Settings.Default.POSPrinter;

                if (string.IsNullOrWhiteSpace (printerName))
                {
                    ShowError ("GREŠKA", $"POS printer nije podešen.\\nMolimo izaberite printer u postavkama.");
                    return;
                }

                pd.PrinterSettings.PrinterName = printerName;


                pd.Print ();
            }
            catch (Exception ex)
            {
                Console.WriteLine ("Greška prilikom ispisa: " + ex.Message);
            }
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            float sirinaMm = float.Parse (Settings.Default.SirinaTrake);
            int fontSize = 9;
            if(sirinaMm < 80)
            {
                fontSize = 7;
            }
            using (Font font = new Font ("Consolas", 9))
            {

                int maxWidth = e.MarginBounds.Width;
                int pageWidth = e.PageBounds.Width;
                // float y = 10;

                SizeF textSize = e.Graphics.MeasureString (_journalText, font);

                int qrWidth = 0, qrHeight = 0;
                if (_qrBitmap != null)
                {
                    qrWidth = _qrBitmap.Width;
                    qrHeight = _qrBitmap.Height;

                    // Skaliranje QR koda ako je širi od trake
                    if (qrWidth > maxWidth)
                    {
                        float scale = (float)maxWidth / qrWidth;
                        qrWidth = maxWidth;
                        qrHeight = (int)(qrHeight * scale);
                    }
                }

                // Ukupna visina sadržaja (tekst + mali razmak + QR)
                float totalHeight = textSize.Height + (qrHeight > 0 ? qrHeight + 10 : 0);

                // Početna pozicija Y da se sadržaj vertikalno pozicionira odozgo
                float startY = e.MarginBounds.Top;

                // Centriranje teksta horizontalno

                float textX = (maxWidth - textSize.Width) / 2-2; //e.MarginBounds.Left+ ((maxWidth - textSize.Width) / 2);
                float textY = startY;
                e.Graphics.DrawString (_journalText, font, Brushes.Black, new PointF (textX, textY));

                // Crtanje QR koda ispod teksta
                if (_qrBitmap != null)
                {
                   
                    int qrX =  (maxWidth - qrWidth) / 2;
                    int qrY = (int)(textY + textSize.Height + 10); // 10px razmak
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    e.Graphics.DrawImage (_qrBitmap, new Rectangle (qrX, qrY, qrWidth, qrHeight));
                }
            }
        }


        public async Task<bool> IzdajFiskalniRacun(string invoiceType, string transactionType, string ReferentDocumentNumber, string referentDocumentDT,
            ObservableCollection<Item> StavkeRacuna,
            TblKupci? selectedKupac,
            int SelectedNacinPlacanjaIndex,
            decimal? TotalSum)
        {
            if (StavkeRacuna.Count == 0)
                return false;

            int brojracuna;
            using (var db = new AppDbContext ())
            {
                brojracuna = await db.Racuni
                    .MaxAsync (r => (int?)r.BrojRacuna) ?? 0;  // Ako je tabela prazna, vrati 0
            }

            Debug.WriteLine ("----------------------- BROJ RACUNA: " + brojracuna + " ------------------------------------------------------------ ");

            var Racun = new TblRacuni
            {
                Datum = DateTime.Now,
                NacinPlacanja = SelectedNacinPlacanjaIndex,
                Radnik = Globals.ulogovaniKorisnik.IdRadnika.ToString (),
                Iznos = TotalSum,
                Kupac = "Gradjani" // default vrijednost
            };

            if (SelectedNacinPlacanjaIndex != -1 && selectedKupac != null)
            {
                Racun.Kupac = selectedKupac.Kupac;
                Racun.KupacId = selectedKupac.JIB;
            }

            // Mapiranje vrste plaćanja
            string paymenttype = SelectedNacinPlacanjaIndex switch
            {
                0 => "Cash",
                1 => "Card",
                2 => "Check",
                3 => "WireTransfer",
                _ => "Unknown"
            };

            var _FiskalniRacun = new FiskalniRacun
            {
                Print = extprinter == "DA" ? false : true,
                RenderReceiptImage = extprinter == "DA" ? false : true,
                ReceiptImageFormat = "Png",
                ReceiptSlipWidth = printerwidth == "57" ? 386 : 576,
                ReceiptSlipFontSizeNormal = printerwidth == "57" ? 23 : 25,
                ReceiptSlipFontSizeLarge = printerwidth == "57" ? 27 : 30
            };

            var InvoiceRequest = new InvoiceRequest
            {
                InvoiceType = invoiceType,
                Cashier = Globals.ulogovaniKorisnik.Radnik,
                TransactionType = transactionType,
                Payments = new List<Payment>
                    {
                        new() { PaymentType = paymenttype, Amount = Racun.Iznos }
                    },
                Items = StavkeRacuna.ToList ()
            };

            if (Racun.Kupac != "Gradjani")
            {
                InvoiceRequest.BuyerId = Racun.KupacId;
                if (InvoiceRequest.BuyerId == "0000000000000")
                {
                    ShowError ("GREŠKA", $"Ne postoje podaci u bazi o kupcu {Racun.Kupac}.\nDodajte kupca u bazu prije izdavanja računa.");
                    return false;
                }
            }

            if (string.Equals (transactionType, "Refund", StringComparison.OrdinalIgnoreCase) || string.Equals (invoiceType, "Copy", StringComparison.OrdinalIgnoreCase))
            {
                InvoiceRequest.ReferentDocumentNumber = ReferentDocumentNumber;
                InvoiceRequest.ReferentDocumentDT = referentDocumentDT;

                Debug.WriteLine (" Refund ili Copy: InvoiceRequest.ReferentDocumentDT = " + InvoiceRequest.ReferentDocumentDT + "InvoiceRequest.ReferentDocumentNumber = " + ReferentDocumentNumber);
            }


            // Provjeri treba li blok za kuhinju
            Globals.treba_blok_za_kuhinju = StavkeRacuna.Any (item => item.Proizvod == 1 && item.Printed != "DA");

            _FiskalniRacun.IR = InvoiceRequest;
            string output = JsonConvert.SerializeObject (_FiskalniRacun);

            var client = new HttpClient ();

            try
            {
                // Provjera dostupnosti kase
                var requestAttention = new HttpRequestMessage (HttpMethod.Get, $"http://{IPLPFR}:3566/api/attention");
                requestAttention.Headers.Add ("Authorization", "Bearer " + APILPFR);

                var responseAttention = await client.SendAsync (requestAttention);
                string contentAttention = await responseAttention.Content.ReadAsStringAsync ();

                if (!responseAttention.IsSuccessStatusCode)
                {
                    ShowError ("GREŠKA", $"Greška provera dostupnosti: {responseAttention.StatusCode}\nIPLPFR: {IPLPFR}, APILPFR: {APILPFR}\nContent: {contentAttention}");
                    return false;
                }

                Debug.WriteLine ($"Attention OK: {responseAttention.StatusCode} - {contentAttention}");

                //   // Provjera statusakase
                var requestStatus = new HttpRequestMessage (HttpMethod.Get, $"http://{IPLPFR}:3566/api/status");
                requestStatus.Headers.Add ("Authorization", "Bearer " + APILPFR);

                var responseStatus = await client.SendAsync (requestStatus);
                string contentStatus = await responseStatus.Content.ReadAsStringAsync ();

                if (!responseStatus.IsSuccessStatusCode)
                {
                    ShowError ("GREŠKA", $"Greška na serveru: {responseStatus.StatusCode}\nContent: {contentStatus}");
                    return false;
                }

                var lpfrStatus = JsonConvert.DeserializeObject<LpfrStatus> (contentStatus);
                if (lpfrStatus.Gsc.Contains ("1300"))
                {
                    ShowError ("GREŠKA", "Bezbednosni element nije prisutan.\nProvjerite da li kartica ubačena i pokušajte ponovo.");
                    return false;
                }

                Debug.WriteLine ($"Status OK: {responseStatus.StatusCode} - {contentStatus}");

                // Unos pina
                var requestPIN = new HttpRequestMessage (HttpMethod.Post, $"http://{IPLPFR}:3566/api/pin");
                requestPIN.Headers.Add ("Authorization", "Bearer " + APILPFR);
                requestPIN.Content = new StringContent (Settings.Default.LPFR_Pin, Encoding.UTF8, "text/plain");

                var responsePIN = await client.SendAsync (requestPIN);
                string contentPIN = await responsePIN.Content.ReadAsStringAsync ();

                Debug.WriteLine ($"PIN OK: {responsePIN.StatusCode} - {contentPIN}");

                if (!responsePIN.IsSuccessStatusCode)
                {
                    ShowError ("GREŠKA", $"Greška PIN: {responsePIN.StatusCode}\n{contentPIN}");
                    return false;
                }


            }
            // Greška u HTTP Request 
            catch (HttpRequestException ex)
            {
                string details = $"Message: {ex.Message}\n";
                if (ex.InnerException != null)
                    details += $"InnerException: {ex.InnerException.Message}\n";
                details += $"StackTrace: {ex.StackTrace}";

                ShowError ("GREŠKA", "HTTP Request exception:\n" + details +
                          $"\nIPLPFR: {IPLPFR}, APILPFR: {APILPFR}");
                return false;
            }
            //Greška u predugom trajanju
            catch (TaskCanceledException ex)
            {
                string details = $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}";
                ShowError ("GREŠKA", "Zahtjev je istekao ili otkazan.\n" + details +
                          $"\nProvjerite da li je kasa upaljena i spojena.\nIPLPFR: {IPLPFR}, APILPFR: {APILPFR}");
                return false;
            }
            //Generalna greška
            catch (Exception ex)
            {
                string details = $"Message: {ex.Message}\n";
                if (ex.InnerException != null)
                    details += $"InnerException: {ex.InnerException.Message}\n";
                details += $"StackTrace: {ex.StackTrace}";

                ShowError ("GREŠKA", "Neočekivana greška pri provjeri statusa kase.\n" + details);
                return false;
            }


            try
            {
                // ================= HTTP POZIV =================
                var request = new HttpRequestMessage (
                    HttpMethod.Post,
                    $"http://{IPLPFR}:3566/api/invoices");

                request.Headers.Add ("Authorization", "Bearer " + APILPFR);
                request.Headers.Add ("RequestId", brojracuna.ToString ());
                request.Content = new StringContent (output, Encoding.UTF8, "application/json");
                Debug.WriteLine ("--------------------------------- " + Environment.NewLine);
                Debug.WriteLine ("--------------------------------- " + Environment.NewLine);
                Debug.WriteLine ("--------------------------------- " + Environment.NewLine);
                Debug.WriteLine ("OUTPUT: " + output);
                Debug.WriteLine ("--------------------------------- " + Environment.NewLine);
                Debug.WriteLine ("--------------------------------- " + Environment.NewLine);
                Debug.WriteLine ("--------------------------------- " + Environment.NewLine);
                Debug.WriteLine ("--------------------------------- " + Environment.NewLine);
                Debug.WriteLine ("REQUEST: " + request);
                Debug.WriteLine ("--------------------------------- " + Environment.NewLine);
                Debug.WriteLine ("--------------------------------- " + Environment.NewLine);
                Debug.WriteLine ("--------------------------------- " + Environment.NewLine);

                var response = await client.SendAsync (request);
                string res = await response.Content.ReadAsStringAsync ();

                Debug.WriteLine ("Status code: " + response.StatusCode);
                Debug.WriteLine ("Content: " + res);
                //Ako nije uspješan zahtjev 
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException ($"Server error: {response.StatusCode}\n{res}");

                var invoiceResponse = JsonConvert.DeserializeObject<InvoiceResponse> (res);

                vrijeme = invoiceResponse.SdcDateTime.ToString ("yyyy-MM-dd'T'HH:mm:ss.fffzzz");
                brojfiskalnog = invoiceResponse.InvoiceNumber.ToString ();
                brojfiskalnogshort = invoiceResponse.TotalCounter.ToString ();
                Debug.WriteLine ("Broj fiskalnog : " + brojfiskalnog + "Broj fiskalnog kratki: " + brojfiskalnogshort);

                // ================= PRINT =================
                PrintJournalFromJson (res);
                //PrintJournalEscPos (res);

                // ================= DATABASE =================
                try
                {
                    if (invoiceType != "Copy")
                    {
                        if (transactionType == "Refund")
                        {
                            await UpdateRacunStornoAsync (ReferentDocumentNumber);
                        }
                        else
                        {


                            using var db = new AppDbContext ();
                            using var transaction = await db.Database.BeginTransactionAsync ();
                            //Ubacujem račun u TblRacuni
                            Racun.BrojFiskalnogRacuna = brojfiskalnog;
                            await db.Racuni.AddAsync (Racun);
                            await db.SaveChangesAsync ();

                            //Ubacujem stavke racuna u TblRacunStavke
                            foreach (var s in StavkeRacuna)
                            {
                                var stavka = new TblRacunStavka
                                {
                                    Artikl = s.Name,
                                    Sifra = s.Sifra,
                                    Kolicina = s.Quantity ?? 0m,
                                    Cijena = s.UnitPrice,
                                    VrstaArtikla = s.Proizvod,
                                    JedinicaMjere = s.JedinicaMjere,
                                    ArtiklNormativ = s.Naziv,
                                    PoreskaStopa = PoreskaStopa (s.Labels[0]),
                                    BrojRacuna = Racun.BrojRacuna
                                };

                                await db.RacunStavka.AddAsync (stavka);
                            }
                            await db.SaveChangesAsync ();
                            await transaction.CommitAsync ();

                            //Provjera za printanje blokova
                            var SankStavke = StavkeRacuna.Where (item => item.Proizvod == 0 && item.Printed != "DA").ToList ();
                            var KuhinjaStavke = StavkeRacuna.Where (item => item.Proizvod == 1 && item.Printed != "DA").ToList ();

                            if (KuhinjaStavke.Any ())
                            {
                                var printer = new BlokPrinter (KuhinjaStavke, "Kuhinja", "Kasa", "Kasa");
                                await printer.Print ();
                            }

                            if (SankStavke.Any ())
                            {
                                var printer = new BlokPrinter (SankStavke, "Sank", "Kasa", "Kasa");
                                await printer.Print ();
                            }
                        }
                    }
                }
                catch (Exception dbEx)
                {
                    Debug.WriteLine ("DB GREŠKA: " + dbEx);
                    ShowError ("GREŠKA BAZE", dbEx.Message);
                    return false;
                }


                return true;
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine ("HTTP GREŠKA: " + httpEx);
                ShowError ("GREŠKA SERVERA", httpEx.Message);
                return false;
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine ("JSON GREŠKA: " + jsonEx);
                ShowError ("GREŠKA PODATAKA", jsonEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine ("NEOČEKIVANA GREŠKA: " + ex);
                ShowError ("GREŠKA", ex.ToString ());
                return false;
            }




            return true;
        }

        int? PoreskaStopa(string taxLabel)
        {

            int ps;
            switch (taxLabel)
            {

                case "\u0415":
                    ps = 1;
                    break;
                case "\u041A":
                    ps = 4;
                    break;
                case "\u0410":
                    ps = 1;
                    break;
                case "\u0408":
                    ps = 3;
                    break;
                default:
                    ps = 2;
                    break;
            }
            return ps;

        }
        private void ShowError(string title, string message)
        {

            Application.Current.Dispatcher.Invoke (() =>
            {
                var myMessageBox = new MyMessageBox
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                myMessageBox.MessageTitle.Text = title;
                myMessageBox.MessageText.Text = message;
                myMessageBox.Show ();
            });

        }

    }

}




