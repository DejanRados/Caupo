using Caupo.Data;
using Caupo.Properties;
using Caupo.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        #region Win32 API Deklaracije

        [DllImport ("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
            MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport ("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport ("user32.dll", CharSet = CharSet.Auto)]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum,
            ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor,
            ref RECT lprcMonitor, IntPtr dwData);

        #endregion

        #region Strukture

        [StructLayout (LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

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

        [StructLayout (LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs (UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        #endregion




        // Firma
        public string Firma { get; set; }
        public string Adresa { get; set; }
        public string Mjesto { get; set; }
        public string JIB { get; set; }
        public string PDV { get; set; }
        public string ZR { get; set; }
        public string Email { get; set; }

        // Theme
        public string Tema { get; set; }

        // Printers
        public string POSPrinter { get; set; }
        public string A4Printer { get; set; }
        public string KuhinjaPrinter { get; set; }
        public string SankPrinter { get; set; }
        public string ExterniPrinter { get; set; }

        // Server settings
        public string ServerIP { get; set; }
        public string DbPath { get; set; }
        public bool ServerMode { get; set; }

        // LPFR
        public string LPFRIP { get; set; }
        public string LPFRKey { get; set; }
        public string LPFRPin { get; set; }

        // Backup & logo
        public string BackupUrl { get; set; }
        public string LogoUrl { get; set; }

        // MultiUser
        public string MultiUser { get; set; }

        // ComboBox selections
        public string BrojKopijaBloka { get; set; }
        public string ProdajaMinus { get; set; }
        public string PDVKorisnik { get; set; }
        public int DisplayKuhinja { get; set; }
        public string SirinaTrake { get; set; }
        public static List<MonitorInfo> GetAllMonitors()
        {
            var monitors = new List<MonitorInfo> ();
            int monitorIndex = 0;

            MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor,
                ref RECT lprcMonitor, IntPtr dwData) =>
            {
                try
                {
                    MONITORINFOEX monitorInfo = new MONITORINFOEX ();
                    monitorInfo.cbSize = Marshal.SizeOf (typeof (MONITORINFOEX));

                    if(GetMonitorInfo (hMonitor, ref monitorInfo))
                    {
                        string deviceName = monitorInfo.szDevice;
                        bool isPrimary = (monitorInfo.dwFlags & 0x1) != 0;

                        // Dobij stvarno ime monitora iz EDID podataka
                        string monitorName = GetRealMonitorName (deviceName);

                        // Ako nismo dobili ime, koristi opšte ime
                        if(string.IsNullOrEmpty (monitorName))
                        {
                            monitorName = GetDisplayNumberFromDeviceName (deviceName);
                        }

                        monitors.Add (new MonitorInfo
                        {
                            Index = monitorIndex,
                            Name = $"{monitorName}" + (isPrimary ? " (Primarni)" : ""),
                            DeviceName = deviceName,
                            IsPrimary = isPrimary

                        });
                        Debug.WriteLine ("[Monitors] " + monitorName + ", " + monitorIndex + ", " + deviceName + ", " + isPrimary);
                        monitorIndex++;
                    }
                }
                catch
                {
                    // Fallback
                    monitors.Add (new MonitorInfo
                    {
                        Index = monitorIndex,
                        Name = $"Monitor {monitorIndex + 1}",
                        IsPrimary = false
                    });
                    monitorIndex++;
                }

                return true;
            };

            EnumDisplayMonitors (IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

            return monitors;
        }

        private static string GetRealMonitorName(string deviceName)
        {
            try
            {
                // Prvo pokušaj da dobiješ ime iz DeviceString-a
                DISPLAY_DEVICE displayDevice = new DISPLAY_DEVICE ();
                displayDevice.cb = Marshal.SizeOf (typeof (DISPLAY_DEVICE));

                uint deviceIndex = 0;
                while(EnumDisplayDevices (deviceName, deviceIndex, ref displayDevice, 0))
                {
                    // Ako je monitor (a ne grafička kartica)
                    if((displayDevice.StateFlags & 0x00000004) != 0) // DISPLAY_DEVICE_ATTACHED_TO_DESKTOP
                    {
                        // Pokušaj da dobiješ deteljnije informacije sa EDID_FLAG
                        DISPLAY_DEVICE monitorDevice = new DISPLAY_DEVICE ();
                        monitorDevice.cb = Marshal.SizeOf (typeof (DISPLAY_DEVICE));

                        uint monitorIndex = 0;
                        while(EnumDisplayDevices (displayDevice.DeviceName, monitorIndex, ref monitorDevice, 1)) // EDID_FLAG = 1
                        {
                            if(!string.IsNullOrEmpty (monitorDevice.DeviceString) &&
                                monitorDevice.DeviceString != "Generic PnP Monitor")
                            {
                                return monitorDevice.DeviceString.Trim ();
                            }
                            monitorIndex++;
                        }
                    }
                    deviceIndex++;
                }
            }
            catch
            {
                // Ignoriši greške
            }

            return null;
        }

        private static string GetDisplayNumberFromDeviceName(string deviceName)
        {
            // Ekstraktuj broj iz imena, npr. "\\.\DISPLAY3" -> "Display 3"
            if(deviceName.StartsWith (@"\\.\DISPLAY", StringComparison.OrdinalIgnoreCase))
            {
                string numberPart = deviceName.Substring (@"\\.\DISPLAY".Length);
                if(int.TryParse (numberPart, out int displayNumber))
                {
                    return $"Monitor {displayNumber}";
                }
            }
            return deviceName;
        }

        public class MonitorInfo
        {
            public string Name { get; set; }
            public int Index { get; set; }
            public string DeviceName { get; set; }
            public bool IsPrimary { get; set; }

            public override string ToString() => Name;
        }
        public ObservableCollection<MonitorInfo> Monitors { get; } = new ObservableCollection<MonitorInfo> ();
        private MonitorInfo _selectedMonitor;
        public MonitorInfo SelectedMonitor
        {
            get => _selectedMonitor;
            set
            {
                _selectedMonitor = value;
                OnPropertyChanged (nameof (SelectedMonitor));
            }
        }

        public int SelectedMonitorIndex { get; private set; } = -1;
        private Brush? _fontColor;
        public Brush? FontColor
        {
            get { return _fontColor; }
            set
            {
                if(_fontColor != value)
                {
                    _fontColor = value;
                    OnPropertyChanged (nameof (FontColor));
                }
            }
        }

        private string? _imagePathFiskalizacijaButton;
        public string? ImagePathFiskalizacijaButton
        {
            get { return _imagePathFiskalizacijaButton; }
            set
            {
                _imagePathFiskalizacijaButton = value;
                OnPropertyChanged (nameof (ImagePathFiskalizacijaButton));
            }
        }

        private string? _imagePathLogoButton;
        public string? ImagePathLogoButton
        {
            get { return _imagePathLogoButton; }
            set
            {
                _imagePathLogoButton = value;
                OnPropertyChanged (nameof (ImagePathLogoButton));
            }
        }

        private string? _imagePathPrinterButton;
        public string? ImagePathPrinterButton
        {
            get { return _imagePathPrinterButton; }
            set
            {
                _imagePathPrinterButton = value;
                OnPropertyChanged (nameof (ImagePathPrinterButton));
            }
        }

        private ObservableCollection<TblRadnici>? _radnici;

        public ObservableCollection<TblRadnici>? Radnici
        {
            get => _radnici;
            set
            {
                if(_radnici != value)
                {
                    _radnici = value;
                    OnPropertyChanged (nameof (Radnici));
                }
            }
        }

        //public ObservableCollection<DatabaseTables.TblRadnici> Radnici { get; set; } = new ObservableCollection<DatabaseTables.TblRadnici>();
        private DatabaseTables.TblRadnici _selectedRadnik = new TblRadnici ();
        public DatabaseTables.TblRadnici SelectedRadnik
        {
            get { return _selectedRadnik; }
            set
            {
                _selectedRadnik = value;
                OnPropertyChanged (nameof (SelectedRadnik));
            }
        }


        //public ICommand AddCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand DeleteCommand { get; }
        public SettingsViewModel()
        {
            Radnici = new ObservableCollection<TblRadnici> ();
            DeleteCommand = new AsyncRelayCommand (async () => await DeleteRadnik (SelectedRadnik));
            UpdateCommand = new AsyncRelayCommand (async () => await OpenUpdateRadnik (SelectedRadnik));
            Start ();

            var all = GetAllMonitors ();
            foreach(var m in all)
                Monitors.Add (m);

            if(Monitors.Count > 0)
                SelectedMonitor = Monitors[0];

            LoadSettingsFromProperties ();
        }
        public bool isUpdate = false;

        public async void Start()
        {

            await LoadRadniciAsync ();
            await SetImage ();
        }
        public async Task SetImage()
        {
            await Task.Delay (1);
            string tema = Settings.Default.Tema;
            Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema);
            if(tema == "Tamna")
            {
                ImagePathPrinterButton = "pack://application:,,,/Images/Dark/printer.svg";
                ImagePathFiskalizacijaButton = "pack://application:,,,/Images/Dark/cashregister.svg";

            }
            else
            {
                ImagePathPrinterButton = "pack://application:,,,/Images/Light/printer.svg";
                ImagePathFiskalizacijaButton = "pack://application:,,,/Images/Light/cashregister.svg";

            }


        }

        public async Task LoadRadniciAsync()
        {
            try
            {
                using(var db = new AppDbContext ())
                {
                    var radnikExists = await db.Radnici.AnyAsync ();

                }


                using(var db = new AppDbContext ())
                {
                    var radnici = await db.Radnici.ToListAsync ();

                    Radnici?.Clear ();
                    foreach(var radnik in radnici)
                    {


                        Radnici?.Add (radnik);
                    }

                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }

        public async Task NewRadnik(DatabaseTables.TblRadnici radnik)
        {
            try
            {
                using(var db = new AppDbContext ())
                {
                    await db.Radnici.AddAsync (radnik);
                    await db.SaveChangesAsync ();
                }

                Radnici?.Add (radnik);
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }

        public async Task UpdateRadnik(DatabaseTables.TblRadnici radnik)
        {
            try
            {
                Debug.WriteLine ("id radnika:" + radnik.IdRadnika);
                Debug.WriteLine ("Update rlozinka:" + radnik.Lozinka);
                using(var db = new AppDbContext ())
                {
                    var user = await db.Radnici.FindAsync (radnik.IdRadnika);
                    if(user != null)
                    {

                        user.Radnik = radnik.Radnik;
                        user.Lozinka = radnik.Lozinka;
                        user.Dozvole = radnik.Dozvole;


                        db.Radnici.Update (user);

                        await db.SaveChangesAsync ();

                        await LoadRadniciAsync ();
                        //Debug.WriteLine("--------------------------- Radnik -" +    Radnici[4]?.Radnik);
                        OnPropertyChanged (nameof (Radnici));
                    }
                    else
                    {
                        Debug.WriteLine ("Nenadje radnika:" + radnik.Radnik);

                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }

        public async Task DeleteRadnik(DatabaseTables.TblRadnici radnik)
        {


            YesNoPopup myMessageBox = new YesNoPopup ();

            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
            myMessageBox.MessageText.Text = "Da li ste sigurni da želite obrisati radnika:" + Environment.NewLine + radnik.Radnik + " ?";
            myMessageBox.ShowDialog ();
            if(myMessageBox.Kliknuo == "Da")
            {
                Radnici?.Remove (radnik);
                using(var db = new AppDbContext ())
                {
                    var user = await db.Radnici.FindAsync (radnik.IdRadnika);
                    if(user != null)
                    {
                        db.Radnici.Remove (user);
                        await db.SaveChangesAsync ();
                    }
                }
            }
        }

        public async Task OpenUpdateRadnik(DatabaseTables.TblRadnici radnik)
        {
            await Task.Delay (1);
            if(radnik != null)
            {

                NewWorkerPopup update = new NewWorkerPopup (this);
                update.isUpdate = true;
                update.DataContext = this;
                update.lblid.Content = radnik.IdRadnika;
                update.PopupTitle.Text = "Uredi radnika";
                update.txtRadnik.Text = radnik.Radnik;
                update.txtLozinka.Text = radnik.Lozinka;
                update.cmbDozvole.SelectedItem = update.cmbDozvole.Items.Cast<ComboBoxItem> ().FirstOrDefault (i => (string)i.Content == radnik.Dozvole);

                update.ShowDialog ();


            }
            else
            {
                MessageBox.Show ("Nema selektovanog radnika!", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        private void LoadSettingsFromProperties()
        {
            Firma = Properties.Settings.Default.Firma;
            Adresa = Properties.Settings.Default.Adresa;
            Mjesto = Properties.Settings.Default.Mjesto;
            JIB = Properties.Settings.Default.JIB;
            PDV = Properties.Settings.Default.PDV;
            ZR = Properties.Settings.Default.ZR;
            Email = Properties.Settings.Default.Email;

            Tema = Properties.Settings.Default.Tema;
            POSPrinter = Properties.Settings.Default.POSPrinter;
            A4Printer = Properties.Settings.Default.A4Printer;
            KuhinjaPrinter = Properties.Settings.Default.KuhinjaPrinter;
            SankPrinter = Properties.Settings.Default.SankPrinter;
            ExterniPrinter = Properties.Settings.Default.ExterniPrinter;

            ServerIP = Properties.Settings.Default.ServerIP;
            DbPath = Properties.Settings.Default.DbPath;
            ServerMode = !string.IsNullOrEmpty (DbPath);

            LPFRIP = Properties.Settings.Default.LPFR_IP;
            LPFRKey = Properties.Settings.Default.LPFR_Key;
            LPFRPin = Properties.Settings.Default.LPFR_Pin;

            BackupUrl = Properties.Settings.Default.BackupUrl;
            LogoUrl = Properties.Settings.Default.LogoUrl;

            MultiUser = Properties.Settings.Default.MultiUser;
            BrojKopijaBloka = Properties.Settings.Default.BlokKopija;
            ProdajaMinus = Properties.Settings.Default.ProdajaMinus;
            PDVKorisnik = Properties.Settings.Default.PDVKorisnik;
            DisplayKuhinja = int.TryParse (Properties.Settings.Default.DisplayKuhinja, out int d) ? d : 0;
            SirinaTrake = Properties.Settings.Default.SirinaTrake;

            SelectedMonitor = Monitors.FirstOrDefault (m => m.Index == DisplayKuhinja);
        }

        public async Task SaveSettingsAsync()
        {
            Properties.Settings.Default.Firma = Firma;
            Properties.Settings.Default.Adresa = Adresa;
            Properties.Settings.Default.Mjesto = Mjesto;
            Properties.Settings.Default.JIB = JIB;
            Properties.Settings.Default.PDV = PDV;
            Properties.Settings.Default.ZR = ZR;
            Properties.Settings.Default.Email = Email;

            Properties.Settings.Default.Tema = Tema;
            Properties.Settings.Default.POSPrinter = POSPrinter;
            Properties.Settings.Default.A4Printer = A4Printer;
            Properties.Settings.Default.KuhinjaPrinter = KuhinjaPrinter;
            Properties.Settings.Default.SankPrinter = SankPrinter;
            Properties.Settings.Default.ExterniPrinter = ExterniPrinter;

            Properties.Settings.Default.ServerIP = ServerIP;
            Properties.Settings.Default.DbPath = DbPath;

            Properties.Settings.Default.LPFR_IP = LPFRIP;
            Properties.Settings.Default.LPFR_Key = LPFRKey;
            Properties.Settings.Default.LPFR_Pin = LPFRPin;

            Properties.Settings.Default.BackupUrl = BackupUrl;
            Properties.Settings.Default.LogoUrl = LogoUrl;

            Properties.Settings.Default.MultiUser = MultiUser;
            Properties.Settings.Default.BlokKopija = BrojKopijaBloka;
            Properties.Settings.Default.ProdajaMinus = ProdajaMinus;
            Properties.Settings.Default.PDVKorisnik = PDVKorisnik;
            Properties.Settings.Default.DisplayKuhinja = DisplayKuhinja.ToString ();
            Properties.Settings.Default.SirinaTrake = SirinaTrake;

            Properties.Settings.Default.Save ();
        }




        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? propertyName)
        {
            Debug.WriteLine ($"Property Changed: {propertyName}");
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }



    }
}
