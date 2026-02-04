using Caupo.Data;
using Caupo.Helpers;
using Caupo.Models;
using Caupo.Properties;
using Caupo.ViewModels;
using Caupo.Views;
using MAES.Fiskal;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel;
using System.Text;
using System.Windows;
using System.Windows.Controls.Ribbon;
using Tring.Fiscal.Driver;
using Tring.Fiscal.Driver.Interfaces;
using static Caupo.Data.DatabaseTables;
using static System.Net.WebRequestMethods;


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
                    if(_quantity != value)
                    {
                        _quantity = value;
                        OnPropertyChanged (nameof (Quantity));
                        OnPropertyChanged (nameof (TotalAmount)); // Ako se Quantity promeni, ukupan iznos se mora ažurirati
                    }
                }
            }

            [JsonIgnore]
            public decimal PnpStopa
            {
                get
                {
                    if(decimal.TryParse (
                            Settings.Default.PnpStopa,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var pnp))
                    {
                        return Math.Clamp (pnp, 0m, 3m); // max 3%
                    }

                    return 0m;
                }
            }
            [JsonIgnore]
            public decimal Pdvstopa
            {
                get
                {
                    if(Labels == null || Labels.Count == 0)
                        return 0m;

                    return Labels[0] switch
                    {
                        "2" => 25m,
                        "1" => 13m,
                        "0" => 5m,
                        _ => 0m
                    };
                }
            }

            [JsonIgnore]
            public decimal UkupnoStope => Pdvstopa + PnpStopa;
            [JsonIgnore]
            public decimal? UkupnoPoreza => UnitPrice.HasValue ? Math.Round (UnitPrice.Value * (UkupnoStope / (100m + UkupnoStope)), 2, MidpointRounding.AwayFromZero) : null;

            [JsonIgnore]
            public decimal? Osnovica => UnitPrice.HasValue && UkupnoPoreza.HasValue ? UnitPrice - UkupnoPoreza : null;

            [JsonIgnore]
            public decimal? IznosPDV => (UkupnoPoreza.HasValue && UkupnoStope > 0) ? Math.Round (UkupnoPoreza.Value * (Pdvstopa / UkupnoStope), 2, MidpointRounding.AwayFromZero) : 0m;

            [JsonIgnore]
            public decimal? IznosPNP => (UkupnoPoreza.HasValue && UkupnoStope > 0) ? Math.Round (UkupnoPoreza.Value * (PnpStopa / UkupnoStope), 2, MidpointRounding.AwayFromZero) : 0m;


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
            [NotMapped]
            [JsonIgnore]
            public string? JedinicaMjereName
            {
                get
                {
                    return JedinicaMjere switch
                    {
                        1 => "kom",
                        2 => "kg",
                        3 => "m",
                        4 => "m2",
                        5 => "m3",
                        6 => "lit",
                        7 => "tona",
                        8 => "g",
                        9 => "por",
                        10 => "pak",
                        _ => ""
                    };
                }
            }


            [JsonIgnore]
            // [JsonProperty("Naziv", NullValueHandling = NullValueHandling.Ignore)]
            public string? Naziv { get; set; }
            [JsonIgnore]
            //  [JsonProperty("Printed", NullValueHandling = NullValueHandling.Ignore)]
            public string? Printed { get; set; }

            [JsonProperty ("note", NullValueHandling = NullValueHandling.Ignore)]
            private string? _note;

            public string? Note
            {
                get => _note;
                set
                {
                    if(_note != value)
                    {
                        _note = value;
                        OnPropertyChanged (nameof (Note));
                    }
                }
            }

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
        [JsonIgnore]
        public bool naknadna_fiskalizacija = false;

        public static bool reklamirani_racun = false;
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

            if(string.IsNullOrWhiteSpace (brojfiskalnog))
            {
                Debug.WriteLine ("❌ brojfiskalnog je NULL ili PRAZAN");
                return false;
            }

            try
            {
                using(var db = new AppDbContext ())
                {
                    Debug.WriteLine ("✔ AppDbContext kreiran");

                    // Provjera da li račun postoji prije update-a
                    var postoji = await db.Racuni
                        .AnyAsync (r => r.BrojFiskalnogRacuna == brojfiskalnog);

                    Debug.WriteLine ($"Postoji račun sa tim brojem: {postoji}");

                    if(!postoji)
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
            catch(Exception ex)
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
                if(string.IsNullOrWhiteSpace (journal))
                    throw new Exception ("Journal nije pronađen u JSON-u.");

                var verificationUrl = obj["verificationUrl"]?.ToString ();
                if(string.IsNullOrWhiteSpace (verificationUrl))
                    throw new Exception ("verificationUrl nije pronađen u JSON-u.");

                // Generiranje QR koda u Bitmap (bez spremanja u datoteku)
                using(QRCodeGenerator qrGenerator = new QRCodeGenerator ())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode (verificationUrl, QRCodeGenerator.ECCLevel.Q);
                    using(QRCode qrCode = new QRCode (qrCodeData))
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

                if(string.IsNullOrWhiteSpace (printerName))
                {
                    ShowError ("GREŠKA", $"POS printer nije podešen.\\nMolimo izaberite printer u postavkama.");
                    return;
                }

                pd.PrinterSettings.PrinterName = printerName;


                pd.Print ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("Greška prilikom ispisa: " + ex.Message);
            }
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            float sirinaMm = float.Parse (Settings.Default.SirinaTrake);
            if(sirinaMm < 80)
            {
            }
            using(Font font = new Font ("Consolas", 9))
            {

                int maxWidth = e.MarginBounds.Width;
                int pageWidth = e.PageBounds.Width;
                // float y = 10;

                SizeF textSize = e.Graphics.MeasureString (_journalText, font);

                int qrWidth = 0, qrHeight = 0;
                if(_qrBitmap != null)
                {
                    qrWidth = _qrBitmap.Width;
                    qrHeight = _qrBitmap.Height;

                    // Skaliranje QR koda ako je širi od trake
                    if(qrWidth > maxWidth)
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

                float textX = (maxWidth - textSize.Width) / 2 - 2; //e.MarginBounds.Left+ ((maxWidth - textSize.Width) / 2);
                float textY = startY;
                e.Graphics.DrawString (_journalText, font, Brushes.Black, new PointF (textX, textY));

                // Crtanje QR koda ispod teksta
                if(_qrBitmap != null)
                {

                    int qrX = (maxWidth - qrWidth) / 2;
                    int qrY = (int)(textY + textSize.Height + 10); // 10px razmak
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    e.Graphics.DrawImage (_qrBitmap, new Rectangle (qrX, qrY, qrWidth, qrHeight));
                }
            }
        }

        private TblRacuni KreirajRacun(int nacinPlacanjaIndex, decimal? totalSum, TblKupci? selectedKupac)
        {
            var racun = new TblRacuni
            {
                Datum = DateTime.Now,
                NacinPlacanja = nacinPlacanjaIndex,
                Radnik = Globals.ulogovaniKorisnik.IdRadnika.ToString(),
                Iznos = totalSum,
                Kupac = "Gradjani"
            };

            if(nacinPlacanjaIndex != -1 && selectedKupac != null)
            {
                racun.Kupac = selectedKupac.Kupac;
                racun.KupacId = selectedKupac.JIB;
            }

            return racun;
        }

        private async Task<int> BrojRacuna()
        {
            using var db = new AppDbContext ();

            // MaxAsync direktno na nullable int, EF Core vraća null ako nema redova
            int? maxBroj = await db.Racuni
                .MaxAsync (r => (int?)r.BrojRacuna);

            // Ako nema redova, vrati 0
            return maxBroj + 1 ?? 1;
        }


        public async Task<bool> IzdajFiskalniRacun(string invoiceType, string transactionType, string ReferentDocumentNumber, string referentDocumentDT,
            ObservableCollection<Item> StavkeRacuna,
            TblKupci? selectedKupac,
            int SelectedNacinPlacanjaIndex,
            decimal? TotalSum)
        {
            if(StavkeRacuna.Count == 0)
                return false;

            int brojracuna = await BrojRacuna ();


            Debug.WriteLine ("----------------------- BROJ RACUNA: " + brojracuna + " ------------------------------------------------------------ ");

            var Racun = KreirajRacun (SelectedNacinPlacanjaIndex, TotalSum, selectedKupac);

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

            if(Racun.Kupac != "Gradjani")
            {
                InvoiceRequest.BuyerId = Racun.KupacId;
                if(InvoiceRequest.BuyerId == "0000000000000")
                {
                    ShowError ("GREŠKA", $"Ne postoje podaci u bazi o kupcu {Racun.Kupac}.\nDodajte kupca u bazu prije izdavanja računa.");
                    return false;
                }
            }

            if(string.Equals (transactionType, "Refund", StringComparison.OrdinalIgnoreCase) || string.Equals (invoiceType, "Copy", StringComparison.OrdinalIgnoreCase))
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

                if(!responseAttention.IsSuccessStatusCode)
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

                if(!responseStatus.IsSuccessStatusCode)
                {
                    ShowError ("GREŠKA", $"Greška na serveru: {responseStatus.StatusCode}\nContent: {contentStatus}");
                    return false;
                }

                var lpfrStatus = JsonConvert.DeserializeObject<LpfrStatus> (contentStatus);
                if(lpfrStatus.Gsc.Contains ("1300"))
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

                if(!responsePIN.IsSuccessStatusCode)
                {
                    ShowError ("GREŠKA", $"Greška PIN: {responsePIN.StatusCode}\n{contentPIN}");
                    return false;
                }


            }
            // Greška u HTTP Request 
            catch(HttpRequestException ex)
            {
                string details = $"Message: {ex.Message}\n";
                if(ex.InnerException != null)
                    details += $"InnerException: {ex.InnerException.Message}\n";
                details += $"StackTrace: {ex.StackTrace}";

                ShowError ("GREŠKA", "HTTP Request exception:\n" + details +
                          $"\nIPLPFR: {IPLPFR}, APILPFR: {APILPFR}");
                return false;
            }
            //Greška u predugom trajanju
            catch(TaskCanceledException ex)
            {
                string details = $"Message: {ex.Message}\nStackTrace: {ex.StackTrace}";
                ShowError ("GREŠKA", "Zahtjev je istekao ili otkazan.\n" + details +
                          $"\nProvjerite da li je kasa upaljena i spojena.\nIPLPFR: {IPLPFR}, APILPFR: {APILPFR}");
                return false;
            }
            //Generalna greška
            catch(Exception ex)
            {
                string details = $"Message: {ex.Message}\n";
                if(ex.InnerException != null)
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
                if(!response.IsSuccessStatusCode)
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
                    if(invoiceType != "Copy")
                    {
                        if(transactionType == "Refund")
                        {
                            await UpdateRacunStornoAsync (ReferentDocumentNumber);
                        }
                        else
                        {

                            await SacuvajRacunAsync (Racun, StavkeRacuna, brojfiskalnog);
                            await PrintajBlokoveAsync (StavkeRacuna);

                        }
                    }
                }
                catch(Exception dbEx)
                {
                    Debug.WriteLine ("DB GREŠKA: " + dbEx);
                    ShowError ("GREŠKA BAZE", dbEx.Message);
                    return false;
                }


                return true;
            }
            catch(HttpRequestException httpEx)
            {
                Debug.WriteLine ("HTTP GREŠKA: " + httpEx);
                ShowError ("GREŠKA SERVERA", httpEx.Message);
                return false;
            }
            catch(JsonException jsonEx)
            {
                Debug.WriteLine ("JSON GREŠKA: " + jsonEx);
                ShowError ("GREŠKA PODATAKA", jsonEx.Message);
                return false;
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("NEOČEKIVANA GREŠKA: " + ex);
                ShowError ("GREŠKA", ex.ToString ());
                return false;
            }

        }

        private async Task SacuvajRacunAsync(TblRacuni racun, IEnumerable<FiskalniRacun.Item> stavkeRacuna, string brojFiskalnog)
        {
            using var db = new AppDbContext ();
            using var transaction = await db.Database.BeginTransactionAsync ();

            try
            {
                // --- Racun ---
                Debug.WriteLine("[Tring] RADNIK PRIJE SAVE: " + racun.Radnik);
                racun.BrojFiskalnogRacuna = brojFiskalnog;
                await db.Racuni.AddAsync (racun);
                await db.SaveChangesAsync ();

                // --- Stavke ---
                foreach(var s in stavkeRacuna)
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
                        BrojRacuna = racun.BrojRacuna
                    };

                    await db.RacunStavka.AddAsync (stavka);
                }

                await db.SaveChangesAsync ();
                await transaction.CommitAsync ();
            }
            catch
            {
                await transaction.RollbackAsync ();
                throw;
            }
        }

        private async Task PrintajBlokoveAsync(IEnumerable<FiskalniRacun.Item> stavke)
        {
            var sankStavke = stavke
                .Where (s => s.Proizvod == 0 && s.Printed != "DA")
                .ToList ();

            var kuhinjaStavke = stavke
                .Where (s => s.Proizvod == 1 && s.Printed != "DA")
                .ToList ();

            if(kuhinjaStavke.Any ())
            {
                var printer = new BlokPrinter (kuhinjaStavke, "Kuhinja", "Kasa", "Kasa");
                await printer.Print ();
            }

            if(sankStavke.Any ())
            {
                var printer = new BlokPrinter (sankStavke, "Sank", "Kasa", "Kasa");
                await printer.Print ();
            }
        }


        public async Task<bool> IzdajFiskalniRacunTring(
                        int SelectedNacinPlacanjaIndex,
                        TblRadnici radnik,
                        ObservableCollection<FiskalniRacun.Item> StavkeRacuna,
                        TblKupci? selectedKupac,
                        int brojRacuna
                        )
        {
            try
            {
                decimal? TotalSum = StavkeRacuna?.Sum (item => item.TotalAmount ?? 0);
                var Racun = KreirajRacun (SelectedNacinPlacanjaIndex, TotalSum, selectedKupac);

                // 1. Inicijalizacija printera
                TringFiskalniPrinter printer = new TringFiskalniPrinter ();

                try
                {
                    printer.Inicijalizacija ("127.0.0.1", 8085, 0, "0");
                    Debug.WriteLine ("[PRINTER] Printer je uspješno inicijalizovan.");
                }
                catch(System.Net.Sockets.SocketException sockEx)
                {
                    Debug.WriteLine ("[PRINTER] Socket greška: " + sockEx.Message);

                    MessageBox.Show (
                        "Ne može se povezati na printer.\nProvjerite da li je printer uključen i da li je port 8085 slobodan.",
                        "Greška konekcije",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    return false;
                }
                catch(Exception ex)
                {
                    Debug.WriteLine ("[PRINTER] Druga greška prilikom inicijalizacije: " + ex.Message);

                    MessageBox.Show (
                        "Došlo je do greške prilikom inicijalizacije printera.\n" + ex.Message,
                        "Greška printera",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    return false;
                }
                KasaOdgovor odgovor = new KasaOdgovor ();
                // 2. Napravi novi racun
                //TringModels.Racun racun = new TringModels.Racun ();
                Racun _racun = new Racun ();
                // 3. Dodaj kupca
                if(selectedKupac != null && selectedKupac.Kupac != "Gradjani")
                {
                    Kupac kupac = new Kupac
                    {
                        IDbroj = selectedKupac.JIB ?? "",
                        Naziv = selectedKupac.Kupac ?? "",
                        Adresa = selectedKupac.Adresa ?? "",
                        Grad = selectedKupac.Mjesto ?? ""
                    };
                    _racun.Kupac = kupac;
                }

                // 4. Dodaj stavke
                try
                {
                    foreach(var stavkaracuna in StavkeRacuna)
                    {
                        RacunStavka _stavka = new RacunStavka ();
                        Artikal art = new Artikal ();
                        art.Sifra = stavkaracuna.Sifra;
                        art.Naziv = stavkaracuna.Name;
                        art.JM = stavkaracuna.JedinicaMjereName;
                        switch(stavkaracuna.Labels[0])
                        {
                            case "1":
                                art.Stopa = VrstePoreskihStopa.A_Nulta_stopa_za_neregistrirane_obveznike;
                                break;

                            case "2":
                                art.Stopa = VrstePoreskihStopa.E_Opca_poreska_stopa_PDV;
                                break;

                            case "3":
                                art.Stopa = VrstePoreskihStopa.J_Nedefinirana;
                                break;
                            case "4":
                                art.Stopa = VrstePoreskihStopa.K_Poreska_stopa_PDV_za_artikle_oslobodjene_PDV;
                                break;
                            default:
                                break;
                        }

                        art.Cijena = Convert.ToDouble (stavkaracuna.UnitPrice);

                        _stavka.artikal = art;
                        _stavka.Kolicina = Convert.ToDouble (stavkaracuna.Quantity);
                        _stavka.Rabat = 0;

                        switch(SelectedNacinPlacanjaIndex)
                        {
                            case 0:

                                _racun.DodajVrstuPlacanja (VrstePlacanja.Gotovina, 0);

                                break;

                            case 1:

                                _racun.DodajVrstuPlacanja (VrstePlacanja.Kartica, 0);

                                break;

                            case 2:

                                _racun.DodajVrstuPlacanja (VrstePlacanja.Cek, 0);

                                break;

                            case 3:

                                _racun.DodajVrstuPlacanja (VrstePlacanja.Virman, 0);

                                break;

                            default:

                                break;
                        }

                        _racun.DodajStavkuRacuna (_stavka);
                    }

                    _racun.Napomena = radnik.Radnik + Environment.NewLine + "Int. broj računa: " + brojRacuna + Environment.NewLine + "Hvala na posjeti!";

                    odgovor = printer.StampatiFiskalniRacun (_racun);

                    if(odgovor.VrstaOdgovora == VrsteOdgovora.Greska)
                    {
                        // 1. Kreiramo string sa svim porukama greške
                        string greske = string.Join (Environment.NewLine,
                            odgovor.Odgovori.Select (o => $"{o.Naziv}: {o.Vrijednost}"));

                        // 2. Logujemo greške u debug
                        Debug.WriteLine ("Fiskalni printer vratio GREŠKU:");
                        foreach(var o in odgovor.Odgovori)
                        {
                            Debug.WriteLine ($"{o.Naziv}: {o.Vrijednost}");
                        }



                        return false;
                    }
                    else
                    {
                        Debug.WriteLine ("Fiskalni printer vratio OK:");

                        // Ispis svih odgovora u Debug i Console
                        foreach(var o in odgovor.Odgovori)
                        {
                            string linija = $"{o.Naziv}: {o.Vrijednost}";
                            Debug.WriteLine (linija);
                            Debug.WriteLine (linija);
                        }

                        // Pronalaženje broja fiskalnog računa
                        var brojFiskalnog = odgovor.Odgovori
                            .FirstOrDefault (o => o.Naziv == "BrojFiskalnogRacuna")?
                            .Vrijednost?.ToString ();

                        if(!string.IsNullOrEmpty (brojFiskalnog))
                        {
                            Debug.WriteLine ($"[TRING]Broj fiskalnog računa: {brojFiskalnog}");
                            Debug.WriteLine($"[TRING]Racun radnik: {Racun.Radnik}");
                            await SacuvajRacunAsync (Racun, StavkeRacuna, brojFiskalnog);
                            await PrintajBlokoveAsync (StavkeRacuna);
                        }
                    }
                    return true;
                }
                catch(Exception ex)
                {
                    MessageBox.Show (ex.Message);
                    return false;
                }

            }
            catch(Exception ex)
            {

                Debug.WriteLine ($"Stavke rašuna : {ex.Message}");
                return false;
            }
        }




        public decimal? pdv13osnovica;
        public decimal? pdv13iznos;
        public decimal? pdv25osnovica;
        public decimal? pdv25iznos;
        public decimal? pnposnovica;
        public decimal? pnpiznos;
        public string jir;
        string zki;
        public string naknadni_jir { get; set; }
        public string fiskalizovan = "DA";
        public string tekstGreske { get; set; }
        public async Task<bool> IzdajFiskalniRacunHrvatska(
           int vrstaplacanja,
           TblKupci Kupac,
           decimal? iznos_racuna,
           ObservableCollection<FiskalniRacun.Item> StavkeRacuna,
           string jir_poslani,
           bool naknadnadostava)
        {
            try
            {
                // === Postavljanje tipova plaćanja i ostalih parametara ===
                NacinPlacanjaType nacinPlacanjaType = vrstaplacanja switch
                {
                    0 => NacinPlacanjaType.G,
                    1 => NacinPlacanjaType.K,
                    2 => NacinPlacanjaType.C,
                    3 => NacinPlacanjaType.T,
                    4 => NacinPlacanjaType.O,
                    _ => throw new ArgumentOutOfRangeException (nameof (vrstaplacanja), "Nepoznata vrsta placanja")
                };

                bool USustPdv = Settings.Default.PDVKorisnik?.ToUpper () == "DA";
                string radnik = Globals.ulogovaniKorisnik.Radnik;
                string oib_operatera = Globals.ulogovaniKorisnik.IB;
                string oznaka_slijednosti = Settings.Default.OznakaSlijednosti;
                string oznaka_pos_prostora = Settings.Default.PoslovniProstor;
                string oznaka_nap_uredjaja = Settings.Default.NaplatniUredjaj;
                string oib_firme = Settings.Default.JIB;
                string firma = Settings.Default.Firma;
                string kupac = Kupac.Kupac;
                string datv = DateTime.Now.ToString ("dd.MM.yyyyTHH:mm:ss");
                string datvQr = DateTime.Now.ToString ("yyyyMMdd_HHmm");
                string brojracuna = (await BrojRacuna ()).ToString ();
                brojfiskalnog = brojracuna;

                OznakaSlijednostiType oznakaSlijednostiType = oznaka_slijednosti switch
                {
                    "Na nivou poslovnog prostora" => OznakaSlijednostiType.P,
                    _ => OznakaSlijednostiType.N
                };

                string pnpStopa = decimal.TryParse (Settings.Default.PnpStopa, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p.ToString ("0.00", CultureInfo.InvariantCulture) : "0.00";

                // === Porezi ===
                var porezi = new Dictionary<int, (PorezType Porez, decimal? Osnovica, decimal? Iznos)>
        {
            { 0, (new PorezType { Stopa = 5.ToString("F2", CultureInfo.InvariantCulture) }, 0, 0) },
            { 1, (new PorezType { Stopa = 13.ToString("F2", CultureInfo.InvariantCulture) }, 0, 0) },
            { 2, (new PorezType { Stopa = 25.ToString("F2", CultureInfo.InvariantCulture) }, 0, 0) },
            { 3, (new PorezType { Stopa = pnpStopa }, 0, 0) },
        };

                foreach(var stavka in StavkeRacuna)
                {
                    int stopa = Convert.ToInt32 (stavka.Labels[0]);
                    if(porezi.ContainsKey (stopa))
                    {
                        var entry = porezi[stopa];
                        entry.Osnovica += Convert.ToDecimal (stavka.Osnovica) * Convert.ToDecimal (stavka.Quantity);
                        entry.Iznos += Convert.ToDecimal (stavka.IznosPDV) * Convert.ToDecimal (stavka.Quantity);
                        porezi[stopa] = entry;
                    }

                    if(stavka.PnpStopa > 0)
                    {
                        var entry = porezi[3];
                        entry.Osnovica += Convert.ToDecimal (stavka.Osnovica) * Convert.ToDecimal (stavka.Quantity);
                        entry.Iznos += Convert.ToDecimal (stavka.IznosPNP) * Convert.ToDecimal (stavka.Quantity);
                        porezi[3] = entry;
                    }
                }

                foreach(var key in porezi.Keys.ToList ())
                {
                    var entry = porezi[key];
                    entry.Porez.Osnovica = entry.Osnovica.HasValue ? entry.Osnovica.Value.ToString ("F2", CultureInfo.InvariantCulture) : "0.00";
                    entry.Porez.Iznos = entry.Iznos.HasValue ? entry.Iznos.Value.ToString ("F2", CultureInfo.InvariantCulture) : "0.00";
                    porezi[key] = entry;
                }

                var pdv5 = porezi[0].Porez;
                var pdv13 = porezi[1].Porez;
                var pdv25 = porezi[2].Porez;
                var pnp = porezi[3].Porez;

                pdv25iznos = decimal.Parse (pdv25.Iznos);
                pdv25osnovica = decimal.Parse (pdv25.Osnovica);
                pdv13iznos = decimal.Parse (pdv13.Iznos);
                pdv13osnovica = decimal.Parse (pdv13.Osnovica);
                pnpiznos = decimal.Parse (pnp.Iznos);
                pnposnovica = decimal.Parse (pnp.Osnovica);

                if(!kopijaracuna && vrstaplacanja != 3 & vrstaplacanja != 5)
                {
                    var invoice = new RacunType
                    {
                        BrRac = new BrojRacunaType
                        {
                            BrOznRac = brojracuna,
                            OznPosPr = oznaka_pos_prostora,
                            OznNapUr = oznaka_nap_uredjaja
                        },
                        DatVrijeme = datv,
                        IznosUkupno = iznos_racuna.HasValue ? iznos_racuna.Value.ToString ("F2", CultureInfo.InvariantCulture) : "0.00",
                        NakDost = naknadnadostava,
                        Oib = oib_firme,
                        OibOper = oib_operatera,
                        OznSlijed = oznakaSlijednostiType,
                        Pdv = new[] { pdv13, pdv25 },
                        Pnp = new[] { pnp },
                        USustPdv = USustPdv,
                        NacinPlac = nacinPlacanjaType
                    };

                    // === Učitaj sertifikat ===
                    string certificatesFolder = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, "Certificates");
                    string certPath = Path.Combine (certificatesFolder, Properties.Settings.Default.CerificateName);
                    string certpass = "Demo2022";

                    X509Certificate2 certificate = new X509Certificate2 (
                        certPath,
                        certpass,
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable
                    );

                    using(RSA rsa = certificate.GetRSAPrivateKey ())
                    {
                        if(rsa == null)
                            throw new Exception ("RSA privatni ključ nije dostupan.");

                        zki = invoice.ZKI (certificate);
                        invoice.ZastKod = zki;
                    }

                    string url = Settings.Default.VerzijaAplikacije?.Trim ().Equals ("Demo", StringComparison.OrdinalIgnoreCase) == true
                        ? "https://cistest.apis-it.hr:8449/FiskalizacijaServiceTest"
                        : "https://cistest.apis-it.hr:8449/FiskalizacijaService";

                    if(NetworkHelper.HasInternet ())
                    {
                        try
                        {
                            var response = await invoice.SendAsync (certificate, url);
                            if(response != null && !string.IsNullOrWhiteSpace (response.Jir))
                            {
                                jir = response.Jir;
                                fiskalizovan = "DA";
                            }
                          

                            // Prikaži SOAP greške ako ih ima
                            if(response?.Greske != null && response.Greske.Length > 0)
                            {
                                var porukeGresaka = response.Greske.Select (g => g.PorukaGreske).ToArray ();
                                Debug.WriteLine ("[FISKALNI HR] GREŠKA: " + string.Join (", ", porukeGresaka));
                                tekstGreske = ("[FISKALNI HR] GREŠKA: " + string.Join (", ", porukeGresaka));
                                fiskalizovan = "NE";
                            }
                        }
                        catch(Exception ex)
                        {
                            fiskalizovan = "NE";
                            Debug.WriteLine (ex.ToString ());
                            HandleFiscalizationException (ex);
                            return false;
                        }
                    }
                    else
                    {
                        tekstGreske = "Internet nije dostupan";
                        fiskalizovan = "NE";
                    }

                        // === Lokalno spremanje + printanje (oba slučaja) ===
                        await SpremiIPrintajRacun (
                            kupac, radnik, vrstaplacanja, brojfiskalnog, jir, zki,
                            brojracuna, oznaka_pos_prostora, oznaka_nap_uredjaja,
                            StavkeRacuna, iznos_racuna, datv, datvQr,
                            pdv13iznos, pdv13osnovica, pdv25iznos, pdv25osnovica, pnpiznos, pnposnovica, fiskalizovan, tekstGreske
                        );
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("GLOBALNAGREŠKA: " + ex.Message);
                ShowError ("GLOBALNAGREŠKA:", ex.Message);
                return false;
            }

            return true;
        }

        private void HandleFiscalizationException(Exception r)
        {
            string odgovor = r.ToString ();
            string naslov = "GREŠKA";
            string tekst = "";

            if(odgovor.Contains ("s005"))
                tekst = "OIB iz poruke zahtjeva nije jednak OIB-u iz certifikata. Račun nije fiskalizovan.";
            else if(odgovor.Contains ("s001") || odgovor.Contains ("s004") || odgovor.Contains ("s006"))
            {
                if(odgovor.Length > 250)
                    odgovor = odgovor.Substring (0, 250);
                tekst = $"Dogodila se greška: {Environment.NewLine}{Environment.NewLine}{odgovor}{Environment.NewLine}Račun nije fiskalizovan.";
            }
            else if(odgovor.Contains ("The operation has timed out"))
                tekst = "Predviđeno vrijeme za fiskalizaciju računa je isteklo. Račun nije fiskalizovan.";
            else if(odgovor.Contains ("The remote name could not be resolved"))
                tekst = "Niste spojeni na internet, provjerite vezu! Račun nije fiskalizovan, potrebno je to uraditi naknadno (najkasnije 48h).";

            if(!string.IsNullOrEmpty (tekst))
            {
                MyMessageBox frm = new MyMessageBox ();
                frm.MessageText.Text = tekst;
                frm.MessageTitle.Text = naslov;
                frm.ShowDialog ();

                jir = " ";
                fiskalizovan = "NE";
            }
        }

        // === Privatna metoda za spremanje i printanje ===
        private async Task SpremiIPrintajRacun(
            string kupac, string radnik, int vrstaplacanja, string brojfiskalnog,
            string jir, string zki, string brojracuna, string oznaka_pos_prostora,
            string oznaka_nap_uredjaja, ObservableCollection<FiskalniRacun.Item> StavkeRacuna,
            decimal? iznos_racuna, string datv, string datvQr,
            decimal? pdv13iznos, decimal? pdv13osnovica, decimal? pdv25iznos, decimal? pdv25osnovica,
            decimal? pnpiznos, decimal? pnposnovica, string fiskalizovan, string tekstGreske)
        {
            TblRacuni racun = new TblRacuni
            {
                Kupac = kupac,
                Datum = DateTime.ParseExact (datv, "dd.MM.yyyyTHH:mm:ss", CultureInfo.InvariantCulture),
                Radnik = radnik,
                NacinPlacanja = vrstaplacanja,
                BrojFiskalnogRacuna = brojfiskalnog,
                Fiskalizovan = fiskalizovan,
                Jir = jir,
                Zki = zki,
                BrojRacunaHr = brojracuna + "/" + oznaka_pos_prostora + "/" + oznaka_nap_uredjaja,
                Iznos = iznos_racuna
            };

            try
            {
                if(!kopijaracuna)
                {
                    if(reklamirani_racun)
                    {
                        await UpdateRacunStornoAsync (brojfiskalnog);
                    }
                    else
                    {
                        await SacuvajRacunAsync (racun, StavkeRacuna, brojfiskalnog);
                        await PrintajBlokoveAsync (StavkeRacuna);

                        var racunModel = new PrintRacunHrvatska.RacunModel
                        {
                            Firma = Settings.Default.Firma,
                            Adresa = Settings.Default.Adresa,
                            Grad = Settings.Default.Mjesto,
                            Oib = Settings.Default.JIB,
                            BrojRacuna = racun.BrojRacunaHr,
                            DatumVrijeme = racun.Datum,
                            Konobar = racun.Radnik,
                            NacinPlacanja = racun.NacinPlacanja,
                            Stavke = StavkeRacuna,
                            Ukupno = racun.Iznos,
                            JIR = racun.Jir,
                            ZIK = racun.Zki,
                            Logo = Image.FromFile (Settings.Default.LogoUrl),
                            QR = @"https://porezna.gov.hr/rn?jir=" + jir + "&datv=" + datvQr + "&izn=" + iznos_racuna * 100,
                            PDV13 = pdv13iznos,
                            PDV13Osnovica = pdv13osnovica,
                            PDV25 = pdv25iznos,
                            PDV25Osnovica = pdv25osnovica,
                            PNP = pnpiznos,
                            PNPOsnovica = pnposnovica,
                            PNPPostotak = Settings.Default.PnpStopa
                        };

                        PrintRacunHrvatska.Print (racunModel);
                        if(fiskalizovan == "NE"){
                            ShowError ("GREŠKA FISKALIZACIJE", "Račun nije fiskalizovan i spremljen je u bazu kao takav."+ Environment.NewLine + "Imate 48h da ga naknadno fiskalizujete" + Environment.NewLine + "Razlog: "+tekstGreske);
                        }
                     
                    }
                }
            }
            catch(Exception dbEx)
            {
                Debug.WriteLine ("DB GREŠKA: " + dbEx);
                ShowError ("GREŠKA BAZE", dbEx.Message);
            }
        }




        int? PoreskaStopa(string taxLabel)
        {

            int ps;
            switch(taxLabel)
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




