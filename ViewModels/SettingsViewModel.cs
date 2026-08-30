using Caupo.Data;
using Caupo.Helpers;
using Caupo.Properties;
using Caupo.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        #region Win32 API - monitori

        [DllImport ("user32.dll")]
        private static extern bool EnumDisplayMonitors(
            IntPtr hdc,
            IntPtr lprcClip,
            MonitorEnumProc lpfnEnum,
            IntPtr dwData);

        private delegate bool MonitorEnumProc(
            IntPtr hMonitor,
            IntPtr hdcMonitor,
            IntPtr lprcMonitor,
            IntPtr dwData);

        [DllImport ("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(
            IntPtr hMonitor,
            ref MONITORINFOEX lpmi);

        [DllImport ("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplayDevices(
            string? lpDevice,
            uint iDevNum,
            ref DISPLAY_DEVICE lpDisplayDevice,
            uint dwFlags);

        [StructLayout (LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;

            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout (LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout (LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DISPLAY_DEVICE
        {
            public int cb;

            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;

            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;

            public uint StateFlags;

            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;

            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        #endregion


        #region Država

        public enum Drzava
        {
            FederacijaBiH,
            RepublikaSrpska,
            Hrvatska,
            Srbija
        }

        public Array Drzave => Enum.GetValues (typeof (Drzava));

        private Drzava _odabranaDrzava;

        public Drzava OdabranaDrzava
        {
            get => _odabranaDrzava;
            set
            {
                if(SetProperty (ref _odabranaDrzava, value))
                {
                    OnPropertyChanged (nameof (ShowFBIH));
                    OnPropertyChanged (nameof (ShowRS));
                    OnPropertyChanged (nameof (ShowHR));
                    OnPropertyChanged (nameof (ShowSR));
                }
            }
        }

        public bool ShowFBIH =>
            OdabranaDrzava == Drzava.FederacijaBiH;

        public bool ShowRS =>
            OdabranaDrzava == Drzava.RepublikaSrpska;

        public bool ShowHR =>
            OdabranaDrzava == Drzava.Hrvatska;

        public bool ShowSR =>
            OdabranaDrzava == Drzava.Srbija;

        #endregion


        #region Firma

        private string _firma = string.Empty;
        public string Firma
        {
            get => _firma;
            set => SetProperty (ref _firma, value);
        }

        private string _adresa = string.Empty;
        public string Adresa
        {
            get => _adresa;
            set => SetProperty (ref _adresa, value);
        }

        private string _mjesto = string.Empty;
        public string Mjesto
        {
            get => _mjesto;
            set => SetProperty (ref _mjesto, value);
        }

        private string _jib = string.Empty;
        public string JIB
        {
            get => _jib;
            set => SetProperty (ref _jib, value);
        }

        private string _pdv = string.Empty;
        public string PDV
        {
            get => _pdv;
            set => SetProperty (ref _pdv, value);
        }

        private string _zr = string.Empty;
        public string ZR
        {
            get => _zr;
            set => SetProperty (ref _zr, value);
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetProperty (ref _email, value);
        }

        private int _pdvKorisnik;
        public int PDVKorisnik
        {
            get => _pdvKorisnik;
            set => SetProperty (ref _pdvKorisnik, value);
        }

        #endregion


        #region Federacija BiH

        private string _tringServerIpAddress = string.Empty;

        public string TringServerIpAddress
        {
            get => _tringServerIpAddress;
            set => SetProperty (ref _tringServerIpAddress, value);
        }

        #endregion


        #region Republika Srpska

        private string _lpfrIP = string.Empty;

        public string LPFRIP
        {
            get => _lpfrIP;
            set => SetProperty (ref _lpfrIP, value);
        }

        private string _lpfrKey = string.Empty;

        public string LPFRKey
        {
            get => _lpfrKey;
            set => SetProperty (ref _lpfrKey, value);
        }

        private string _lpfrPin = string.Empty;

        public string LPFRPin
        {
            get => _lpfrPin;
            set => SetProperty (ref _lpfrPin, value);
        }

        private int _externiPrinter;

        public int ExterniPrinter
        {
            get => _externiPrinter;
            set => SetProperty (ref _externiPrinter, value);
        }

        private string _sirinaTrake = "80";

        public string SirinaTrake
        {
            get => _sirinaTrake;
            set => SetProperty (ref _sirinaTrake, value);
        }

        #endregion


        #region Hrvatska

        private string _demoServerUrl = string.Empty;

        public string DemoServerUrl
        {
            get => _demoServerUrl;
            set => SetProperty (ref _demoServerUrl, value);
        }

        private string _productionServerUrl = string.Empty;

        public string ProductionServerUrl
        {
            get => _productionServerUrl;
            set => SetProperty (ref _productionServerUrl, value);
        }

        private string _poslovniProstor = string.Empty;

        public string PoslovniProstor
        {
            get => _poslovniProstor;
            set => SetProperty (ref _poslovniProstor, value);
        }

        private string _naplatniUredjaj = string.Empty;

        public string NaplatniUredjaj
        {
            get => _naplatniUredjaj;
            set => SetProperty (ref _naplatniUredjaj, value);
        }

        private int _oznakaSlijednosti;

        public int OznakaSlijednosti
        {
            get => _oznakaSlijednosti;
            set => SetProperty (ref _oznakaSlijednosti, value);
        }

        private int _verzijaAplikacije;

        public int VerzijaAplikacije
        {
            get => _verzijaAplikacije;
            set => SetProperty (ref _verzijaAplikacije, value);
        }

        private string _certificateName = string.Empty;

        public string CertificateName
        {
            get => _certificateName;
            set => SetProperty (ref _certificateName, value);
        }

        private string _certificatePassword = string.Empty;

        public string CertificatePassword
        {
            get => _certificatePassword;
            set => SetProperty (ref _certificatePassword, value);
        }

        private string _pnpStopa = string.Empty;

        public string PnpStopa
        {
            get => _pnpStopa;
            set => SetProperty (ref _pnpStopa, value);
        }

        #endregion


        #region Srbija

        // =====================================================
        // OPŠTE
        // =====================================================

        private string _srbijaPfrType = "VPFR";

        public string SrbijaPfrType
        {
            get => _srbijaPfrType;
            set => SetProperty (ref _srbijaPfrType, value);
        }


        private string _srbijaEnvironment = "Sandbox";

        public string SrbijaEnvironment
        {
            get => _srbijaEnvironment;
            set => SetProperty (ref _srbijaEnvironment, value);
        }


        // =====================================================
        // VPFR
        // =====================================================

        private string _srbijaVPFRUrl =
            "https://vsdc.sandbox.suf.purs.gov.rs";

        public string SrbijaVPFRUrl
        {
            get => _srbijaVPFRUrl;
            set => SetProperty (ref _srbijaVPFRUrl, value);
        }


        private string _srbijaCertificateName = string.Empty;

        public string SrbijaCertificateName
        {
            get => _srbijaCertificateName;
            set => SetProperty (ref _srbijaCertificateName, value);
        }


        private string _srbijaCertificatePassword = string.Empty;

        public string SrbijaCertificatePassword
        {
            get => _srbijaCertificatePassword;
            set => SetProperty (ref _srbijaCertificatePassword, value);
        }


        private string _srbijaPAC = string.Empty;

        public string SrbijaPAC
        {
            get => _srbijaPAC;
            set => SetProperty (ref _srbijaPAC, value);
        }


        private string _srbijaAcceptLanguage = "en-US";

        public string SrbijaAcceptLanguage
        {
            get => _srbijaAcceptLanguage;
            set => SetProperty (ref _srbijaAcceptLanguage, value);
        }


        // =====================================================
        // LPFR
        // =====================================================

        private string _srbijaLPFRToken = string.Empty;

        public string SrbijaLPFRToken
        {
            get => _srbijaLPFRToken;
            set => SetProperty (ref _srbijaLPFRToken, value);
        }


        private string _srbijaLPFRUrl = string.Empty;

        public string SrbijaLPFRUrl
        {
            get => _srbijaLPFRUrl;
            set => SetProperty (ref _srbijaLPFRUrl, value);
        }


        private string _srbijaLPFRPin = string.Empty;

        public string SrbijaLPFRPin
        {
            get => _srbijaLPFRPin;
            set => SetProperty (ref _srbijaLPFRPin, value);
        }


        private string _srbijaLPFRJid = string.Empty;

        public string SrbijaLPFRJid
        {
            get => _srbijaLPFRJid;
            set => SetProperty (ref _srbijaLPFRJid, value);
        }


        // =====================================================
        // STATUS
        // =====================================================

        private string _srbijaConnectionStatus = string.Empty;

        public string SrbijaConnectionStatus
        {
            get => _srbijaConnectionStatus;
            set => SetProperty (ref _srbijaConnectionStatus, value);
        }


        private ObservableCollection<TblPoreskeStope> _poreskeStope =
    new ObservableCollection<TblPoreskeStope> ();

        public ObservableCollection<TblPoreskeStope> PoreskeStope
        {
            get => _poreskeStope;
            set => SetProperty (ref _poreskeStope, value);
        }

        #endregion


        #region Aplikacija

        private string _dbPath = string.Empty;

        public string DbPath
        {
            get => _dbPath;
            set => SetProperty (ref _dbPath, value);
        }

        private string _backupUrl = string.Empty;

        public string BackupUrl
        {
            get => _backupUrl;
            set => SetProperty (ref _backupUrl, value);
        }

        private string _logoUrl = string.Empty;

        public string LogoUrl
        {
            get => _logoUrl;
            set => SetProperty (ref _logoUrl, value);
        }

        private string _posPrinter = string.Empty;

        public string POSPrinter
        {
            get => _posPrinter;
            set => SetProperty (ref _posPrinter, value);
        }

        private string _a4Printer = string.Empty;

        public string A4Printer
        {
            get => _a4Printer;
            set => SetProperty (ref _a4Printer, value);
        }

        private string _kuhinjaPrinter = string.Empty;

        public string KuhinjaPrinter
        {
            get => _kuhinjaPrinter;
            set => SetProperty (ref _kuhinjaPrinter, value);
        }

        private string _sankPrinter = string.Empty;

        public string SankPrinter
        {
            get => _sankPrinter;
            set => SetProperty (ref _sankPrinter, value);
        }

        private string _brojKopijaBloka = "1";

        public string BrojKopijaBloka
        {
            get => _brojKopijaBloka;
            set => SetProperty (ref _brojKopijaBloka, value);
        }

        private int _prodajaMinus;

        public int ProdajaMinus
        {
            get => _prodajaMinus;
            set => SetProperty (ref _prodajaMinus, value);
        }

        private int _multiUser;

        public int MultiUser
        {
            get => _multiUser;
            set => SetProperty (ref _multiUser, value);
        }

        private int _tema;

        public int Tema
        {
            get => _tema;
            set
            {
                if(SetProperty (ref _tema, value))
                    ApplySelectedTheme ();
            }
        }

        private string _serverIP = string.Empty;

        public string ServerIP
        {
            get => _serverIP;
            set => SetProperty (ref _serverIP, value);
        }

        #endregion


        #region Monitori

        public class MonitorInfo
        {
            public string Name { get; set; } = string.Empty;

            public int Index { get; set; }

            public string DeviceName { get; set; } = string.Empty;

            public bool IsPrimary { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public ObservableCollection<MonitorInfo> Monitors { get; } =
            new ObservableCollection<MonitorInfo> ();

        private MonitorInfo? _selectedMonitor;

        public MonitorInfo? SelectedMonitor
        {
            get => _selectedMonitor;
            set
            {
                if(SetProperty (ref _selectedMonitor, value))
                {
                    DisplayKuhinja = value?.Index ?? 0;
                }
            }
        }

        private int _displayKuhinja;

        public int DisplayKuhinja
        {
            get => _displayKuhinja;
            set => SetProperty (ref _displayKuhinja, value);
        }

        #endregion


        #region Radnici

        private ObservableCollection<TblRadnici> _radnici =
            new ObservableCollection<TblRadnici> ();

        public ObservableCollection<TblRadnici> Radnici
        {
            get => _radnici;
            set => SetProperty (ref _radnici, value);
        }

        private TblRadnici? _selectedRadnik;

        public TblRadnici? SelectedRadnik
        {
            get => _selectedRadnik;
            set => SetProperty (ref _selectedRadnik, value);
        }

        public bool isUpdate = false;

        #endregion


        #region Commands

        public ICommand SaveCommand { get; }

        public ICommand SelectDatabaseCommand { get; }

        public ICommand SelectLogoCommand { get; }

        public ICommand SelectBackupCommand { get; }

        public ICommand SelectSankPrinterCommand { get; }

        public ICommand SelectKuhinjaPrinterCommand { get; }

        public ICommand SelectPOSPrinterCommand { get; }

        public ICommand SelectA4PrinterCommand { get; }

        public ICommand SelectCroatiaCertificateCommand { get; }

        public ICommand SelectSrbijaCertificateCommand { get; }

        public ICommand TestSrbijaPfrCommand { get; }

        public ICommand DobaviSrbijaPoreskeStopeCommand { get; }

        public ICommand NewRadnikCommand { get; }

        public ICommand UpdateCommand { get; }

        public ICommand DeleteCommand { get; }

        #endregion


        #region Constructor

        public SettingsViewModel()
        {
            SaveCommand =
                new AsyncRelayCommand (SaveSettingsAsync);

            SelectDatabaseCommand =
                new RelayCommand (SelectDatabase);

            SelectLogoCommand =
                new RelayCommand (SelectLogo);

            SelectBackupCommand =
                new RelayCommand (SelectBackup);

            SelectSankPrinterCommand =
                new RelayCommand (() =>
                    SelectPrinter (
                        "Odaberite printer za šank",
                        p => SankPrinter = p));

            SelectKuhinjaPrinterCommand =
                new RelayCommand (() =>
                    SelectPrinter (
                        "Odaberite printer za kuhinju",
                        p => KuhinjaPrinter = p));

            SelectPOSPrinterCommand =
                new RelayCommand (() =>
                    SelectPrinter (
                        "Odaberite printer računa",
                        p => POSPrinter = p));

            SelectA4PrinterCommand =
                new RelayCommand (() =>
                    SelectPrinter (
                        "Odaberite A4 printer",
                        p => A4Printer = p));

            SelectCroatiaCertificateCommand =
                new RelayCommand (SelectCroatiaCertificate);

            SelectSrbijaCertificateCommand =
                new RelayCommand (SelectSrbijaCertificate);

            TestSrbijaPfrCommand =
                new AsyncRelayCommand (TestSrbijaPfrAsync);

            DobaviSrbijaPoreskeStopeCommand =
                    new AsyncRelayCommand (DobaviSrbijaPoreskeStopeAsync);

            NewRadnikCommand =
                new RelayCommand (OpenNewRadnik);

            UpdateCommand =
                new AsyncRelayCommand (
                    async () => await OpenUpdateRadnik (SelectedRadnik));

            DeleteCommand =
                new AsyncRelayCommand (
                    async () => await DeleteRadnik (SelectedRadnik));


            foreach(var monitor in GetAllMonitors ())
                Monitors.Add (monitor);


            LoadSettingsFromProperties ();


            _ = LoadRadniciAsync ();

            _ = LoadPoreskeStopeAsync ();
        }

        #endregion


        private async Task LoadPoreskeStopeAsync()
        {
            try
            {
                await using var db =
                    new AppDbContext ();

                var stope =
    await db.PoreskeStope
        .AsNoTracking ()
        .Where (x => x.Aktivna)
        .OrderBy (x => x.IdStope)
        .ToListAsync ();

                PoreskeStope =
                    new ObservableCollection<TblPoreskeStope> (
                        stope);
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[SRBIJA] Greška učitavanja poreskih stopa: {ex}");
            }
        }

        private async Task<string> GetSrbijaVPFRStatusBodyAsync()
        {
            if(string.IsNullOrWhiteSpace (SrbijaVPFRUrl))
            {
                throw new Exception (
                    "V-PFR URL nije podešen.");
            }

            if(string.IsNullOrWhiteSpace (SrbijaCertificateName))
            {
                throw new Exception (
                    "Certifikat nije odabran.");
            }

            if(string.IsNullOrWhiteSpace (SrbijaCertificatePassword))
            {
                throw new Exception (
                    "Zaporka certifikata nije unesena.");
            }

            if(string.IsNullOrWhiteSpace (SrbijaPAC))
            {
                throw new Exception (
                    "PAC nije unesen.");
            }

            string certificatePath =
                Path.Combine (
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Certificates",
                    SrbijaCertificateName);

            if(!File.Exists (certificatePath))
            {
                throw new FileNotFoundException (
                    "Certifikat nije pronađen.",
                    certificatePath);
            }

            var certificate =
                new X509Certificate2 (
                    certificatePath,
                    SrbijaCertificatePassword,
                    X509KeyStorageFlags.MachineKeySet |
                    X509KeyStorageFlags.Exportable);

            using var handler =
                new HttpClientHandler ();

            handler.ClientCertificateOptions =
                ClientCertificateOption.Manual;

            handler.ClientCertificates.Add (
                certificate);

            handler.UseProxy = false;

            using var client =
                new HttpClient (handler);

            client.DefaultRequestHeaders.Accept.Clear ();

            client.DefaultRequestHeaders.Accept.Add (
                new MediaTypeWithQualityHeaderValue (
                    "application/json"));

            client.DefaultRequestHeaders.Add (
                "PAC",
                SrbijaPAC);

            if(!string.IsNullOrWhiteSpace (
                SrbijaAcceptLanguage))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation (
                    "Accept-Language",
                    SrbijaAcceptLanguage);
            }

            string baseUrl =
                SrbijaVPFRUrl.TrimEnd ('/');

            string statusUrl;

            if(baseUrl.EndsWith (
                "/api/v3/status",
                StringComparison.OrdinalIgnoreCase))
            {
                statusUrl = baseUrl;
            }
            else
            {
                statusUrl =
                    baseUrl + "/api/v3/status";
            }

            Debug.WriteLine (
                $"[SRBIJA VPFR] Status URL: {statusUrl}");

            using HttpResponseMessage response =
                await client.GetAsync (statusUrl);

            string body =
                await response.Content
                    .ReadAsStringAsync ();

            Debug.WriteLine (
                $"[SRBIJA VPFR] Status HTTP: " +
                $"{(int)response.StatusCode} " +
                $"{response.StatusCode}");

            Debug.WriteLine (
                $"[SRBIJA VPFR] Status response: {body}");

            if(!response.IsSuccessStatusCode)
            {
                throw new Exception (
                    $"V-PFR status greška: " +
                    $"{(int)response.StatusCode} " +
                    $"{response.StatusCode}\n{body}");
            }

            return body;
        }

        private async Task DobaviSrbijaPoreskeStopeAsync()
        {
            SrbijaConnectionStatus =
                "Dobavljanje poreskih stopa...";

            try
            {
                string body =
                    await GetSrbijaVPFRStatusBodyAsync ();

                using JsonDocument document =
                    JsonDocument.Parse (body);

                JsonElement root =
                    document.RootElement;

                if(!root.TryGetProperty (
                    "currentTaxRates",
                    out JsonElement currentTaxRates))
                {
                    throw new Exception (
                        "VPFR odgovor ne sadrži currentTaxRates.");
                }

                if(!currentTaxRates.TryGetProperty (
                    "taxCategories",
                    out JsonElement taxCategories))
                {
                    throw new Exception (
                        "VPFR odgovor ne sadrži taxCategories.");
                }

                var stope =
                    new List<TblPoreskeStope> ();

                foreach(JsonElement category
                    in taxCategories.EnumerateArray ())
                {
                    string opis =
                        category.TryGetProperty (
                            "name",
                            out JsonElement nameElement)
                        ? nameElement.GetString () ?? string.Empty
                        : string.Empty;

                    if(!category.TryGetProperty (
                        "taxRates",
                        out JsonElement taxRates))
                    {
                        continue;
                    }

                    foreach(JsonElement taxRate
                        in taxRates.EnumerateArray ())
                    {
                        decimal postotak =
                            taxRate.GetProperty ("rate")
                                .GetDecimal ();

                        string oznaka =
                            taxRate.GetProperty ("label")
                                .GetString ()
                            ?? string.Empty;

                        stope.Add (
                            new TblPoreskeStope
                            {
                                Postotak = postotak,
                                Opis = opis,
                                Oznaka = oznaka
                            });
                    }
                }

                if(stope.Count == 0)
                {
                    throw new Exception (
                        "VPFR nije vratio nijednu aktivnu poresku stopu.");
                }

                await SpremiSrbijaPoreskeStopeAsync (
                    stope);

                await LoadPoreskeStopeAsync ();

                SrbijaConnectionStatus =
                    $"Poreske stope učitane: {stope.Count}";

                Debug.WriteLine (
                    $"[SRBIJA] Učitano {stope.Count} poreskih stopa.");

                foreach(var stopa in stope)
                {
                    Debug.WriteLine (
                        $"[SRBIJA] {stopa.Opis} | " +
                        $"{stopa.Postotak:0.####}% | " +
                        $"{stopa.Oznaka}");
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[SRBIJA] Greška dobavljanja poreskih stopa: {ex}");

                SrbijaConnectionStatus =
                    "Greška: " + ex.Message;
            }
        }


        private async Task SpremiSrbijaPoreskeStopeAsync(
    List<TblPoreskeStope> noveStope)
        {
            if(noveStope == null ||
               noveStope.Count == 0)
            {
                throw new Exception (
                    "Nema poreskih stopa za spremanje.");
            }

            await using var db =
                new AppDbContext ();

            var postojeceStope =
                await db.PoreskeStope
                    .ToListAsync ();

            // Sve postojeće prvo označimo kao neaktivne.
            // One koje VPFR ponovo vrati biće ponovo aktivirane.
            foreach(var postojeca in postojeceStope)
            {
                postojeca.Aktivna = false;
            }

            foreach(var nova in noveStope)
            {
                if(string.IsNullOrWhiteSpace (nova.Oznaka))
                {
                    continue;
                }

                var postojeca =
                    postojeceStope.FirstOrDefault (
                        x =>
                            !string.IsNullOrWhiteSpace (x.Oznaka) &&
                            string.Equals (
                                x.Oznaka,
                                nova.Oznaka,
                                StringComparison.Ordinal));

                if(postojeca != null)
                {
                    // Postoji ista fiskalna oznaka.
                    // Zadržavamo IdStope.
                    postojeca.Postotak =
                        nova.Postotak;

                    postojeca.Opis =
                        nova.Opis;

                    postojeca.Oznaka =
                        nova.Oznaka;

                    postojeca.Aktivna =
                        true;

                    Debug.WriteLine (
                        $"[SRBIJA] Ažurirana stopa: " +
                        $"ID={postojeca.IdStope}, " +
                        $"Oznaka={postojeca.Oznaka}, " +
                        $"Postotak={postojeca.Postotak}");
                }
                else
                {
                    // Nova oznaka koju ranije nismo imali.
                    var novaStopa =
                        new TblPoreskeStope
                        {
                            Postotak =
                                nova.Postotak,

                            Opis =
                                nova.Opis,

                            Oznaka =
                                nova.Oznaka,

                            Aktivna =
                                true
                        };

                    db.PoreskeStope.Add (
                        novaStopa);

                    Debug.WriteLine (
                        $"[SRBIJA] Dodana nova stopa: " +
                        $"Oznaka={nova.Oznaka}, " +
                        $"Postotak={nova.Postotak}");
                }
            }

            await db.SaveChangesAsync ();

            Debug.WriteLine (
                $"[SRBIJA] Sinhronizacija poreskih stopa završena. " +
                $"Aktivnih sa VPFR-a: {noveStope.Count}");
        }


        #region Load settings

        private void LoadSettingsFromProperties()
        {
            try
            {
                // =================================================
                // DRŽAVA
                // =================================================

                string savedCountry =
                    Properties.Settings.Default.Country;

                if(!string.IsNullOrWhiteSpace (savedCountry) &&
                   Enum.TryParse (
                       savedCountry,
                       out Drzava savedDrzava))
                {
                    OdabranaDrzava = savedDrzava;
                }
                else
                {
                    OdabranaDrzava =
                        Drzava.FederacijaBiH;
                }


                // =================================================
                // FIRMA
                // =================================================

                Firma =
                    Properties.Settings.Default.Firma ?? string.Empty;

                Adresa =
                    Properties.Settings.Default.Adresa ?? string.Empty;

                Mjesto =
                    Properties.Settings.Default.Mjesto ?? string.Empty;

                JIB =
                    Properties.Settings.Default.JIB ?? string.Empty;

                PDV =
                    Properties.Settings.Default.PDV ?? string.Empty;

                ZR =
                    Properties.Settings.Default.ZR ?? string.Empty;

                Email =
                    Properties.Settings.Default.Email ?? string.Empty;

                PDVKorisnik =
                    StringToYesNoIndex (
                        Properties.Settings.Default.PDVKorisnik);


                // =================================================
                // FEDERACIJA BiH
                // =================================================

                TringServerIpAddress =
                    Properties.Settings.Default
                        .TringServerIpAddress
                    ?? string.Empty;


                // =================================================
                // REPUBLIKA SRPSKA
                // =================================================

                LPFRIP =
                    Properties.Settings.Default.LPFR_IP
                    ?? string.Empty;

                LPFRKey =
                    Properties.Settings.Default.LPFR_Key
                    ?? string.Empty;

                LPFRPin =
                    Properties.Settings.Default.LPFR_Pin
                    ?? string.Empty;

                ExterniPrinter =
                    StringToYesNoIndex (
                        Properties.Settings.Default.ExterniPrinter);

                SirinaTrake =
                    string.IsNullOrWhiteSpace (
                        Properties.Settings.Default.SirinaTrake)
                    ? "80"
                    : Properties.Settings.Default.SirinaTrake;


                // =================================================
                // HRVATSKA
                // =================================================

                DemoServerUrl =
                    Properties.Settings.Default.DemoServerUrl
                    ?? string.Empty;

                ProductionServerUrl =
                    Properties.Settings.Default.ProductionSeverUrl
                    ?? string.Empty;

                PoslovniProstor =
                    Properties.Settings.Default.PoslovniProstor
                    ?? string.Empty;

                NaplatniUredjaj =
                    Properties.Settings.Default.NaplatniUredjaj
                    ?? string.Empty;

                OznakaSlijednosti =
                    OznakaSlijednostiToIndex (
                        Properties.Settings.Default
                            .OznakaSlijednosti);

                VerzijaAplikacije =
                    VerzijaAplikacijeToIndex (
                        Properties.Settings.Default
                            .VerzijaAplikacije);

                CertificateName =
                    Properties.Settings.Default.CerificateName
                    ?? string.Empty;

                CertificatePassword =
                    Properties.Settings.Default.CerificatePassword
                    ?? string.Empty;

                PnpStopa =
                    Properties.Settings.Default.PnpStopa
                    ?? string.Empty;


                // =================================================
                // SRBIJA - OPŠTE
                // =================================================

                SrbijaPfrType =
                    string.IsNullOrWhiteSpace (
                        Properties.Settings.Default.SrbijaPfrType)
                    ? "VPFR"
                    : Properties.Settings.Default.SrbijaPfrType;

                SrbijaEnvironment =
                    string.IsNullOrWhiteSpace (
                        Properties.Settings.Default.SrbijaEnvironment)
                    ? "Sandbox"
                    : Properties.Settings.Default.SrbijaEnvironment;


                // =================================================
                // SRBIJA - VPFR
                // =================================================

                SrbijaVPFRUrl =
                    string.IsNullOrWhiteSpace (
                        Properties.Settings.Default.SrbijaVPFRUrl)
                    ? "https://vsdc.sandbox.suf.purs.gov.rs"
                    : Properties.Settings.Default.SrbijaVPFRUrl;

                SrbijaCertificateName =
                    Properties.Settings.Default
                        .SrbijaCertificateName
                    ?? string.Empty;

                SrbijaCertificatePassword =
                    Properties.Settings.Default
                        .SrbijaCertificatePassword
                    ?? string.Empty;

                SrbijaPAC =
                    Properties.Settings.Default.SrbijaPAC
                    ?? string.Empty;

                SrbijaAcceptLanguage =
                    string.IsNullOrWhiteSpace (
                        Properties.Settings.Default
                            .SrbijaAcceptLanguage)
                    ? "en-US"
                    : Properties.Settings.Default
                        .SrbijaAcceptLanguage;


                // =================================================
                // SRBIJA - LPFR
                // =================================================

                SrbijaLPFRToken =
                    Properties.Settings.Default.SrbijaLPFRToken
                    ?? string.Empty;

                SrbijaLPFRUrl =
                    Properties.Settings.Default.SrbijaLPFRUrl
                    ?? string.Empty;

                SrbijaLPFRPin =
                    Properties.Settings.Default.SrbijaLPFRPin
                    ?? string.Empty;

                SrbijaLPFRJid =
                    Properties.Settings.Default.SrbijaLPFRJid
                    ?? string.Empty;


                // Status se ne sprema
                SrbijaConnectionStatus = string.Empty;


                // =================================================
                // APLIKACIJA
                // =================================================

                DbPath =
                    string.IsNullOrWhiteSpace (
                        Properties.Settings.Default.DbPath)
                    ? @"C:\DsoftData\sysFormWPF.db"
                    : Properties.Settings.Default.DbPath;

                BackupUrl =
                    Properties.Settings.Default.BackupUrl
                    ?? string.Empty;

                LogoUrl =
                    Properties.Settings.Default.LogoUrl
                    ?? string.Empty;

                POSPrinter =
                    Properties.Settings.Default.POSPrinter
                    ?? string.Empty;

                A4Printer =
                    Properties.Settings.Default.A4Printer
                    ?? string.Empty;

                KuhinjaPrinter =
                    Properties.Settings.Default.KuhinjaPrinter
                    ?? string.Empty;

                SankPrinter =
                    Properties.Settings.Default.SankPrinter
                    ?? string.Empty;

                BrojKopijaBloka =
                    string.IsNullOrWhiteSpace (
                        Properties.Settings.Default.BlokKopija)
                    ? "1"
                    : Properties.Settings.Default.BlokKopija;

                ProdajaMinus =
                    StringToYesNoIndex (
                        Properties.Settings.Default.ProdajaMinus);

                MultiUser =
                    StringToYesNoIndex (
                        Properties.Settings.Default.MultiUser);

                Tema =
                    TemaToIndex (
                        Properties.Settings.Default.Tema);

                ServerIP =
                    Properties.Settings.Default.ServerIP
                    ?? string.Empty;


                // =================================================
                // MONITOR
                // =================================================

                if(!int.TryParse (
                    Properties.Settings.Default.DisplayKuhinja,
                    out int monitorIndex))
                {
                    monitorIndex = 0;
                }

                DisplayKuhinja = monitorIndex;

                SelectedMonitor =
                    Monitors.FirstOrDefault (
                        x => x.Index == monitorIndex)
                    ?? Monitors.FirstOrDefault ();
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[Settings] Greška pri učitavanju: {ex}");

                ShowError (
                    "Greška pri učitavanju postavki:\n\n" +
                    ex.Message);
            }
        }

        #endregion


        #region Save settings

        private async Task SaveSettingsAsync()
        {
            if(!ValidateSettings ())
                return;

            try
            {
                // =================================================
                // DRŽAVA
                // =================================================

                Properties.Settings.Default.Country =
                    OdabranaDrzava.ToString ();


                // =================================================
                // FIRMA
                // =================================================

                Properties.Settings.Default.Firma =
                    Firma?.Trim () ?? string.Empty;

                Properties.Settings.Default.Adresa =
                    Adresa?.Trim () ?? string.Empty;

                Properties.Settings.Default.Mjesto =
                    Mjesto?.Trim () ?? string.Empty;

                Properties.Settings.Default.JIB =
                    JIB?.Trim () ?? string.Empty;

                Properties.Settings.Default.PDV =
                    PDV?.Trim () ?? string.Empty;

                Properties.Settings.Default.ZR =
                    ZR?.Trim () ?? string.Empty;

                Properties.Settings.Default.Email =
                    Email?.Trim () ?? string.Empty;

                Properties.Settings.Default.PDVKorisnik =
                    YesNoIndexToString (PDVKorisnik);


                // =================================================
                // FEDERACIJA BiH
                // =================================================

                Properties.Settings.Default.TringServerIpAddress =
                    TringServerIpAddress?.Trim ()
                    ?? string.Empty;


                // =================================================
                // REPUBLIKA SRPSKA
                // =================================================

                Properties.Settings.Default.LPFR_IP =
                    LPFRIP?.Trim () ?? string.Empty;

                Properties.Settings.Default.LPFR_Key =
                    LPFRKey?.Trim () ?? string.Empty;

                Properties.Settings.Default.LPFR_Pin =
                    LPFRPin?.Trim () ?? string.Empty;

                Properties.Settings.Default.ExterniPrinter =
                    YesNoIndexToString (ExterniPrinter);

                Properties.Settings.Default.SirinaTrake =
                    SirinaTrake ?? "80";


                // =================================================
                // HRVATSKA
                // =================================================

                Properties.Settings.Default.DemoServerUrl =
                    DemoServerUrl?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.ProductionSeverUrl =
                    ProductionServerUrl?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.PoslovniProstor =
                    PoslovniProstor?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.NaplatniUredjaj =
                    NaplatniUredjaj?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.OznakaSlijednosti =
                    OznakaSlijednosti == 0
                        ? "Naplatni uređaj"
                        : "Poslovni prostor";

                Properties.Settings.Default.VerzijaAplikacije =
                    VerzijaAplikacije == 0
                        ? "Demo"
                        : "Produkcijska";

                Properties.Settings.Default.CerificateName =
                    CertificateName?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.CerificatePassword =
                    CertificatePassword
                    ?? string.Empty;

                Properties.Settings.Default.PnpStopa =
                    PnpStopa?.Trim ()
                    ?? string.Empty;


                // =================================================
                // SRBIJA - OPŠTE
                // =================================================

                Properties.Settings.Default.SrbijaPfrType =
                    SrbijaPfrType?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.SrbijaEnvironment =
                    SrbijaEnvironment?.Trim ()
                    ?? string.Empty;


                // =================================================
                // SRBIJA - VPFR
                // =================================================

                Properties.Settings.Default.SrbijaVPFRUrl =
                    SrbijaVPFRUrl?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.SrbijaCertificateName =
                    SrbijaCertificateName?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default
                    .SrbijaCertificatePassword =
                    SrbijaCertificatePassword
                    ?? string.Empty;

                Properties.Settings.Default.SrbijaPAC =
                    SrbijaPAC?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.SrbijaAcceptLanguage =
                    SrbijaAcceptLanguage?.Trim ()
                    ?? string.Empty;


                // =================================================
                // SRBIJA - LPFR
                // =================================================

                Properties.Settings.Default.SrbijaLPFRToken =
                    SrbijaLPFRToken?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.SrbijaLPFRUrl =
                    SrbijaLPFRUrl?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.SrbijaLPFRPin =
                    SrbijaLPFRPin?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.SrbijaLPFRJid =
                    SrbijaLPFRJid?.Trim ()
                    ?? string.Empty;


                // =================================================
                // APLIKACIJA
                // =================================================

              //  Properties.Settings.Default.DbPath =
              //      DbPath?.Trim ()
              //      ?? string.Empty;

                Properties.Settings.Default.BackupUrl =
                    BackupUrl?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.LogoUrl =
                    LogoUrl?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.POSPrinter =
                    POSPrinter?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.A4Printer =
                    A4Printer?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.KuhinjaPrinter =
                    KuhinjaPrinter?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.SankPrinter =
                    SankPrinter?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.BlokKopija =
                    BrojKopijaBloka ?? "1";

                Properties.Settings.Default.ProdajaMinus =
                    YesNoIndexToString (ProdajaMinus);

                Properties.Settings.Default.MultiUser =
                    YesNoIndexToString (MultiUser);

                Properties.Settings.Default.Tema =
                    Tema == 0
                        ? "Tamna"
                        : "Svijetla";

                Properties.Settings.Default.ServerIP =
                    ServerIP?.Trim ()
                    ?? string.Empty;

                Properties.Settings.Default.DisplayKuhinja =
                    (SelectedMonitor?.Index ?? 0)
                    .ToString ();


                // =================================================
                // SPREMANJE
                // =================================================

                Properties.Settings.Default.Save ();

                Globals.CurrentDbPath =
                    DbPath?.Trim () ?? string.Empty;


                ShowInfo (
                    "Podešavanja su spremljena!");


                var page = new HomePage
                {
                    DataContext = new HomeViewModel ()
                };

                PageNavigator.NavigateWithFade (page);

                await Task.CompletedTask;
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[Settings] Greška pri spremanju: {ex}");

                ShowError (
                    "Greška pri spremanju podešavanja:\n\n" +
                    ex.Message);
            }
        }

        #endregion


        #region Validacija

        private bool ValidateSettings()
        {
            // =================================================
            // FIRMA
            // =================================================

            if(string.IsNullOrWhiteSpace (Firma))
            {
                ShowError ("Unesite naziv firme.");
                return false;
            }

            if(string.IsNullOrWhiteSpace (Adresa))
            {
                ShowError ("Unesite adresu firme.");
                return false;
            }

            if(string.IsNullOrWhiteSpace (Mjesto))
            {
                ShowError ("Unesite mjesto.");
                return false;
            }

            if(string.IsNullOrWhiteSpace (JIB))
            {
                ShowError ("Unesite JIB/OIB firme.");
                return false;
            }

            if(string.IsNullOrWhiteSpace (Email))
            {
                ShowError ("Unesite email firme.");
                return false;
            }


            if(PDVKorisnik == 1 &&
               string.IsNullOrWhiteSpace (PDV))
            {
                ShowError (
                    "Unesite PDV broj.");

                return false;
            }


            // =================================================
            // FEDERACIJA BiH
            // =================================================

            if(OdabranaDrzava == Drzava.FederacijaBiH)
            {
                if(string.IsNullOrWhiteSpace (
                    TringServerIpAddress))
                {
                    ShowError (
                        "Unesite IP adresu Tring fiskalnog servera.");

                    return false;
                }
            }


            // =================================================
            // REPUBLIKA SRPSKA
            // =================================================

            if(OdabranaDrzava == Drzava.RepublikaSrpska)
            {
                if(string.IsNullOrWhiteSpace (LPFRIP))
                {
                    ShowError (
                        "Unesite LPFR IP adresu.");

                    return false;
                }

                if(string.IsNullOrWhiteSpace (LPFRKey))
                {
                    ShowError (
                        "Unesite LPFR API ključ.");

                    return false;
                }

                if(string.IsNullOrWhiteSpace (LPFRPin))
                {
                    ShowError (
                        "Unesite LPFR PIN.");

                    return false;
                }
            }


            // =================================================
            // HRVATSKA
            // =================================================

            if(OdabranaDrzava == Drzava.Hrvatska)
            {
                if(string.IsNullOrWhiteSpace (
                    PoslovniProstor))
                {
                    ShowError (
                        "Unesite oznaku poslovnog prostora.");

                    return false;
                }

                if(string.IsNullOrWhiteSpace (
                    NaplatniUredjaj))
                {
                    ShowError (
                        "Unesite oznaku naplatnog uređaja.");

                    return false;
                }

                if(string.IsNullOrWhiteSpace (
                    CertificateName))
                {
                    ShowError (
                        "Odaberite certifikat za fiskalizaciju.");

                    return false;
                }

                if(string.IsNullOrWhiteSpace (
                    CertificatePassword))
                {
                    ShowError (
                        "Unesite zaporku certifikata.");

                    return false;
                }
            }


            // =================================================
            // SRBIJA
            // =================================================

            if(OdabranaDrzava == Drzava.Srbija)
            {
                if(string.IsNullOrWhiteSpace (
                    SrbijaPfrType))
                {
                    ShowError (
                        "Odaberite tip PFR-a.");

                    return false;
                }

                if(string.IsNullOrWhiteSpace (
                    SrbijaEnvironment))
                {
                    ShowError (
                        "Odaberite PFR okruženje.");

                    return false;
                }


                // =================================================
                // SRBIJA - VPFR
                // =================================================

                if(string.Equals (
                    SrbijaPfrType,
                    "VPFR",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if(string.IsNullOrWhiteSpace (
                        SrbijaVPFRUrl))
                    {
                        ShowError (
                            "Unesite V-PFR URL.");

                        return false;
                    }

                    if(string.IsNullOrWhiteSpace (
                        SrbijaCertificateName))
                    {
                        ShowError (
                            "Odaberite V-PFR certifikat.");

                        return false;
                    }

                    if(string.IsNullOrWhiteSpace (
                        SrbijaCertificatePassword))
                    {
                        ShowError (
                            "Unesite zaporku V-PFR certifikata.");

                        return false;
                    }

                    if(string.IsNullOrWhiteSpace (
                        SrbijaPAC))
                    {
                        ShowError (
                            "Unesite PAC za V-PFR.");

                        return false;
                    }

                    if(string.IsNullOrWhiteSpace (
                        SrbijaAcceptLanguage))
                    {
                        ShowError (
                            "Odaberite jezik komunikacije.");

                        return false;
                    }
                }


                // =================================================
                // SRBIJA - LPFR
                // =================================================

                else if(string.Equals (
                    SrbijaPfrType,
                    "LPFR",
                    StringComparison.OrdinalIgnoreCase))
                {
                    if(string.IsNullOrWhiteSpace (
                        SrbijaLPFRToken))
                    {
                        ShowError (
                            "Unesite L-PFR token.");

                        return false;
                    }

                    if(string.IsNullOrWhiteSpace (
                        SrbijaLPFRUrl))
                    {
                        ShowError (
                            "Unesite L-PFR URL.");

                        return false;
                    }

                    if(string.IsNullOrWhiteSpace (
                        SrbijaLPFRPin))
                    {
                        ShowError (
                            "Unesite L-PFR PIN.");

                        return false;
                    }

                    if(string.IsNullOrWhiteSpace (
                        SrbijaLPFRJid))
                    {
                        ShowError (
                            "Unesite L-PFR JID.");

                        return false;
                    }
                }
                else
                {
                    ShowError (
                        "Nepoznat tip PFR-a.");

                    return false;
                }
            }


            // =================================================
            // APLIKACIJA
            // =================================================

            if(string.IsNullOrWhiteSpace (POSPrinter))
            {
                ShowError (
                    "Printer za račune je obavezan podatak.\n\n" +
                    "Odaberite printer za račune prije spremanja postavki.");

                return false;
            }

            if(string.IsNullOrWhiteSpace (BackupUrl))
            {
                ShowError (
                    "Odaberite direktorij za backup.");

                return false;
            }

            if(!Directory.Exists (BackupUrl))
            {
                ShowError (
                    "Direktorij za backup ne postoji.\n\n" +
                    BackupUrl);

                return false;
            }

            if(string.IsNullOrWhiteSpace (DbPath))
            {
                ShowError (
                    "Odaberite bazu podataka.");

                return false;
            }

            return true;
        }

        #endregion


        #region Datoteke

        private void SelectDatabase()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Odaberite Caupo bazu podataka",
                    Filter =
                        "SQLite baza (*.db)|*.db|" +
                        "Sve datoteke (*.*)|*.*",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if(!string.IsNullOrWhiteSpace (DbPath))
                {
                    try
                    {
                        string? directory =
                            Path.GetDirectoryName (DbPath);

                        if(!string.IsNullOrWhiteSpace (directory) &&
                           Directory.Exists (directory))
                        {
                            dialog.InitialDirectory =
                                directory;
                        }
                    }
                    catch
                    {
                    }
                }
                else if(Directory.Exists (@"C:\DsoftData"))
                {
                    dialog.InitialDirectory =
                        @"C:\DsoftData";
                }

                if(dialog.ShowDialog () == true)
                {
                    DbPath =
                        dialog.FileName;
                }
            }
            catch(Exception ex)
            {
                ShowError (
                    "Greška pri izboru baze:\n\n" +
                    ex.Message);
            }
        }


        private void SelectLogo()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Odaberite logo",
                    Filter =
                        "Slike (*.png;*.jpg;*.jpeg;*.bmp)|" +
                        "*.png;*.jpg;*.jpeg;*.bmp|" +
                        "Sve datoteke (*.*)|*.*"
                };

                if(dialog.ShowDialog () == true)
                {
                    LogoUrl =
                        dialog.FileName;
                }
            }
            catch(Exception ex)
            {
                ShowError (
                    "Greška pri izboru logoa:\n\n" +
                    ex.Message);
            }
        }


        private void SelectBackup()
        {
            try
            {
                var dialog =
                    new Microsoft.Win32.OpenFolderDialog
                    {
                        Title =
                            "Odaberite direktorij za backup"
                    };

                if(!string.IsNullOrWhiteSpace (BackupUrl) &&
                   Directory.Exists (BackupUrl))
                {
                    dialog.InitialDirectory =
                        BackupUrl;
                }

                if(dialog.ShowDialog () == true)
                {
                    BackupUrl =
                        dialog.FolderName;
                }
            }
            catch(Exception ex)
            {
                ShowError (
                    "Greška pri izboru backup direktorija:\n\n" +
                    ex.Message);
            }
        }

        #endregion


        #region Printeri

        private void SelectPrinter(
            string title,
            Action<string> setPrinter)
        {
            try
            {
                var printerDialog =
                    new PrinterDialog ();

                printerDialog.MessageTitle.Text =
                    title;

                if(printerDialog.ShowDialog () == true &&
                   !string.IsNullOrWhiteSpace (
                       printerDialog.SelectedPrinter))
                {
                    setPrinter (
                        printerDialog.SelectedPrinter);
                }
            }
            catch(Exception ex)
            {
                ShowError (
                    "Greška pri izboru printera:\n\n" +
                    ex.Message);
            }
        }

        #endregion


        #region Certifikati

        private void SelectCroatiaCertificate()
        {
            SelectAndCopyCertificate (
                certificate =>
                    CertificateName = certificate);
        }


        private void SelectSrbijaCertificate()
        {
            SelectAndCopyCertificate (
                certificate =>
                    SrbijaCertificateName = certificate);
        }


        private void SelectAndCopyCertificate(
            Action<string> setCertificate)
        {
            try
            {
                var dialog =
                    new OpenFileDialog
                    {
                        Title =
                            "Odaberite certifikat",

                        Filter =
                            "Certifikati|" +
                            "*.cer;*.crt;*.pfx;*.p12;*.pem;*.der|" +
                            "Sve datoteke (*.*)|*.*"
                    };

                if(dialog.ShowDialog () != true)
                    return;


                string certificatesFolder =
                    Path.Combine (
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Certificates");


                if(!Directory.Exists (certificatesFolder))
                {
                    Directory.CreateDirectory (
                        certificatesFolder);
                }


                string fileName =
                    Path.GetFileName (
                        dialog.FileName);

                string destination =
                    Path.Combine (
                        certificatesFolder,
                        fileName);


                if(!string.Equals (
                    Path.GetFullPath (dialog.FileName),
                    Path.GetFullPath (destination),
                    StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy (
                        dialog.FileName,
                        destination,
                        true);
                }


                setCertificate (fileName);
            }
            catch(Exception ex)
            {
                ShowError (
                    "Greška pri izboru certifikata:\n\n" +
                    ex.Message);
            }
        }

        #endregion


        #region Srbija test

        private async Task TestSrbijaLPFRAsync()
        {
            // =====================================================
            // PROVJERA PODATAKA
            // =====================================================

            if(string.IsNullOrWhiteSpace (SrbijaLPFRUrl))
            {
                SrbijaConnectionStatus =
                    "L-PFR URL nije unesen.";

                return;
            }

            if(string.IsNullOrWhiteSpace (SrbijaLPFRToken))
            {
                SrbijaConnectionStatus =
                    "L-PFR token nije unesen.";

                return;
            }

            if(string.IsNullOrWhiteSpace (SrbijaLPFRPin))
            {
                SrbijaConnectionStatus =
                    "L-PFR PIN nije unesen.";

                return;
            }

            if(string.IsNullOrWhiteSpace (SrbijaLPFRJid))
            {
                SrbijaConnectionStatus =
                    "L-PFR JID nije unesen.";

                return;
            }


            try
            {
                // =====================================================
                // BASE URL + TOKEN
                // =====================================================

                string baseUrl =
                    BuildSrbijaLPFRBaseUrl (
                        SrbijaLPFRUrl,
                        SrbijaLPFRToken);


                Debug.WriteLine (
                    $"[SRBIJA LPFR] Base URL: {baseUrl}");

                Debug.WriteLine (
                    $"[SRBIJA LPFR] JID: {SrbijaLPFRJid}");


                using var client =
                    new HttpClient
                    {
                        Timeout = TimeSpan.FromSeconds (20)
                    };


                client.DefaultRequestHeaders.Accept.Clear ();

                client.DefaultRequestHeaders.Accept.Add (
                    new MediaTypeWithQualityHeaderValue (
                        "application/json"));


                // =====================================================
                // 1. POTVRDA PIN-a
                // =====================================================

                string pinUrl = $"{baseUrl}/v3/pin";


                Debug.WriteLine (
                    $"[SRBIJA LPFR] PIN URL: {pinUrl}");


                /*
                 * TaxCore zahtijeva PIN kao PLAIN TEXT body,
                 * ali Content-Type mora biti application/json.
                 */

                using var pinContent =
                    new StringContent (
                        SrbijaLPFRPin.Trim (),
                        Encoding.UTF8,
                        "application/json");


                using HttpResponseMessage pinResponse =
                    await client.PostAsync (
                        pinUrl,
                        pinContent);


                string pinResult =
                    (await pinResponse.Content
                        .ReadAsStringAsync ())
                    .Trim ()
                    .Trim ('"');


                Debug.WriteLine (
                    $"[SRBIJA LPFR] PIN HTTP: {(int)pinResponse.StatusCode} {pinResponse.StatusCode}");

                Debug.WriteLine (
                    $"[SRBIJA LPFR] PIN response: {pinResult}");


                // =====================================================
                // PROVJERA PIN ODGOVORA
                // =====================================================

                if(!pinResponse.IsSuccessStatusCode)
                {
                    SrbijaConnectionStatus =
                        $"L-PFR PIN greška - HTTP {(int)pinResponse.StatusCode}: {pinResult}";

                    return;
                }


                if(!string.Equals (
                    pinResult,
                    "0100",
                    StringComparison.OrdinalIgnoreCase))
                {
                    SrbijaConnectionStatus =
                        $"L-PFR PIN nije prihvaćen: {pinResult}";

                    return;
                }


                // =====================================================
                // 2. STATUS
                // =====================================================

                string statusUrl =  $"{baseUrl}/v3/status";


                Debug.WriteLine (
                    $"[SRBIJA LPFR] Status URL: {statusUrl}");


                using HttpResponseMessage statusResponse =
                    await client.GetAsync (
                        statusUrl);


                string statusBody =
                    await statusResponse.Content
                        .ReadAsStringAsync ();


                Debug.WriteLine (
                    $"[SRBIJA LPFR] Status HTTP: {(int)statusResponse.StatusCode} {statusResponse.StatusCode}");

                Debug.WriteLine (
                    $"[SRBIJA LPFR] Status response: {statusBody}");


                if(!statusResponse.IsSuccessStatusCode)
                {
                    SrbijaConnectionStatus =
                        $"PIN OK, ali status greška - HTTP {(int)statusResponse.StatusCode}";

                    return;
                }


                // =====================================================
                // 3. PROVJERA JID-a
                // =====================================================

                string? returnedJid =
                    TryGetSrbijaLPFRJid (
                        statusBody);


                if(!string.IsNullOrWhiteSpace (returnedJid))
                {
                    Debug.WriteLine (
                        $"[SRBIJA LPFR] JID sa servera: {returnedJid}");


                    if(!string.Equals (
                        returnedJid.Trim (),
                        SrbijaLPFRJid.Trim (),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        SrbijaConnectionStatus =
                            $"L-PFR radi, ali JID nije isti. Server: {returnedJid}";

                        return;
                    }
                }


                // =====================================================
                // USPJEH
                // =====================================================

                SrbijaConnectionStatus =
                    "L-PFR komunikacija uspješna - PIN OK, status OK.";
            }
            catch(TaskCanceledException)
            {
                SrbijaConnectionStatus =
                    "L-PFR ne odgovara - istekao timeout.";
            }
            catch(HttpRequestException ex)
            {
                Debug.WriteLine (
                    $"[SRBIJA LPFR] HTTP greška: {ex}");

                SrbijaConnectionStatus =
                    "L-PFR HTTP greška: " +
                    ex.Message;
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[SRBIJA LPFR] Greška: {ex}");

                SrbijaConnectionStatus =
                    "L-PFR greška: " +
                    ex.Message;
            }
        }


        private static string BuildSrbijaLPFRBaseUrl(
            string url,
            string token)
        {
            string cleanUrl =
                url.Trim ()
                   .TrimEnd ('/');

            // Ako URL već završava sa /api,
            // znači da je kompletan sandbox LPFR URL
            // i token je već sadržan u njemu.
            if(cleanUrl.EndsWith (
                "/api",
                StringComparison.OrdinalIgnoreCase))
            {
                return cleanUrl;
            }

            string cleanToken =
                token?.Trim ()
                     .Trim ('/')
                ?? string.Empty;

            // Ako je token već dio URL-a,
            // samo dodaj /api ako nedostaje.
            if(!string.IsNullOrWhiteSpace (cleanToken) &&
               cleanUrl.Contains (
                   "/" + cleanToken,
                   StringComparison.OrdinalIgnoreCase))
            {
                return $"{cleanUrl}/api";
            }

            // Ako korisnik unese samo host,
            // onda sastavi kompletan URL.
            if(!string.IsNullOrWhiteSpace (cleanToken))
            {
                return $"{cleanUrl}/{cleanToken}/api";
            }

            return cleanUrl;
        }

        private static string? TryGetSrbijaLPFRJid(
    string json)
        {
            if(string.IsNullOrWhiteSpace (json))
                return null;

            try
            {
                using JsonDocument document =
                    JsonDocument.Parse (json);

                JsonElement root =
                    document.RootElement;


                // TaxCore status koristi uid.
                if(root.TryGetProperty (
                    "uid",
                    out JsonElement uidElement))
                {
                    return uidElement.GetString ();
                }


                // Ostavljamo podršku i ako neka verzija
                // vrati jid naziv.
                if(root.TryGetProperty (
                    "jid",
                    out JsonElement jidElement))
                {
                    return jidElement.GetString ();
                }


                return null;
            }
            catch(JsonException)
            {
                return null;
            }
        }
        private async Task TestSrbijaPfrAsync()
        {
            SrbijaConnectionStatus =
                "Provjera komunikacije...";

            try
            {
                if(string.Equals (
                    SrbijaPfrType,
                    "VPFR",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await TestSrbijaVPFRAsync ();
                }
                else if(string.Equals (
                    SrbijaPfrType,
                    "LPFR",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await TestSrbijaLPFRAsync ();
                }
                else
                {
                    SrbijaConnectionStatus =
                        "Nepoznat PFR tip.";
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[SRBIJA] PFR test error: {ex}");

                SrbijaConnectionStatus =
                    "Greška: " + ex.Message;
            }
        }


        private async Task TestSrbijaVPFRAsync()
        {
            if(string.IsNullOrWhiteSpace (SrbijaVPFRUrl))
            {
                SrbijaConnectionStatus =
                    "V-PFR URL nije unesen.";
                return;
            }

            if(string.IsNullOrWhiteSpace (SrbijaCertificateName))
            {
                SrbijaConnectionStatus =
                    "V-PFR certifikat nije odabran.";
                return;
            }

            if(string.IsNullOrWhiteSpace (SrbijaCertificatePassword))
            {
                SrbijaConnectionStatus =
                    "Lozinka V-PFR certifikata nije unesena.";
                return;
            }

            if(string.IsNullOrWhiteSpace (SrbijaPAC))
            {
                SrbijaConnectionStatus =
                    "PAC nije unesen.";
                return;
            }

            string certificatePath =
                Path.Combine (
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Certificates",
                    SrbijaCertificateName);

            if(!File.Exists (certificatePath))
            {
                SrbijaConnectionStatus =
                    $"Certifikat nije pronađen: {certificatePath}";
                return;
            }

            try
            {
                // =====================================================
                // CERTIFIKAT
                // =====================================================

                var certificate =
                    new X509Certificate2 (
                        certificatePath,
                        SrbijaCertificatePassword,
                        X509KeyStorageFlags.MachineKeySet |
                        X509KeyStorageFlags.Exportable);

                Debug.WriteLine (
                    $"[SRBIJA VPFR] Certifikat: {certificate.Subject}");

                Debug.WriteLine (
                    $"[SRBIJA VPFR] Thumbprint: {certificate.Thumbprint}");

                Debug.WriteLine (
                    $"[SRBIJA VPFR] HasPrivateKey: {certificate.HasPrivateKey}");

                Debug.WriteLine (
                    $"[SRBIJA VPFR] Valid from: {certificate.NotBefore}");

                Debug.WriteLine (
                    $"[SRBIJA VPFR] Valid to: {certificate.NotAfter}");

                if(!certificate.HasPrivateKey)
                {
                    SrbijaConnectionStatus =
                        "V-PFR certifikat nema privatni ključ.";
                    return;
                }


                // =====================================================
                // HTTP HANDLER
                // =====================================================

                using var handler =
                    new HttpClientHandler ();

                handler.ClientCertificateOptions =
                    ClientCertificateOption.Manual;

                handler.ClientCertificates.Add (
                    certificate);

                // Bitno za V-PFR:
                // ne koristiti sistemski HTTP proxy.
                handler.UseProxy = false;


                // =====================================================
                // HTTP CLIENT
                // =====================================================

                using var client =
                    new HttpClient (handler)
                    {
                        Timeout =
                            TimeSpan.FromSeconds (30)
                    };

                client.DefaultRequestHeaders.Accept.Clear ();

                client.DefaultRequestHeaders.Accept.Add (
                    new MediaTypeWithQualityHeaderValue (
                        "application/json"));

                client.DefaultRequestHeaders.Remove ("PAC");

                client.DefaultRequestHeaders.Add (
                    "PAC",
                    SrbijaPAC.Trim ());


                if(!string.IsNullOrWhiteSpace (
                    SrbijaAcceptLanguage))
                {
                    client.DefaultRequestHeaders
                        .AcceptLanguage.Clear ();

                    client.DefaultRequestHeaders
                        .AcceptLanguage.Add (
                            new StringWithQualityHeaderValue (
                                SrbijaAcceptLanguage));
                }


                // =====================================================
                // STATUS URL
                // =====================================================

                string statusUrl =
                    SrbijaVPFRUrl
                        .Trim ()
                        .TrimEnd ('/');

                if(!statusUrl.EndsWith (
                    "/api/v3/status",
                    StringComparison.OrdinalIgnoreCase))
                {
                    statusUrl +=
                        "/api/v3/status";
                }

                Debug.WriteLine (
                    $"[SRBIJA VPFR] Status URL: {statusUrl}");


                // =====================================================
                // REQUEST
                // =====================================================

                using HttpResponseMessage response =
                    await client.GetAsync (statusUrl);

                string body =
                    await response.Content
                        .ReadAsStringAsync ();

                Debug.WriteLine (
                    $"[SRBIJA VPFR] Status HTTP: {(int)response.StatusCode} {response.StatusCode}");

                Debug.WriteLine (
                    $"[SRBIJA VPFR] Status response: {body}");


                // =====================================================
                // REZULTAT
                // =====================================================

                if(response.IsSuccessStatusCode)
                {
                    string? uid =
                        TryGetSrbijaLPFRJid (body);

                    if(!string.IsNullOrWhiteSpace (uid))
                    {
                        SrbijaConnectionStatus =
                            $"V-PFR komunikacija uspješna | JID: {uid}";
                    }
                    else
                    {
                        SrbijaConnectionStatus =
                            "V-PFR komunikacija uspješna.";
                    }
                }
                else
                {
                    SrbijaConnectionStatus =
                        $"V-PFR greška - HTTP {(int)response.StatusCode}: {body}";
                }
            }
            catch(HttpRequestException ex)
            {
                Debug.WriteLine (
                    $"[SRBIJA VPFR] HttpRequestException: {ex}");

                if(ex.InnerException != null)
                {
                    Debug.WriteLine (
                        $"[SRBIJA VPFR] InnerException: {ex.InnerException}");

                    if(ex.InnerException.InnerException != null)
                    {
                        Debug.WriteLine (
                            $"[SRBIJA VPFR] InnerInnerException: {ex.InnerException.InnerException}");
                    }
                }

                SrbijaConnectionStatus =
                    "V-PFR HTTP greška: " +
                    (ex.InnerException?.Message ?? ex.Message);
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[SRBIJA VPFR] Greška: {ex}");

                SrbijaConnectionStatus =
                    "V-PFR greška: " +
                    ex.Message;
            }
        }





        private static string BuildSrbijaStatusUrl(
            string baseUrl)
        {
            string url =
                baseUrl.Trim ()
                    .TrimEnd ('/');

            if(url.EndsWith (
                "/api/v3/status",
                StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            return url +
                   "/api/v3/status";
        }

        #endregion


        #region Radnici

        public async Task LoadRadniciAsync()
        {
            try
            {
                using var db =
                    new AppDbContext ();

                var lista =
                    await db.Radnici
                        .AsNoTracking ()
                        .OrderBy (x => x.IdRadnika)
                        .ToListAsync ();

                Radnici =
                    new ObservableCollection<TblRadnici> (
                        lista);
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[Settings] LoadRadniciAsync: {ex}");

                ShowError (
                    "Greška pri učitavanju radnika:\n\n" +
                    ex.Message);
            }
        }


        public async Task NewRadnik(
            TblRadnici radnik)
        {
            try
            {
                using var db =
                    new AppDbContext ();

                db.Radnici.Add (radnik);

                await db.SaveChangesAsync ();

                await LoadRadniciAsync ();
            }
            catch(Exception ex)
            {
                ShowError (
                    "Greška pri dodavanju radnika:\n\n" +
                    ex.Message);
            }
        }


        public async Task UpdateRadnik(
            TblRadnici radnik)
        {
            try
            {
                using var db =
                    new AppDbContext ();

                var existing =
                    await db.Radnici
                        .FirstOrDefaultAsync (
                            x =>
                                x.IdRadnika ==
                                radnik.IdRadnika);

                if(existing == null)
                {
                    ShowError (
                        "Radnik nije pronađen.");

                    return;
                }


                existing.Radnik =
                    radnik.Radnik;

                existing.Lozinka =
                    radnik.Lozinka;

                existing.Dozvole =
                    radnik.Dozvole;


                await db.SaveChangesAsync ();

                await LoadRadniciAsync ();
            }
            catch(Exception ex)
            {
                ShowError (
                    "Greška pri izmjeni radnika:\n\n" +
                    ex.Message);
            }
        }


        private void OpenNewRadnik()
        {
            try
            {
                var popup =
                    new NewWorkerPopup (this);

                popup.isUpdate = false;

                popup.DataContext =
                    this;

                popup.ShowDialog ();
            }
            catch(Exception ex)
            {
                ShowError (
                    "Greška pri otvaranju novog radnika:\n\n" +
                    ex.Message);
            }
        }


        private async Task OpenUpdateRadnik(
            TblRadnici? radnik)
        {
            if(radnik == null)
            {
                ShowError (
                    "Odaberite radnika kojeg želite izmijeniti.");

                return;
            }


            try
            {
                var popup =
                    new NewWorkerPopup (this);

                popup.isUpdate = true;

                popup.DataContext =
                    this;


                popup.PopupTitle.Text =
                    "Izmijeni radnika";


                popup.lblid.Content =
                    radnik.IdRadnika.ToString ();

                popup.txtRadnik.Text =
                    radnik.Radnik ?? string.Empty;

                popup.txtLozinka.Text =
                    radnik.Lozinka ?? string.Empty;


                foreach(var item in popup.cmbDozvole.Items)
                {
                    if(item is ComboBoxItem comboItem &&
                       string.Equals (
                           comboItem.Content?.ToString (),
                           radnik.Dozvole,
                           StringComparison.OrdinalIgnoreCase))
                    {
                        popup.cmbDozvole.SelectedItem =
                            comboItem;

                        break;
                    }
                }


                popup.ShowDialog ();

                await Task.CompletedTask;
            }
            catch(Exception ex)
            {
                ShowError (
                    "Greška pri izmjeni radnika:\n\n" +
                    ex.Message);
            }
        }


        private async Task DeleteRadnik(
            TblRadnici? radnik)
        {
            if(radnik == null)
            {
                ShowError (
                    "Odaberite radnika kojeg želite obrisati.");

                return;
            }


            try
            {
                var popup =
                    new YesNoPopup ();

                popup.MessageTitle.Text =
                    "Brisanje radnika";

                popup.MessageText.Text =
                    $"Želite li obrisati radnika:\n\n{radnik.Radnik}?";


                popup.ShowDialog ();


                if(popup.Kliknuo != "Da")
                    return;


                using var db =
                    new AppDbContext ();


                var existing =
                    await db.Radnici
                        .FirstOrDefaultAsync (
                            x =>
                                x.IdRadnika ==
                                radnik.IdRadnika);

                if(existing == null)
                    return;


                db.Radnici.Remove (existing);

                await db.SaveChangesAsync ();

                await LoadRadniciAsync ();
            }
            catch(Exception ex)
            {
                ShowError (
                    "Greška pri brisanju radnika:\n\n" +
                    ex.Message);
            }
        }

        #endregion


        #region Monitori helper

        private static List<MonitorInfo> GetAllMonitors()
        {
            var monitors =
                new List<MonitorInfo> ();

            int index = 0;


            EnumDisplayMonitors (
                IntPtr.Zero,
                IntPtr.Zero,

                delegate (
                    IntPtr hMonitor,
                    IntPtr hdcMonitor,
                    IntPtr lprcMonitor,
                    IntPtr dwData)
                {
                    var monitorInfo =
                        new MONITORINFOEX ();

                    monitorInfo.cbSize =
                        Marshal.SizeOf (
                            typeof (MONITORINFOEX));


                    if(GetMonitorInfo (
                        hMonitor,
                        ref monitorInfo))
                    {
                        var displayDevice =
                            new DISPLAY_DEVICE ();

                        displayDevice.cb =
                            Marshal.SizeOf (
                                typeof (DISPLAY_DEVICE));


                        string displayName =
                            monitorInfo.szDevice;


                        if(EnumDisplayDevices (
                            monitorInfo.szDevice,
                            0,
                            ref displayDevice,
                            0))
                        {
                            if(!string.IsNullOrWhiteSpace (
                                displayDevice.DeviceString))
                            {
                                displayName =
                                    displayDevice.DeviceString;
                            }
                        }


                        monitors.Add (
                            new MonitorInfo
                            {
                                Index = index,

                                Name =
                                    $"Monitor {index + 1} - {displayName}",

                                DeviceName =
                                    monitorInfo.szDevice,

                                IsPrimary =
                                    (monitorInfo.dwFlags & 1) != 0
                            });


                        index++;
                    }

                    return true;
                },

                IntPtr.Zero);


            return monitors;
        }

        #endregion


        #region Tema

        private void ApplySelectedTheme()
        {
            try
            {
                string temaName =
                    Tema == 0
                        ? "Tamna"
                        : "Svijetla";


                App.CurrentTheme =
                    temaName;

                App.ApplyTheme (
                    temaName);


                if(Application.Current == null)
                    return;


                if(Tema == 0)
                {
                    Application.Current.Resources[
                        "GlobalFontColor"] =
                        new SolidColorBrush (
                            Color.FromRgb (
                                250,
                                250,
                                250));

                    Application.Current.Resources[
                        "GlobalBackgroundColor"] =
                        new SolidColorBrush (
                            Color.FromRgb (
                                50,
                                50,
                                50));
                }
                else
                {
                    Application.Current.Resources[
                        "GlobalFontColor"] =
                        new SolidColorBrush (
                            Color.FromRgb (
                                32,
                                33,
                                36));

                    Application.Current.Resources[
                        "GlobalBackgroundColor"] =
                        new SolidColorBrush (
                            Color.FromRgb (
                                250,
                                250,
                                250));
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (
                    $"[Settings] ApplySelectedTheme: {ex}");
            }
        }

        #endregion


        #region Helpers

        private static int StringToYesNoIndex(
            string? value)
        {
            return string.Equals (
                value,
                "DA",
                StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0;
        }


        private static string YesNoIndexToString(
            int index)
        {
            return index == 1
                ? "DA"
                : "NE";
        }


        private static int TemaToIndex(
            string? value)
        {
            return string.Equals (
                value,
                "Svijetla",
                StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0;
        }


        private static int OznakaSlijednostiToIndex(
            string? value)
        {
            if(string.IsNullOrWhiteSpace (value))
                return 0;

            return value.Contains (
                "poslov",
                StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0;
        }


        private static int VerzijaAplikacijeToIndex(
            string? value)
        {
            if(string.IsNullOrWhiteSpace (value))
                return 0;

            return value.StartsWith (
                "Prod",
                StringComparison.OrdinalIgnoreCase)
                ? 1
                : 0;
        }


        private static void ShowInfo(
            string message)
        {
            try
            {
                var popup =
                    new MyMessageBox ();

                popup.MessageTitle.Text =
                    "Informacija";

                popup.MessageText.Text =
                    message;

                popup.ShowDialog ();
            }
            catch
            {
                MessageBox.Show (
                    message,
                    "Informacija",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }


        private static void ShowError(
            string message)
        {
            try
            {
                var popup =
                    new MyMessageBox ();

                popup.MessageTitle.Text =
                    "Greška";

                popup.MessageText.Text =
                    message;

                popup.ShowDialog ();
            }
            catch
            {
                MessageBox.Show (
                    message,
                    "Greška",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if(EqualityComparer<T>.Default.Equals (
                field,
                value))
            {
                return false;
            }

            field = value;

            OnPropertyChanged (
                propertyName);

            return true;
        }


        public event PropertyChangedEventHandler?
            PropertyChanged;


        protected virtual void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke (
                this,
                new PropertyChangedEventArgs (
                    propertyName));
        }

        #endregion
    }
}