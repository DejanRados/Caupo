using Caupo.Data;
using Caupo.Properties;
using Caupo.UserControls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Input;
using static Caupo.Data.DatabaseTables;
using System.IO;

namespace Caupo.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private int _paperWidthMm;
        private int _brojBloka;
        private string _firma = Settings.Default.Firma;
        private string _adresa = Settings.Default.Adresa;
        private string _grad = Settings.Default.Mjesto;
        private string _jib= Settings.Default.JIB;
        private string _pdv= Settings.Default.PDV;
        private string logoPath = Settings.Default.LogoUrl;
        private string printerSank = Settings.Default.SankPrinter;
        private string printerPos = Settings.Default.POSPrinter;
        private Image? _logo;

        public class UtrosakRepromaterijala
        {
            public string Repromaterijal { get; set; }
            public string JedinicaMjere { get; set; }
            public decimal? UkupnoKolicina { get; set; }
        }

        public GridLength GornjiGridHeight => ProdaniArtikli != null && ProdaniArtikli.Any()  ? new GridLength(2, GridUnitType.Star)   : new GridLength(0);

        public GridLength DonjiGridHeight => ReklamiraniArtikli != null && ReklamiraniArtikli.Any() ? new GridLength(1, GridUnitType.Star)  : new GridLength(1, GridUnitType.Star);

        #region Kolekcije
        public ObservableCollection<TblRacuni> SviRacuni { get; set; }
        public ObservableCollection<TblRacunStavka> SveStavke { get; set; }

        public ObservableCollection<TblRacuni> SviReklamiraniRacuni { get; set; }
        public ObservableCollection<TblUlazRepromaterijal> SviUlaziNamirnica { get; set; }
        public ObservableCollection<TblUlazRepromaterijalStavka> SveStavkeUlazaNamirnica { get; set; }
        public ObservableCollection<TblRadnici> SviRadnici { get; set; }
        public ObservableCollection<string> VrsteArtikala { get; set; }
        public ObservableCollection<TblArtikli> ListaArtikala { get; set; }

        public ObservableCollection<TblRepromaterijal> ListaRepromaterijala { get; set; }
        public ObservableCollection<TblNormativ> ListaNormativa { get; set; }

        public ObservableCollection<ArtiklPromet> UtrosakNamirnica { get; set; }
        #endregion

        #region Filter Properties
        private DateTime _odDatuma = DateTime.Today;
        private DateTime _doDatuma = DateTime.Today;
        private TblRadnici _odabraniRadnik;
        private int _odabranaVrstaArtikla;
        public DateTime OdDatuma
        {
            get => _odDatuma;
            set { _odDatuma = value; OnPropertyChanged(); Filtrirano(); }
        }

        public DateTime DoDatuma
        {
            get => _doDatuma;
            set { _doDatuma = value; OnPropertyChanged(); Filtrirano(); }
        }

        public TblRadnici OdabraniRadnik
        {
            get => _odabraniRadnik;
            set { _odabraniRadnik = value; OnPropertyChanged(); Filtrirano(); }
        }

        public int OdabranaVrstaArtikla
        {
            get => _odabranaVrstaArtikla;
            set { _odabranaVrstaArtikla = value; OnPropertyChanged(); Filtrirano(); }
        }
        #endregion
        #region Filtrirane kolekcije
        public ObservableCollection<TblRacuni> FiltriraniRacuni { get; set; }
        public ObservableCollection<TblRacunStavka> FiltriraneStavke { get; set; }
        #endregion

        #region Rezultati za prikaz
        public decimal? PrometKes { get; set; }
        public decimal? PrometKartica { get; set; }
        public decimal? PrometCek { get; set; }
        public decimal? PrometVirman { get; set; }
        public decimal? ReklamiraniPromet { get; set; }
        public decimal? UkupanPromet { get; set; }

        public decimal? KonacanPazar { get; set; }

        public ObservableCollection<ArtiklPromet> ProdaniArtikli { get; set; }
        public ObservableCollection<ArtiklPromet> ReklamiraniArtikli { get; set; }

        public class ArtiklPromet
        {
            public string NazivArtikla { get; set; }
            public decimal? Kolicina { get; set; }
            public decimal?   Cijena { get; set; }
            public decimal? Iznos { get; set; }
        }
        #endregion
        public string ReportTitle { get; set; }

        public string ReportType { get; set; }
        public ICommand DnevniPrometUkupnoCommand { get; }
        public ICommand DnevniPrometPoRadnikuCommand { get; }
        public ICommand DnevniPrometPoArtiklimaCommand { get; }


        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        private System.Windows.Media.Brush? _fontColor;
        public System.Windows.Media.Brush? FontColor
        {
            get { return _fontColor; }
            set
            {
                if (_fontColor != value)
                {
                    _fontColor = value;
                    OnPropertyChanged(nameof(FontColor));
                }
            }
        }

        public enum TipIzvjestaja
        {
            Dnevni,
            Periodicni
        }

        private TipIzvjestaja _trenutniIzvjestaj;
        public TipIzvjestaja TrenutniIzvjestaj
        {
            get => _trenutniIzvjestaj;
            set
            {
                _trenutniIzvjestaj = value;
                OnPropertyChanged();
            }
        }

        private bool _periodicniPrometAktivan;
        public bool PeriodicniPrometAktivan
        {
            get => _periodicniPrometAktivan;
            set
            {
                _periodicniPrometAktivan = value;
                OnPropertyChanged(nameof(PeriodicniPrometAktivan));
                OnPropertyChanged(nameof(PrviDatePickerLabel));
                OnPropertyChanged(nameof(DrugiDatePickerVisibility));
                if (!value)
                {
                    DoDatuma = OdDatuma;
                }
            }
        }

     

        private bool _isRadnik;
        public bool IsRadnik
        {
            get => _isRadnik;
            set
            {
                 _isRadnik = value;
           
                OnPropertyChanged(nameof(IsRadnik));
                OnPropertyChanged(nameof(ComboRadnikVisibility));
            }
        }

        public string PrviDatePickerLabel =>
    PeriodicniPrometAktivan ? "Od datuma:" : "Datum:";

        public Visibility DrugiDatePickerVisibility =>
            PeriodicniPrometAktivan ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ComboRadnikVisibility =>
           IsRadnik ? Visibility.Visible : Visibility.Collapsed;

        public DashboardViewModel()
        {
            DnevniPrometUkupnoCommand = new RelayCommand(IzracunajDnevniPromet);
            DnevniPrometPoRadnikuCommand = new RelayCommand(IzracunajDnevniPrometPoRadniku);
            DnevniPrometPoArtiklimaCommand = new RelayCommand(IzracunajDnevniPrometPoArtiklima);
            ProdaniArtikli = new ObservableCollection<ArtiklPromet>();
            ReklamiraniArtikli = new ObservableCollection<ArtiklPromet>();
            SviRacuni = new ObservableCollection<TblRacuni>();
            SveStavke = new ObservableCollection<TblRacunStavka> { };
            SviUlaziNamirnica = new ObservableCollection<TblUlazRepromaterijal>();
            SveStavkeUlazaNamirnica = new ObservableCollection<TblUlazRepromaterijalStavka>();
            FiltriraneStavke = new ObservableCollection<TblRacunStavka>();
            FiltriraniRacuni = new ObservableCollection<TblRacuni>();
            SviRadnici = new ObservableCollection<TblRadnici>();
            ListaArtikala = new ObservableCollection<TblArtikli>();
            ListaRepromaterijala = new ObservableCollection<TblRepromaterijal>();
            ListaNormativa = new ObservableCollection<TblNormativ>();
            UtrosakNamirnica = new ObservableCollection<ArtiklPromet>();
            UcitajRacune();
            UcitajStavke();
            UcitajUlazeNamirnica();
            UcitajStavkeUlazaNamirnica();
            UcitajRadnike();
            UcitajArtikle();
            UcitajNamirnice();
            UcitajNormative();
          
            SetImage();

            PeriodicniPrometAktivan = false;
           OdDatuma = DateTime.Today;
            DoDatuma = DateTime.Today;
            CurrentView = null;
            _paperWidthMm = Convert.ToInt32(Settings.Default.SirinaTrake);
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                _logo = Image.FromFile(logoPath);
            }
        }

        public async Task SetImage()
        {
            await Task.Delay(5);
            string tema = Settings.Default.Tema;
            Debug.WriteLine("Aktivna tema koju vidi viewmodel je : " + tema);
            if (tema == "Tamna")
            {
                FontColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb( 255, 212, 212, 212));
                Application.Current.Resources["GlobalFontColor"] =    FontColor;
            }
            else
            {
                FontColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255,50, 50, 50));
                Application.Current.Resources["GlobalFontColor"] =    FontColor;
            }
        }
        private void ShowReport(string reportType)
        {
            ReportType = reportType;
            switch (reportType)
            {
                
                case "DnevniPromet":
                    CurrentView = new SummaryView { DataContext = this };
                    break;
                case "DnevniPrometPoRadniku":
                    CurrentView = new SummaryView { DataContext = this };
                    break;
                case "DnevniPrometPoArtiklima":
                    CurrentView = new ListView { DataContext = this };
                    break;
                case "PeriodicniPromet":
                    CurrentView = new SummaryView { DataContext = this };
                    break;
                case "PeriodicniPrometPoRadniku":
                    CurrentView = new SummaryView { DataContext = this };
                    break;
                case "PeriodicniPrometPoArtiklima":
                    CurrentView = new ListView { DataContext = this };
                    break;
                case "PrometSanka":
                    CurrentView = new ListView { DataContext = this };
                    break;
                case "PrometKuhinje":
                    CurrentView = new ListView { DataContext = this };
                    break;
                case "PrometOstalo":
                    CurrentView = new ListView { DataContext = this };
                    break;
                case "DnevniUtrosakNamirnica":
                    CurrentView = new ListView { DataContext = this };
                    break;
                case "PeriodicniUtrosakNamirnica":
                    CurrentView = new ListView { DataContext = this };
                    break;
                case "ZalihaNamirnica":
                    CurrentView = new ListView { DataContext = this };
                    break;
                default:
                    CurrentView = null;
                    break;
            }
        }

        #region Učitavanje podataka
        private void UcitajArtikle()
        {
            using var db = new AppDbContext();
            ListaArtikala= new ObservableCollection<TblArtikli>(db.Artikli.ToList());
         
        }

        private void UcitajNamirnice()
        {
            using var db = new AppDbContext();
            ListaRepromaterijala = new ObservableCollection<TblRepromaterijal>(db.Repromaterijal.ToList());

        }

     

        private void UcitajNormative()
        {
            using var db = new AppDbContext();
            ListaNormativa = new ObservableCollection<TblNormativ>(db.Normativ.ToList());
        }


        private void UcitajRacune()
        {
            using var db = new AppDbContext();
            SviRacuni = new ObservableCollection<TblRacuni>(db.Racuni .ToList());
            FiltriraniRacuni = new ObservableCollection<TblRacuni>(SviRacuni);
        }

        private void UcitajStavke()
        {
            using var db = new AppDbContext();
            SveStavke = new ObservableCollection<TblRacunStavka>(db.RacunStavka.ToList());
            FiltriraneStavke = new ObservableCollection<TblRacunStavka>(SveStavke);
        }

        private void UcitajUlazeNamirnica()
        {
            using var db = new AppDbContext();
            SviUlaziNamirnica = new ObservableCollection<TblUlazRepromaterijal>(db.UlazRepromaterijal.ToList());
         
        }

        private void UcitajStavkeUlazaNamirnica()
        {
            using var db = new AppDbContext();
            SveStavkeUlazaNamirnica = new ObservableCollection<TblUlazRepromaterijalStavka>(db.UlazRepromaterijalStavka.ToList());
            
        }

        private void UcitajRadnike()
        {
            using var db = new AppDbContext();
            SviRadnici = new ObservableCollection<TblRadnici>(db.Radnici.ToList());
        }


        #endregion

        public void IzracunajDnevniPrometPoRadniku()
        {
            TrenutniIzvjestaj = TipIzvjestaja.Dnevni;
            ReportTitle = "Dnevni promet po radniku";
             if(OdabraniRadnik == null)
             OdabraniRadnik = SviRadnici.FirstOrDefault();
                IsRadnik = true;
           
            

            var datum = OdDatuma.Date;

            var stavkeZaDatumIRadnika = from racun in SviRacuni
                                        join stavka in SveStavke on racun.BrojRacuna equals stavka.BrojRacuna
                                        where racun.Datum.Date == datum &&
                                              racun.Radnik == OdabraniRadnik.IdRadnika.ToString()
                                        select new { Racun = racun, Stavka = stavka };

            PrometKes = stavkeZaDatumIRadnika.Where(x => x.Racun.NacinPlacanja == 0)
                                            .Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena);

            PrometKartica = stavkeZaDatumIRadnika.Where(x => x.Racun.NacinPlacanja == 1)
                                            .Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena);

            PrometCek = stavkeZaDatumIRadnika.Where(x => x.Racun.NacinPlacanja == 2)
                                            .Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena);

            PrometVirman = stavkeZaDatumIRadnika.Where(x => x.Racun.NacinPlacanja == 3)
                                            .Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena);

            ReklamiraniPromet = stavkeZaDatumIRadnika.Where(x => x.Racun.Reklamiran == "DA")
                                            .Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena);

            UkupanPromet = PrometKes + PrometKartica + PrometCek + PrometVirman;

            KonacanPazar = UkupanPromet - ReklamiraniPromet;

            OnPropertyChanged(nameof(PrometKes));
            OnPropertyChanged(nameof(PrometKartica));
            OnPropertyChanged(nameof(PrometCek));
            OnPropertyChanged(nameof(PrometVirman));
            OnPropertyChanged(nameof(ReklamiraniPromet));
            OnPropertyChanged(nameof(UkupanPromet));
            OnPropertyChanged(nameof(KonacanPazar));
            OnPropertyChanged(nameof(IsRadnik));
            OnPropertyChanged(nameof(ComboRadnikVisibility));

            ShowReport("DnevniPrometPoRadniku");
        }
        public void IzracunajDnevniPromet()
        {
            IsRadnik = false;
        
            TrenutniIzvjestaj = TipIzvjestaja.Dnevni;
            ReportTitle = "Dnevni promet";
            var datum = OdDatuma.Date;
           
            var racuniZaDatum = SviRacuni.Where(r => r.Datum.Date == datum);

            PrometKes = racuniZaDatum.Where(r => r.NacinPlacanja == 0)
                                    .Sum(r => SveStavke.Where(s => s.BrojRacuna == r.BrojRacuna)
                                                            .Sum(s => s.Kolicina * s.Cijena));

            PrometKartica = racuniZaDatum.Where(r => r.NacinPlacanja == 1)
                                        .Sum(r => SveStavke.Where(s => s.BrojRacuna == r.BrojRacuna)
                                                                .Sum(s => s.Kolicina * s.Cijena));

            PrometCek = racuniZaDatum.Where(r => r.NacinPlacanja == 2)
                                    .Sum(r => SveStavke.Where(s => s.BrojRacuna == r.BrojRacuna)
                                                            .Sum(s => s.Kolicina * s.Cijena));

            PrometVirman = racuniZaDatum.Where(r => r.NacinPlacanja == 3)
                                       .Sum(r => SveStavke.Where(s => s.BrojRacuna == r.BrojRacuna)
                                                               .Sum(s => s.Kolicina * s.Cijena));

            ReklamiraniPromet = racuniZaDatum.Where(x => x.Reklamiran == "DA")
                                   .Sum(r => SveStavke.Where(s => s.BrojRacuna == r.BrojRacuna)
                                                               .Sum(s => s.Kolicina * s.Cijena));

            UkupanPromet = PrometKes + PrometKartica + PrometCek + PrometVirman;

            KonacanPazar = UkupanPromet - ReklamiraniPromet;

            OnPropertyChanged(nameof(PrometKes));
            OnPropertyChanged(nameof(PrometKartica));
            OnPropertyChanged(nameof(PrometCek));
            OnPropertyChanged(nameof(PrometVirman));
            OnPropertyChanged(nameof(ReklamiraniPromet));
            OnPropertyChanged(nameof(UkupanPromet));
            OnPropertyChanged(nameof(KonacanPazar));
            OnPropertyChanged(nameof(IsRadnik));
            OnPropertyChanged(nameof(ComboRadnikVisibility));

            ShowReport("DnevniPromet");
        }

        public void IzracunajDnevniPrometPoArtiklima()
        {
            TrenutniIzvjestaj = TipIzvjestaja.Dnevni;
            
            
            ReportTitle = "Dnevni promet po artiklima";
            var datum = OdDatuma.Date;
           
            var stavkeZaDatum = from racun in SviRacuni
                                join stavka in SveStavke on racun.BrojRacuna equals stavka.BrojRacuna
                                where racun.Datum.Date == datum
                                select new { Racun = racun, Stavka = stavka };

            ProdaniArtikli.Clear();
            ProdaniArtikli = new ObservableCollection<ArtiklPromet>(
                stavkeZaDatum
                    .GroupBy(x => x.Stavka.Artikl)
                    .Select(g => new ArtiklPromet
                    {
                        NazivArtikla = g.Key.ToString(),
                        Kolicina = g.Sum(x => x.Stavka.Kolicina),
                        Cijena = g.Average(x => x.Stavka.Cijena),
                        Iznos = g.Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena)
                    })
                    .OrderByDescending(a => a.Iznos)
                    .ToList()
            );

            // Reklamirani artikli
            ReklamiraniArtikli.Clear() ;
            ReklamiraniArtikli = new ObservableCollection<ArtiklPromet>(
                stavkeZaDatum
                    .Where(x => x.Racun.Reklamiran == "DA")
                    .GroupBy(x => x.Stavka.Artikl)
                    .Select(g => new ArtiklPromet
                    {
                        NazivArtikla = g.Key.ToString(),
                        Kolicina = g.Sum(x => x.Stavka.Kolicina),
                        Cijena = g.Average(x => x.Stavka.Cijena),
                        Iznos = g.Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena)
                    })
                    .OrderByDescending(a => a.Iznos)
                    .ToList()
            );

            // Ukupan promet
            UkupanPromet = ProdaniArtikli.Sum(a => a.Iznos);
            ReklamiraniPromet = ReklamiraniArtikli.Sum(a => a.Iznos);

            IsRadnik = false;
            // MVVM Binding
            OnPropertyChanged(nameof(ProdaniArtikli));
            OnPropertyChanged(nameof(ReklamiraniArtikli));
            OnPropertyChanged(nameof(UkupanPromet));
            OnPropertyChanged(nameof(ReklamiraniPromet));
            OnPropertyChanged(nameof(IsRadnik));
            OnPropertyChanged(nameof(ComboRadnikVisibility));
            OnPropertyChanged(nameof(GornjiGridHeight));
            OnPropertyChanged(nameof(DonjiGridHeight));

            ShowReport("DnevniPrometPoArtiklima");
         
            Debug.WriteLine("IsRadnik je: " + IsRadnik.ToString());
        }

        public void IzracunajPeriodicniPromet()
        {
            TrenutniIzvjestaj = TipIzvjestaja.Periodicni;
            IsRadnik = false;
           
            var od = OdDatuma.Date;
            var doo = DoDatuma.Date;
            ReportTitle = "Promet za period:"+Environment.NewLine + od.ToString("dd.MM.yy") + " - " + doo.ToString("dd.MM.yy");
            var racuniZaPeriod = SviRacuni
                .Where(r => r.Datum.Date >= od && r.Datum.Date <= doo);

            // GOTOVINA
            PrometKes = racuniZaPeriod.Where(r => r.NacinPlacanja == 0)
                .Sum(r => SveStavke.Where(s => s.BrojRacuna == r.BrojRacuna)
                    .Sum(s => s.Kolicina * s.Cijena));

            // KARTICA
            PrometKartica = racuniZaPeriod.Where(r => r.NacinPlacanja == 1)
                .Sum(r => SveStavke.Where(s => s.BrojRacuna == r.BrojRacuna)
                    .Sum(s => s.Kolicina * s.Cijena));

            // ČEK
            PrometCek = racuniZaPeriod.Where(r => r.NacinPlacanja == 2)
                .Sum(r => SveStavke.Where(s => s.BrojRacuna == r.BrojRacuna)
                    .Sum(s => s.Kolicina * s.Cijena));

            // VIRMAN
            PrometVirman = racuniZaPeriod.Where(r => r.NacinPlacanja == 3)
                .Sum(r => SveStavke.Where(s => s.BrojRacuna == r.BrojRacuna)
                    .Sum(s => s.Kolicina * s.Cijena));

            // REKLAMIRANI PROMET
            ReklamiraniPromet = racuniZaPeriod.Where(x => x.Reklamiran == "DA")
                .Sum(r => SveStavke.Where(s => s.BrojRacuna == r.BrojRacuna)
                    .Sum(s => s.Kolicina * s.Cijena));

            // UKUPNO
            UkupanPromet = PrometKes + PrometKartica + PrometCek + PrometVirman;

            // KONAČAN PAZAR
            KonacanPazar = UkupanPromet - ReklamiraniPromet;

            // OBAVIJESTI UI
            OnPropertyChanged(nameof(PrometKes));
            OnPropertyChanged(nameof(PrometKartica));
            OnPropertyChanged(nameof(PrometCek));
            OnPropertyChanged(nameof(PrometVirman));
            OnPropertyChanged(nameof(ReklamiraniPromet));
            OnPropertyChanged(nameof(UkupanPromet));
            OnPropertyChanged(nameof(KonacanPazar));
            OnPropertyChanged(nameof(IsRadnik));
            OnPropertyChanged(nameof(ComboRadnikVisibility));

            // POSTAVI TIP IZVJEŠTAJA


            // PROMIJENI PRIKAZ
            ShowReport("PeriodicniPromet");
        }


        public void IzracunajPeriodicniPrometPoRadniku()
        {
            TrenutniIzvjestaj = TipIzvjestaja.Periodicni;
            if (OdabraniRadnik == null)
                OdabraniRadnik = SviRadnici.FirstOrDefault();
                IsRadnik = true;

           
       


            var od = OdDatuma.Date;
            var doo = DoDatuma.Date;
            ReportTitle = "Promet po radniku za period:" + Environment.NewLine + od.ToString("dd.MM.yy") + " - " + doo.ToString("dd.MM.yy");
            var stavkeZaPeriodIRadnika =
                from racun in SviRacuni
                join stavka in SveStavke on racun.BrojRacuna equals stavka.BrojRacuna
                where racun.Datum.Date >= od &&
                      racun.Datum.Date <= doo &&
                      racun.Radnik == OdabraniRadnik.IdRadnika.ToString()
                select new { Racun = racun, Stavka = stavka };

            // GOTOVINA
            PrometKes = stavkeZaPeriodIRadnika
                .Where(x => x.Racun.NacinPlacanja == 0)
                .Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena);

            // KARTICA
            PrometKartica = stavkeZaPeriodIRadnika
                .Where(x => x.Racun.NacinPlacanja == 1)
                .Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena);

            // ČEK
            PrometCek = stavkeZaPeriodIRadnika
                .Where(x => x.Racun.NacinPlacanja == 2)
                .Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena);

            // VIRMAN
            PrometVirman = stavkeZaPeriodIRadnika
                .Where(x => x.Racun.NacinPlacanja == 3)
                .Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena);

            // REKLAMIRANI
            ReklamiraniPromet = stavkeZaPeriodIRadnika
                .Where(x => x.Racun.Reklamiran == "DA")
                .Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena);

            // UKUPNO
            UkupanPromet = PrometKes + PrometKartica + PrometCek + PrometVirman;

            // PO REDU ODUZIMAMO REKLAMIRANO
            KonacanPazar = UkupanPromet - ReklamiraniPromet;

            // OBAVIJESTI UI
            OnPropertyChanged(nameof(PrometKes));
            OnPropertyChanged(nameof(PrometKartica));
            OnPropertyChanged(nameof(PrometCek));
            OnPropertyChanged(nameof(PrometVirman));
            OnPropertyChanged(nameof(ReklamiraniPromet));
            OnPropertyChanged(nameof(UkupanPromet));
            OnPropertyChanged(nameof(KonacanPazar));
            OnPropertyChanged(nameof(IsRadnik));
            OnPropertyChanged(nameof(ComboRadnikVisibility));

            // SETUJ TIP


            // PROMJENA PRIKAZA
            ShowReport("PeriodicniPrometPoRadniku");
        }

        public void IzracunajPeriodicniPrometPoArtiklima()
        {
            TrenutniIzvjestaj = TipIzvjestaja.Periodicni;
            IsRadnik = false;

            var od = OdDatuma.Date;
            var dO = DoDatuma.Date;
            ReportTitle = "Promet po artiklima za period:" + Environment.NewLine + od.ToString("dd.MM.yy") + " - " + dO.ToString("dd.MM.yy");
            var stavkeUPeriodu = from racun in SviRacuni
                                 join stavka in SveStavke on racun.BrojRacuna equals stavka.BrojRacuna
                                 where racun.Datum.Date >= od &&
                                       racun.Datum.Date <= dO
                                 select new { Racun = racun, Stavka = stavka };

            // Prodani artikli u periodu
            ProdaniArtikli.Clear();
            ProdaniArtikli = new ObservableCollection<ArtiklPromet>(
                stavkeUPeriodu
                    .GroupBy(x => x.Stavka.Artikl)
                    .Select(g => new ArtiklPromet
                    {
                        NazivArtikla = g.Key.ToString(),
                        Kolicina = g.Sum(x => x.Stavka.Kolicina),
                        Cijena = g.Average(x => x.Stavka.Cijena),
                        Iznos = g.Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena)
                    })
                    .OrderByDescending(a => a.Iznos)
                    .ToList()
            );

            // Reklamirani artikli u periodu
            ReklamiraniArtikli.Clear() ;
            ReklamiraniArtikli = new ObservableCollection<ArtiklPromet>(
                stavkeUPeriodu
                    .Where(x => x.Racun.Reklamiran == "DA")
                    .GroupBy(x => x.Stavka.Artikl)
                    .Select(g => new ArtiklPromet
                    {
                        NazivArtikla = g.Key.ToString(),
                        Kolicina = g.Sum(x => x.Stavka.Kolicina),
                        Cijena = g.Average(x => x.Stavka.Cijena),
                        Iznos = g.Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena)
                    })
                    .OrderByDescending(a => a.Iznos)
                    .ToList()
            );

            // Ukupan promet u periodu
            UkupanPromet = ProdaniArtikli.Sum(a => a.Iznos);
            ReklamiraniPromet = ReklamiraniArtikli.Sum(a => a.Iznos);
            // MVVM Binding
            OnPropertyChanged(nameof(ProdaniArtikli));
            OnPropertyChanged(nameof(ReklamiraniArtikli));
            OnPropertyChanged(nameof(UkupanPromet));
            OnPropertyChanged(nameof(ReklamiraniPromet));
            OnPropertyChanged(nameof(IsRadnik));
            OnPropertyChanged(nameof(ComboRadnikVisibility));
            OnPropertyChanged(nameof(GornjiGridHeight));
            OnPropertyChanged(nameof(DonjiGridHeight));


            ShowReport("PeriodicniPrometPoArtiklima");
        }


        public void IzracunajDnevniPrometSanka()
        {
            TrenutniIzvjestaj = TipIzvjestaja.Dnevni;
            PeriodicniPrometAktivan = false;
            ReportTitle = "Dnevni promet šanka";
            var datum = OdDatuma.Date;

            
            var stavkeZaDatum = from racun in SviRacuni
                                join stavka in SveStavke on racun.BrojRacuna equals stavka.BrojRacuna
                                where racun.Datum.Date == datum
                                      && stavka.VrstaArtikla == 0
                                select new { Racun = racun, Stavka = stavka };

            // ⭐ Prodani artikli
            ProdaniArtikli.Clear();
            ProdaniArtikli = new ObservableCollection<ArtiklPromet>(
                stavkeZaDatum
                    .GroupBy(x => x.Stavka.Artikl)
                    .Select(g => new ArtiklPromet
                    {
                        NazivArtikla = g.Key.ToString(),
                        Kolicina = g.Sum(x => x.Stavka.Kolicina),
                        Cijena = g.Average(x => x.Stavka.Cijena),
                        Iznos = g.Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena)
                    })
                    .OrderByDescending(a => a.Iznos)
                    .ToList()
            );

            // ⭐ Reklamirani artikli
            ReklamiraniArtikli.Clear();
            ReklamiraniArtikli = new ObservableCollection<ArtiklPromet>(
                stavkeZaDatum
                    .Where(x => x.Racun.Reklamiran == "DA")
                    .GroupBy(x => x.Stavka.Artikl)
                    .Select(g => new ArtiklPromet
                    {
                        NazivArtikla = g.Key.ToString(),
                        Kolicina = g.Sum(x => x.Stavka.Kolicina),
                        Cijena = g.Average(x => x.Stavka.Cijena),
                        Iznos = g.Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena)
                    })
                    .OrderByDescending(a => a.Iznos)
                    .ToList()
            );

            // ⭐ Ukupan promet
            UkupanPromet = ProdaniArtikli.Sum(a => a.Iznos);
            ReklamiraniPromet = ReklamiraniArtikli.Sum(a => a.Iznos);

            IsRadnik = false;

            OnPropertyChanged(nameof(ProdaniArtikli));
            OnPropertyChanged(nameof(ReklamiraniArtikli));
            OnPropertyChanged(nameof(UkupanPromet));
            OnPropertyChanged(nameof(ReklamiraniPromet));
            OnPropertyChanged(nameof(IsRadnik));
            OnPropertyChanged(nameof(ComboRadnikVisibility));
            OnPropertyChanged(nameof(GornjiGridHeight));
            OnPropertyChanged(nameof(DonjiGridHeight));

            ShowReport("PrometSanka");

            Debug.WriteLine("IsRadnik je: " + IsRadnik.ToString());
        }

        public void IzracunajDnevniPrometKuhinje()
        {
            TrenutniIzvjestaj = TipIzvjestaja.Dnevni;
            PeriodicniPrometAktivan = false;
            ReportTitle = "Dnevni promet kuhinje";
            var datum = OdDatuma.Date;


            var stavkeZaDatum = from racun in SviRacuni
                                join stavka in SveStavke on racun.BrojRacuna equals stavka.BrojRacuna
                                where racun.Datum.Date == datum
                                      && stavka.VrstaArtikla == 1
                                select new { Racun = racun, Stavka = stavka };

            // ⭐ Prodani artikli
            ProdaniArtikli.Clear();
            ProdaniArtikli = new ObservableCollection<ArtiklPromet>(
                stavkeZaDatum
                    .GroupBy(x => x.Stavka.Artikl)
                    .Select(g => new ArtiklPromet
                    {
                        NazivArtikla = g.Key.ToString(),
                        Kolicina = g.Sum(x => x.Stavka.Kolicina),
                        Cijena = g.Average(x => x.Stavka.Cijena),
                        Iznos = g.Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena)
                    })
                    .OrderByDescending(a => a.Iznos)
                    .ToList()
            );

            // ⭐ Reklamirani artikli
            ReklamiraniArtikli.Clear();
            ReklamiraniArtikli = new ObservableCollection<ArtiklPromet>(
                stavkeZaDatum
                    .Where(x => x.Racun.Reklamiran == "DA")
                    .GroupBy(x => x.Stavka.Artikl)
                    .Select(g => new ArtiklPromet
                    {
                        NazivArtikla = g.Key.ToString(),
                        Kolicina = g.Sum(x => x.Stavka.Kolicina),
                        Cijena = g.Average(x => x.Stavka.Cijena),
                        Iznos = g.Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena)
                    })
                    .OrderByDescending(a => a.Iznos)
                    .ToList()
            );

            // ⭐ Ukupan promet
            UkupanPromet = ProdaniArtikli.Sum(a => a.Iznos);
            ReklamiraniPromet = ReklamiraniArtikli.Sum(a => a.Iznos);

            IsRadnik = false;

            OnPropertyChanged(nameof(ProdaniArtikli));
            OnPropertyChanged(nameof(ReklamiraniArtikli));
            OnPropertyChanged(nameof(UkupanPromet));
            OnPropertyChanged(nameof(ReklamiraniPromet));
            OnPropertyChanged(nameof(IsRadnik));
            OnPropertyChanged(nameof(ComboRadnikVisibility));
            OnPropertyChanged(nameof(GornjiGridHeight));
            OnPropertyChanged(nameof(DonjiGridHeight));

            ShowReport("PrometKuhinje");

            Debug.WriteLine("IsRadnik je: " + IsRadnik.ToString());
        }

        public void IzracunajDnevniPrometOstalo()
        {
            TrenutniIzvjestaj = TipIzvjestaja.Dnevni;
            PeriodicniPrometAktivan = false;
            ReportTitle = "Dnevni promet ostalo";
            var datum = OdDatuma.Date;


            var stavkeZaDatum = from racun in SviRacuni
                                join stavka in SveStavke on racun.BrojRacuna equals stavka.BrojRacuna
                                where racun.Datum.Date == datum
                                      && stavka.VrstaArtikla == 2
                                select new { Racun = racun, Stavka = stavka };

            // ⭐ Prodani artikli
            ProdaniArtikli.Clear();
            ProdaniArtikli = new ObservableCollection<ArtiklPromet>(
                stavkeZaDatum
                    .GroupBy(x => x.Stavka.Artikl)
                    .Select(g => new ArtiklPromet
                    {
                        NazivArtikla = g.Key.ToString(),
                        Kolicina = g.Sum(x => x.Stavka.Kolicina),
                        Cijena = g.Average(x => x.Stavka.Cijena),
                        Iznos = g.Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena)
                    })
                    .OrderByDescending(a => a.Iznos)
                    .ToList()
            );

            // ⭐ Reklamirani artikli
            ReklamiraniArtikli.Clear();
            ReklamiraniArtikli = new ObservableCollection<ArtiklPromet>(
                stavkeZaDatum
                    .Where(x => x.Racun.Reklamiran == "DA")
                    .GroupBy(x => x.Stavka.Artikl)
                    .Select(g => new ArtiklPromet
                    {
                        NazivArtikla = g.Key.ToString(),
                        Kolicina = g.Sum(x => x.Stavka.Kolicina),
                        Cijena = g.Average(x => x.Stavka.Cijena),
                        Iznos = g.Sum(x => x.Stavka.Kolicina * x.Stavka.Cijena)
                    })
                    .OrderByDescending(a => a.Iznos)
                    .ToList()
            );

            // ⭐ Ukupan promet
            UkupanPromet = ProdaniArtikli.Sum(a => a.Iznos);
            ReklamiraniPromet = ReklamiraniArtikli.Sum(a => a.Iznos);

            IsRadnik = false;

            OnPropertyChanged(nameof(ProdaniArtikli));
            OnPropertyChanged(nameof(ReklamiraniArtikli));
            OnPropertyChanged(nameof(UkupanPromet));
            OnPropertyChanged(nameof(ReklamiraniPromet));
            OnPropertyChanged(nameof(IsRadnik));
            OnPropertyChanged(nameof(ComboRadnikVisibility));
            OnPropertyChanged(nameof(GornjiGridHeight));
            OnPropertyChanged(nameof(DonjiGridHeight));

            ShowReport("PrometOstalo");

            Debug.WriteLine("IsRadnik je: " + IsRadnik.ToString());
        }

        public void IzracunajDnevniUtrosakNamirnica()
        {
            var datum = OdDatuma.Date;
            ReportTitle = "Dnevni utrošak namirnica";
            // Prodane stavke artikala iz kuhinje (VrstaArtikla = 1)
            var prodaneStavke =
                from racun in SviRacuni
                join stavka in SveStavke on racun.BrojRacuna equals stavka.BrojRacuna
                join artikal in ListaArtikala on stavka.Sifra equals artikal.Sifra
                where racun.Datum.Date == datum
                      && artikal.VrstaArtikla == 1   // kuhinja
                select new
                {
                    Stavka = stavka,
                    Artikal = artikal
                };

            var normativi = ListaNormativa;

            var utrosak =
               from ps in prodaneStavke
               join norm in normativi on ps.Artikal.IdArtikla equals norm.IdProizvoda
               select new
               {
                   norm.Repromaterijal,
                   norm.JedinicaMjere,
                   norm.Cijena,
                   Kolicina = (norm.Kolicina ?? 0) * ps.Stavka.Kolicina
               }
               into temp
               group temp by temp.Repromaterijal into g
               select new ArtiklPromet
               {
                   NazivArtikla = g.Key,
                   Kolicina = g.Sum(x => x.Kolicina),
                   Cijena = g.Sum(x => x.Cijena),
                   Iznos = g.Sum(x => x.Kolicina * x.Cijena)

               };
            UkupanPromet = utrosak.Sum(a => a.Iznos);
            ReklamiraniPromet = 0;
            ReklamiraniArtikli.Clear();
            ProdaniArtikli.Clear();
            ProdaniArtikli = new ObservableCollection<ArtiklPromet>(utrosak.OrderBy(x => x.NazivArtikla));

            OnPropertyChanged(nameof(UtrosakNamirnica));
            OnPropertyChanged(nameof(GornjiGridHeight));
            OnPropertyChanged(nameof(DonjiGridHeight));

            ShowReport("DnevniUtrosakNamirnica");
        }


        public void IzracunajPeriodicniUtrosakNamirnica()
        {
            PeriodicniPrometAktivan = true;
            var od = OdDatuma.Date;
            var dO = DoDatuma.Date;
            ReportTitle = "Periodični  utrošak namirnica" + Environment.NewLine + od.ToString("dd.MM.yy") + " - " + dO.ToString("dd.MM.yy");
            var prodaneStavke =
                from racun in SviRacuni
                join stavka in SveStavke on racun.BrojRacuna equals stavka.BrojRacuna
                join artikal in ListaArtikala on stavka.Sifra equals artikal.Sifra
                where racun.Datum.Date >= od && racun.Datum.Date <= dO
                      && artikal.VrstaArtikla == 1
                select new
                {
                    Stavka = stavka,
                    Artikal = artikal
                };

            var normativi = ListaNormativa;

            var utrosak =
                from ps in prodaneStavke
                join norm in normativi on ps.Artikal.IdArtikla equals norm.IdProizvoda
                select new
                {
                    norm.Repromaterijal,
                    norm.JedinicaMjere,
                    norm.Cijena,
                    Kolicina = (norm.Kolicina ?? 0) * ps.Stavka.Kolicina
                }
                into temp
                group temp by temp.Repromaterijal into g
                select new ArtiklPromet
                {
                    NazivArtikla = g.Key,
                    Kolicina = g.Sum(x => x.Kolicina),
                    Cijena = g.Sum(x => x.Cijena),
                    Iznos = g.Sum(x => x.Kolicina * x.Cijena)

                };
            UkupanPromet = utrosak.Sum(a => a.Iznos);
            ReklamiraniPromet = 0;
            ReklamiraniArtikli.Clear();
            ProdaniArtikli.Clear();
            ProdaniArtikli = new ObservableCollection<ArtiklPromet>(utrosak.OrderBy(x => x.NazivArtikla));

            OnPropertyChanged(nameof(UtrosakNamirnica));
            OnPropertyChanged(nameof(GornjiGridHeight));
            OnPropertyChanged(nameof(DonjiGridHeight));

            ShowReport("PeriodicniUtrosakNamirnica");
        }

        public void IzracunajZalihuNamirnica()
        {
            ReportTitle = "Zaliha namirnica na dan: " + OdDatuma.ToString("dd.MM.yyyy");

            // 1️⃣ Lista svih repromaterijala
            var sviRepromaterijali = ListaRepromaterijala; // ili mapirano iz tblRepromaterijal

            // 2️⃣ Ulazi do datuma
            var ulazi =
                from ulaz in SviUlaziNamirnica
                join stavka in SveStavkeUlazaNamirnica on ulaz.BrojUlaza equals stavka.BrojUlaza
                where ulaz.Datum.Date <= OdDatuma.Date
                group stavka by stavka.Artikl into g
                select new
                {
                    Artikl = g.Key,
                    Kolicina = g.Sum(x => x.Kolicina)
                };

            // 3️⃣ Utrošak do datuma
            var prodaneStavke =
                from racun in SviRacuni
                join stavka in SveStavke on racun.BrojRacuna equals stavka.BrojRacuna
                join artikal in ListaArtikala on stavka.Sifra equals artikal.Sifra
                where racun.Datum.Date <= OdDatuma.Date && artikal.VrstaArtikla == 1
                select new
                {
                    Stavka = stavka,
                    Artikal = artikal
                };

            var utrosak =
                from ps in prodaneStavke
                join norm in ListaNormativa on ps.Artikal.IdArtikla equals norm.IdProizvoda
                select new
                {
                    norm.Repromaterijal,
                    Kolicina = (norm.Kolicina ?? 0) * ps.Stavka.Kolicina
                }
                into temp
                group temp by temp.Repromaterijal into g
                select new
                {
                    Artikl = g.Key,
                    Kolicina = g.Sum(x => x.Kolicina)
                };

            // 4️⃣ Spajanje i izračunavanje zalihe
            var zaliha =
                from r in sviRepromaterijali
                join u in ulazi on r.Repromaterijal equals u.Artikl into gjU
                from u in gjU.DefaultIfEmpty()
                join t in utrosak on r.Repromaterijal equals t.Artikl into gjT
                from t in gjT.DefaultIfEmpty()
                select new ArtiklPromet
                {
                    NazivArtikla = r.Repromaterijal,
                    Kolicina = (u?.Kolicina ?? 0) - (t?.Kolicina ?? 0),
                    Cijena = r.NabavnaCijena,
                    Iznos = ((u?.Kolicina ?? 0) - (t?.Kolicina ?? 0)) * r.NabavnaCijena
                };

            // 5️⃣ Dodavanje u ObservableCollection
            UkupanPromet = zaliha.Sum(a => a.Iznos);
            ReklamiraniPromet = 0;
            ReklamiraniArtikli.Clear();
            ProdaniArtikli.Clear();
            foreach (var item in zaliha.OrderBy(x => x.NazivArtikla))
                ProdaniArtikli.Add(item);

            OnPropertyChanged(nameof(ProdaniArtikli));
            OnPropertyChanged(nameof(GornjiGridHeight));
            OnPropertyChanged(nameof(DonjiGridHeight));

            ShowReport("ZalihaNamirnica");
        }

        #region Filtriranje
        private void Filtrirano()
        {
            FiltriraniRacuni.Clear();
            FiltriraneStavke.Clear();

            // Filtriraj račune
            var filtriraniRacuni = SviRacuni.Where(r =>
                r.Datum >= OdDatuma && r.Datum <= DoDatuma &&
                (OdabraniRadnik == null || r.Radnik == OdabraniRadnik.IdRadnika.ToString())
            );

            foreach (var racun in filtriraniRacuni)
            {
                FiltriraniRacuni.Add(racun);
            }

           
            var filtriraneStavke = from stavka in SveStavke
                                   join racun in FiltriraniRacuni
                                   on stavka.BrojRacuna equals (int?)racun.BrojRacuna
                                   where OdabranaVrstaArtikla != null ||
                                         stavka.VrstaArtikla == OdabranaVrstaArtikla
                                   select stavka;

            foreach (var stavka in filtriraneStavke)
            {
                FiltriraneStavke.Add(stavka);
            }

            OnPropertyChanged(nameof(FiltriraniRacuni));
            OnPropertyChanged(nameof(FiltriraneStavke));
          
        }
        #endregion

        // Metoda za printanje dashboard izvještaja (pasteaj u istu klasu gdje imaš PrintReport)
        public void PrintDashboardReport(
            string reportType,
            ObservableCollection<ArtiklPromet> prodaniArtikli,
            ObservableCollection<ArtiklPromet> reklamiraniArtikli,
            DateTime odDatuma,
            DateTime? doDatuma = null)
        {
            FlowDocument doc = new FlowDocument
            {
                PageWidth = 700,
                PageHeight = 1122,
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontSize = 10,
                PagePadding = new Thickness(50)
            };

            // === ZAGLAVLJE (isti stil kao kod knjige šanka) ===
            Table headerTable = new Table();
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(200) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(200) });
            headerTable.Columns.Add(new TableColumn { Width = new GridLength(150) });

            TableRowGroup trg = new TableRowGroup();
            headerTable.RowGroups.Add(trg);
            TableRow row = new TableRow();
            trg.Rows.Add(row);

            // Lijevo: podaci o firmi
            Paragraph firmaPar = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 12
            };
            firmaPar.Inlines.Add(_firma + Environment.NewLine);
            firmaPar.Inlines.Add(_adresa + Environment.NewLine);
            firmaPar.Inlines.Add(_grad + Environment.NewLine);
            firmaPar.Inlines.Add("JIB: " +_jib + Environment.NewLine);
            firmaPar.Inlines.Add("PDV: " + _pdv);

            row.Cells.Add(new TableCell(firmaPar) { BorderThickness = new Thickness(0) });

            // Centar: naslov (dinamičan)
            Paragraph naslovPar = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 20,
                FontWeight = FontWeights.Bold
            };

            naslovPar.Inlines.Add(GetTitleForReportType(reportType));

            Paragraph datumPar = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeights.Medium
            };
            if (doDatuma == odDatuma)
                datumPar.Inlines.Add($"Za dan: {odDatuma:dd.MM.yyyy}");
           else
                datumPar.Inlines.Add($"Period: {odDatuma:dd.MM.yyyy} - {doDatuma:dd.MM.yyyy}");

            var centerCell = new TableCell
            {
                BorderThickness = new Thickness(0),
                TextAlignment = TextAlignment.Center
            };
            centerCell.Blocks.Add(naslovPar);
            centerCell.Blocks.Add(datumPar);
            row.Cells.Add(centerCell);

            // Desno: obrazac/oznaka
            Paragraph obrazacPar = new Paragraph
            {
                TextAlignment = TextAlignment.Right,
                FontSize = 10
            };
            //obrazacPar.Inlines.Add("Izvještaj");
            row.Cells.Add(new TableCell(obrazacPar) { BorderThickness = new Thickness(0) });

            doc.Blocks.Add(headerTable);
            doc.Blocks.Add(new Paragraph(new Run(" ")) { FontSize = 6 });

            // === Prva tabela: Prodani artikli ===
            doc.Blocks.Add(new Paragraph(new Run("PRODANI ARTIKLI"))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 6, 0, 4)
            });

            doc.Blocks.Add(BuildArtikliTableLikeOriginal(prodaniArtikli));

            // Razmak između tabela
            doc.Blocks.Add(new Paragraph(new Run(" ")) { FontSize = 6 });
            if (reklamiraniArtikli.Count > 0)
            {
                // === Druga tabela: Reklamirani artikli ===
                doc.Blocks.Add(new Paragraph(new Run("REKLAMIRANI ARTIKLI"))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 4)
                });

                doc.Blocks.Add(BuildArtikliTableLikeOriginal(reklamiraniArtikli));
            }
            // === POTPIS (kao u originalu) ===
            Paragraph footer = new Paragraph
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 0)
            };
            footer.Inlines.Add("POTPIS: __________________________");
            doc.Blocks.Add(footer);

            // === ŠTAMPA ===
            System.Windows.Controls.PrintDialog printDlg = new System.Windows.Controls.PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                IDocumentPaginatorSource idpSource = doc;
                printDlg.PrintDocument(idpSource.DocumentPaginator, GetTitleForReportType(reportType));
            }
        }

        // Pomoćna: vraća naslov na osnovu reportType
        private string GetTitleForReportType(string type)
        {
            switch (type)
            {
                case "DnevniPromet": return "DNEVNI PROMET";
                case "DnevniPrometPoRadniku": return "DNEVNI PROMET PO RADNIKU";
                case "DnevniPrometPoArtiklima": return "DNEVNI PROMET PO ARTIKLIMA";
                case "PeriodicniPromet": return "PERIODIČNI PROMET";
                case "PeriodicniPrometPoRadniku": return "PERIODIČNI PROMET PO RADNIKU";
                case "PeriodicniPrometPoArtiklima": return "PERIODIČNI PROMET PO ARTIKLIMA";
                case "PrometSanka": return "PROMET ŠANKA";
                case "PrometKuhinje": return "PROMET KUHINJE";
                case "PrometOstalo": return "OSTALI PROMET";
                case "DnevniUtrosakNamirnica": return "DNEVNI UTROŠAK NAMIRNICA";
                case "PeriodicniUtrosakNamirnica": return "PERIODIČNI UTROŠAK NAMIRNICA";
                case "ZalihaNamirnica": return "ZALIHA NAMIRNICA";
                default: return "IZVJEŠTAJ";
            }
        }

        // Pomoćna: gradi tabelu s istim stilom kao u PrintReport, i dodaje red UKUPNO (suma Iznosa)
        private Block BuildArtikliTableLikeOriginal(ObservableCollection<ArtiklPromet> lista)
        {
            Table table = new Table();
            table.CellSpacing = 0;

            // kolone (prilagodio širine, lako mijenjaš)
            table.Columns.Add(new TableColumn { Width = new GridLength(300) }); // Naziv
            table.Columns.Add(new TableColumn { Width = new GridLength(60) });  // Količina
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });  // Cijena
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });  // Iznos

            // Header
            TableRowGroup headerGroup = new TableRowGroup();
            table.RowGroups.Add(headerGroup);
            TableRow headerRow = new TableRow();
            headerGroup.Rows.Add(headerRow);

            string[] kolone = { "Naziv artikla", "Kol.", "Cijena", "Iznos" };
            foreach (var col in kolone)
            {
                TableCell cell = new TableCell(new Paragraph(new Run(col)))
                {
                    FontWeight = FontWeights.Medium,
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(4),
                    FontSize = 10,
                    BorderBrush = System.Windows.Media.Brushes.Gray,
                    BorderThickness = new Thickness(0.5)
                };
                headerRow.Cells.Add(cell);
            }

            // Body
            TableRowGroup bodyGroup = new TableRowGroup();
            table.RowGroups.Add(bodyGroup);

            decimal ukupno = 0m;
            if (lista != null)
            {
                foreach (var a in lista)
                {
                    TableRow r = new TableRow();
                    bodyGroup.Rows.Add(r);

                    r.Cells.Add(CreateCell(a.NazivArtikla ?? string.Empty, TextAlignment.Left));
                    r.Cells.Add(CreateCell((a.Kolicina.GetValueOrDefault()).ToString("F2"), TextAlignment.Right));
                    r.Cells.Add(CreateCell((a.Cijena.GetValueOrDefault()).ToString("F2"), TextAlignment.Right));
                    r.Cells.Add(CreateCell((a.Iznos.GetValueOrDefault()).ToString("F2"), TextAlignment.Right));

                    ukupno += a.Iznos.GetValueOrDefault();
                }
            }

            // Total red (zadnji)
            TableRow totalRow = new TableRow();
            bodyGroup.Rows.Add(totalRow);

            // Prvi cell: "UKUPNO:"
            totalRow.Cells.Add(new TableCell(new Paragraph(new Run("UKUPNO:")))
            {
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(4),
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                ColumnSpan = 3,
                TextAlignment = TextAlignment.Left
            });

            // Posljednji cell: suma
            totalRow.Cells.Add(new TableCell(new Paragraph(new Run(ukupno.ToString("F2"))))
            {
                TextAlignment = TextAlignment.Right,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(4),
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(0.5)
            });

            return table;
        }

        // Ako nema CreateCell u klasi, ovo je jednostavna implementacija koja se koristi gore
        private TableCell CreateCell(string text, TextAlignment align)
        {
            return new TableCell(new Paragraph(new Run(text)))
            {
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(4),
                TextAlignment = align
            };
        }


        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public class RekapitulacijaPrometa
        {
            public DateTime Datum { get; set; }

            // 1. OSTVAREN PROMET
            public decimal Gotovina { get; set; }
            public decimal Kartica { get; set; }
            public decimal Cek { get; set; }
            public decimal Virman { get; set; }
            public decimal Ukupno { get; set; }
            public decimal Reklamirano { get; set; }
            public decimal PazarUkupno => Ukupno - Reklamirano;
            public decimal PazarGotovina => Gotovina - (Reklamirano);

            // 2. PROMET PO RADNICIMA
            public List<RadnikPromet> PrometPoRadnicima { get; set; } = new();

            // 3. Šank
            public List<ArtiklPromet> SankProdano { get; set; } = new();
            public List<ArtiklPromet> SankReklamirano { get; set; } = new();
            public decimal SankUkupno { get; set; }

            // 4. Kuhinja
            public List<ArtiklPromet> KuhinjaProdano { get; set; } = new();
            public List<ArtiklPromet> KuhinjaReklamirano { get; set; } = new();
            public decimal KuhinjaUkupno { get; set; }

            // 5. Ostalo
            public List<ArtiklPromet> OstaloProdano { get; set; } = new();
            public List<ArtiklPromet> OstaloReklamirano { get; set; } = new();
            public decimal OstaloUkupno { get; set; }
        }

        public class RadnikPromet
        {
            public string RadnikId { get; set; }
            public string Radnik { get; set; }
            public decimal Gotovina { get; set; }
            public decimal Kartica { get; set; }
            public decimal Cek { get; set; }
            public decimal Virman { get; set; }
            public decimal Ukupno => Gotovina + Kartica + Cek + Virman;
            public decimal Reklamirano { get; set; }
        }

        public RekapitulacijaPrometa IzracunajRekapitulaciju(DateTime datum)
        {
            var r = new RekapitulacijaPrometa();
            r.Datum = datum.Date;

            var racuniZaDan = SviRacuni
                .Where(x => x.Datum.Date == datum.Date)
                .ToList();

            var radniciMap = SviRadnici.ToDictionary(x => x.IdRadnika, x => x.Radnik);
            foreach (var racun in racuniZaDan)
            {
                if (string.IsNullOrEmpty(racun.RadnikName))
                {
                    if (int.TryParse(racun.Radnik, out int radnikId))
                    {
                        if (radniciMap.TryGetValue(radnikId, out var ime))
                        {
                            racun.RadnikName = ime;
                        }
                        else
                        {
                            racun.RadnikName = racun.Radnik; // fallback na originalni string
                        }
                    }
                    else
                    {
                        // ne može parsirati, samo zadrži originalni string
                        racun.RadnikName = racun.Radnik;
                    }
                }
            }
            var stavkeZaDan =
                from rac in racuniZaDan
                join st in SveStavke on rac.BrojRacuna equals st.BrojRacuna
                select new { Racun = rac, Stavka = st };

            // 1. OSTVAREN PROMET
            r.Gotovina = stavkeZaDan
                .Where(x => x.Racun.NacinPlacanja == 0)
                .Sum(x => x.Stavka.Kolicina.GetValueOrDefault() * x.Stavka.Cijena.GetValueOrDefault());

            r.Kartica = stavkeZaDan
                .Where(x => x.Racun.NacinPlacanja == 1)
                .Sum(x => x.Stavka.Kolicina.GetValueOrDefault() * x.Stavka.Cijena.GetValueOrDefault());

            r.Cek = stavkeZaDan
                .Where(x => x.Racun.NacinPlacanja == 2)
                .Sum(x => x.Stavka.Kolicina.GetValueOrDefault() * x.Stavka.Cijena.GetValueOrDefault());

            r.Virman = stavkeZaDan
                .Where(x => x.Racun.NacinPlacanja == 3)
                .Sum(x => x.Stavka.Kolicina.GetValueOrDefault() * x.Stavka.Cijena.GetValueOrDefault());

            // Ukupno
            r.Ukupno = r.Gotovina + r.Kartica + r.Cek + r.Virman;

            // Reklamirano
            r.Reklamirano = stavkeZaDan
                .Where(x => x.Racun.Reklamiran == "DA")
                .Sum(x => x.Stavka.Kolicina.GetValueOrDefault() * x.Stavka.Cijena.GetValueOrDefault());

            // Pazar
         

            // -------------------------------
            // 2. PROMET PO RADNICIMA
            // -------------------------------
            var prometPoRadnicima = stavkeZaDan
                .GroupBy(x => x.Racun.Radnik)
                .Select(g =>
                {
                    var stavke = g.ToList();

                    var gotovina = stavke
                        .Where(s => s.Racun.NacinPlacanja == 0 && s.Racun.Reklamiran != "DA")
                        .Sum(s => s.Stavka.Kolicina.GetValueOrDefault() * s.Stavka.Cijena.GetValueOrDefault());

                    var kartica = stavke
                        .Where(s => s.Racun.NacinPlacanja == 1 && s.Racun.Reklamiran != "DA")
                        .Sum(s => s.Stavka.Kolicina.GetValueOrDefault() * s.Stavka.Cijena.GetValueOrDefault());

                    var cek = stavke
                        .Where(s => s.Racun.NacinPlacanja == 2 && s.Racun.Reklamiran != "DA")
                        .Sum(s => s.Stavka.Kolicina.GetValueOrDefault() * s.Stavka.Cijena.GetValueOrDefault());

                    var virman = stavke
                        .Where(s => s.Racun.NacinPlacanja == 3 && s.Racun.Reklamiran != "DA")
                        .Sum(s => s.Stavka.Kolicina.GetValueOrDefault() * s.Stavka.Cijena.GetValueOrDefault());

                    var reklamirano = stavke
                        .Where(s => s.Racun.Reklamiran == "DA")
                        .Sum(s => s.Stavka.Kolicina.GetValueOrDefault() * s.Stavka.Cijena.GetValueOrDefault());

                    return new RadnikPromet
                    {
                        RadnikId = g.Key,
                        Radnik = stavke.First().Racun.RadnikName ?? stavke.First().Racun.Radnik,
                        Gotovina = gotovina,
                        Kartica = kartica,
                        Cek = cek,
                        Virman = virman,
                        Reklamirano = reklamirano
                    };
                })
                .Where(r => r.Gotovina + r.Kartica + r.Cek + r.Virman > 0 || r.Reklamirano > 0)
                .ToList();

            r.PrometPoRadnicima = prometPoRadnicima;


            // helper metoda:
            List<ArtiklPromet> GetPromet(int vrsta, bool reklamirano)
            {
                // Početni query: sve stavke te vrste
                var q = from ps in stavkeZaDan
                        where ps.Stavka.VrstaArtikla == vrsta
                        select ps;

                // Ako tražimo reklamirano → filtriramo samo one s Reklamiran == "DA"
                if (reklamirano)
                    q = q.Where (ps => ps.Racun.Reklamiran == "DA");

                var result = (from ps in q
                              group ps by ps.Stavka.Artikl into g
                              select new ArtiklPromet
                              {
                                  NazivArtikla = g.Key,
                                  // Sumiramo sigurno nullable vrijednosti
                                  Kolicina = g.Sum (x => x.Stavka.Kolicina.GetValueOrDefault ()),
                                  Cijena = g.Any () ? (decimal?)Math.Round (g.Average (x => x.Stavka.Cijena.GetValueOrDefault ()), 2) : (decimal?)0m,
                                  Iznos = g.Sum (x => (x.Stavka.Kolicina.GetValueOrDefault () * x.Stavka.Cijena.GetValueOrDefault ()))
                              })
                             .OrderBy (x => x.NazivArtikla)
                             .ToList ();

                return result;
            }


            // ŠANK
            r.SankProdano = GetPromet(0, false);
            r.SankReklamirano = GetPromet(0, true);
            r.SankUkupno = r.SankProdano.Sum(x => x.Iznos ?? 0) -
                           r.SankReklamirano.Sum(x => x.Iznos ?? 0);

            // KUHINJA
            r.KuhinjaProdano = GetPromet(1, false);
            r.KuhinjaReklamirano = GetPromet(1, true);
            r.KuhinjaUkupno = r.KuhinjaProdano.Sum(x => x.Iznos ?? 0) -
                              r.KuhinjaReklamirano.Sum(x => x.Iznos ?? 0);

            // OSTALO
            r.OstaloProdano = GetPromet(2, false);
            r.OstaloReklamirano = GetPromet(2, true);
            r.OstaloUkupno = r.OstaloProdano.Sum(x => x.Iznos ?? 0) -
                             r.OstaloReklamirano.Sum(x => x.Iznos ?? 0);

            return r;
        }

      




        public async Task Print(DateTime datum)
        {
            await Task.Delay(1);
            rekapitulacija = IzracunajRekapitulaciju(datum);
            try
            {
                PrintDocument printDoc = new PrintDocument();
                string printerName = Properties.Settings.Default.POSPrinter;
                if (string.IsNullOrWhiteSpace (printerName))
                {
                    MessageBox.Show (
                        "POS printer nije podešen.\nMolimo izaberite printer u postavkama.",
                        "Printer nije definisan",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return; // prekini štampanje
                }
                printDoc.PrinterSettings.PrinterName = printerName;
                printDoc.PrintPage += GenerisiPosRekap;

                int widthHundredthsInch = (int)(_paperWidthMm / 25.4 * 100);
                PaperSize ps = new PaperSize("Custom", widthHundredthsInch - 15, 0);
                printDoc.DefaultPageSettings.PaperSize = ps;
                printDoc.DefaultPageSettings.Margins = new Margins(5, 5, 5, 5);
                //printDoc.OriginAtMargins = false;
                printDoc.Print();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Greška prilikom štampe: {ex.Message}", "Greška", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }


        }
        RekapitulacijaPrometa rekapitulacija;
        private async void GenerisiPosRekap(object sender, PrintPageEventArgs e)
        {
          
            if (e.Graphics != null)
            {
                Graphics g = e.Graphics;

                Font font = new Font("Consolas", 9);
                Font bold = new Font("Consolas", 10, System.Drawing.FontStyle.Bold);
                Font title = new Font ("Consolas", 14, System.Drawing.FontStyle.Bold);
                System.Drawing.Brush brush = System.Drawing.Brushes.Black; 
                int y = 0;
                int lineHeight = (int)font.GetHeight(g) + 2;
                int pageWidth = e.MarginBounds.Width;
                Debug.WriteLine("   int pageWidth = e.MarginBounds.Width;" + pageWidth);
                // LOGO
                if (_logo != null)
                {
                    int maxLogoHeight = 50;

                    // Izvorišna veličina slike
                    int originalWidth = _logo.Width;
                    int originalHeight = _logo.Height;

                    // Skaliraj sliku proporcionalno na max visinu
                    float scale = (float)maxLogoHeight / originalHeight;
                    int scaledWidth = (int)(originalWidth * scale);
                    int scaledHeight = (int)(originalHeight * scale);

                    // Centriraj sliku po širini papira
                    int x = (pageWidth - scaledWidth) / 2;

                    g.DrawImage(_logo, new Rectangle(x, y, scaledWidth, scaledHeight));
                    y += scaledHeight + 5;
                }

                int col1 = 0;
                int col2 = pageWidth - 120;
                int col3 = pageWidth - 60;
                int widthCol2 = 60;
                int widthCol3 = 60;


                //NASLOV//
                //
                string[] headerLines = new[]
                        {
                        _firma,
                        _adresa,
                        _grad,
                        "",
                        $"REKAPITULACIJA PROMETA",
                        "",
                        $"Datum: {DateTime.Now:dd.MM.yyyy HH:mm}",
                        
                    };

                foreach (var line in headerLines)
                {
                    var currentFont = (line == _firma || line == "REKAPITULACIJA PROMETA" ) ? title : font;
                    var textSize = g.MeasureString(line, currentFont);
                    float x = (pageWidth - textSize.Width) / 2;
                    g.DrawString(line, currentFont, brush, x, y);
                    y += lineHeight;
                }
                y += lineHeight;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y); 
                y += lineHeight;
             

                //PROMET UKUPNO
                //
                var textSizeTitlePromet = g.MeasureString("OSTVARENI PROMET", bold);
                float xTitlePromet = (pageWidth - textSizeTitlePromet.Width) / 2;
                g.DrawString("OSTVARENI PROMET", bold, brush, xTitlePromet, y);
                y += lineHeight*2;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                SizeF sizeGotovina = g.MeasureString(rekapitulacija.Gotovina.ToString("0.00") ?? "0.00", font);
                SizeF sizeKartica = g.MeasureString(rekapitulacija.Kartica.ToString("0.00") ?? "0.00", font);
                SizeF sizeCek = g.MeasureString(rekapitulacija.Cek.ToString("0.00") ?? "0.00", font);
                SizeF sizeVirman = g.MeasureString(rekapitulacija.Virman.ToString("0.00") ?? "0.00", font);
                SizeF sizeUkupno = g.MeasureString(rekapitulacija.PazarUkupno.ToString("0.00") ?? "0.00", font);
                SizeF sizeReklamirano = g.MeasureString(rekapitulacija.Reklamirano.ToString("0.00") ?? "0.00", font);
                SizeF sizePazar = g.MeasureString(rekapitulacija.PazarUkupno.ToString("0.00") ?? "0.00", font);
                SizeF sizePazarGotovina = g.MeasureString(rekapitulacija.PazarGotovina.ToString("0.00") ?? "0.00", font);

                Debug.WriteLine("sizeGotovina - " + sizeGotovina);
                Debug.WriteLine("sizeGKartica - " + sizeKartica);
                Debug.WriteLine("sizeCek - " + sizeCek);
                Debug.WriteLine("sizeVirman - " + sizeVirman);
                Debug.WriteLine("sizeReklamirano - " + sizeReklamirano);

                g.DrawString("Gotovina:", font, brush, col1, y);
                g.DrawString(rekapitulacija.Gotovina.ToString("0.00") ?? "0.00", font, brush, pageWidth - 10 - sizeGotovina.Width, y);
                y += lineHeight;
                g.DrawString("Kartica:", font, brush, col1, y);
                g.DrawString(rekapitulacija.Kartica.ToString("0.00") ?? "0.00", font, brush, pageWidth - 10 - sizeKartica.Width , y);
                y += lineHeight;
                g.DrawString("Ček:", font, brush, col1, y);
                g.DrawString(rekapitulacija.Cek.ToString("0.00") ?? "0.00", font, brush, pageWidth - 10 - sizeCek.Width , y);
                y += lineHeight;
                g.DrawString("Virman", font, brush, col1, y);
                g.DrawString(rekapitulacija.Virman.ToString("0.00") ?? "0.00", font, brush, pageWidth - 10 - sizeVirman.Width , y);
                y += lineHeight*2;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;
                g.DrawString("Prodano:", font, brush, col1, y);
                g.DrawString(rekapitulacija.Ukupno.ToString("0.00") ?? "0.00", font, brush, pageWidth - 10 - sizeUkupno.Width , y);
                y += lineHeight;
                g.DrawString("Reklamirano:", font, brush, col1, y);
                g.DrawString(rekapitulacija.Reklamirano.ToString("0.00") ?? "0.00", font, brush, pageWidth - 10 - sizeReklamirano.Width , y);
                y += lineHeight*2;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;
                g.DrawString("PAZAR UKUPNO::", bold, brush, col1, y);
                g.DrawString(rekapitulacija.PazarUkupno.ToString("0.00") ?? "0.00", bold, brush, pageWidth - 10 - sizePazar.Width , y);
                y += lineHeight*2;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y); 
                y += lineHeight;



                //PROMET PO RADNICIMA
                //

                var textSizeTitlePrometRadnici = g.MeasureString("PROMET PO RADNICIMA", bold);
                float xTitlePrometRadnici = (pageWidth - textSizeTitlePrometRadnici.Width) / 2;

                g.DrawString("PROMET PO RADNICIMA", bold, brush, xTitlePrometRadnici, y);
                y += lineHeight*2;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;
                foreach (var radnik in rekapitulacija.PrometPoRadnicima)
                {
                    string gotovina = radnik.Gotovina.ToString("0.00") ?? "0.00";
                    string kartica = radnik.Kartica.ToString("0.00") ?? "0.00";
                    string cek = radnik.Cek.ToString("0.00") ?? "0.00";
                    string virman = radnik.Virman.ToString("0.00") ?? "0.00";
                    string reklamirano = radnik.Reklamirano.ToString("0.00") ?? "0.00";
                    string ukupno = radnik.Ukupno.ToString("0.00") ?? "0.00";

                    SizeF sizeGotovinaRad = g.MeasureString(gotovina, font);
                    SizeF sizeKarticaRad = g.MeasureString(kartica, font);
                    SizeF sizeCekRad = g.MeasureString(cek, font);
                    SizeF sizeVirmanRad = g.MeasureString(virman, font);
                    SizeF sizeReklamiranoRad = g.MeasureString(virman, font);
                    SizeF sizePazarRad = g.MeasureString(ukupno, font);

                    Debug.WriteLine("sizeGotovinaRad - " + sizeGotovinaRad);
                    Debug.WriteLine("sizeGKarticaRad - " + sizeKarticaRad);
                    Debug.WriteLine("sizeCekRad - " + sizeCekRad);
                    Debug.WriteLine("sizeVirmanRad - " + sizeVirmanRad);
                    Debug.WriteLine("sizeReklamiranoRad - " + sizeReklamiranoRad);

                    g.DrawString(radnik.Radnik, bold, brush, col1, y);
                    y += lineHeight*2;
                    g.DrawString("Gotovina:", font, brush, col1, y);
                    g.DrawString(gotovina, font, brush, pageWidth - 10 - sizeGotovinaRad.Width , y);
                    y += lineHeight;
                    g.DrawString("Kartica:", font, brush, col1, y);
                    g.DrawString(kartica, font, brush, pageWidth - 10 - sizeKarticaRad.Width , y);
                    y += lineHeight;
                    g.DrawString("Ček:", font, brush, col1, y);
                    g.DrawString(cek, font, brush, pageWidth - 10 - sizeCekRad.Width , y);
                    y += lineHeight;
                    g.DrawString("Virman", font, brush, col1, y);
                    g.DrawString(virman, font, brush, pageWidth - 10 - sizeVirmanRad.Width , y);
                    y += lineHeight* 2;
                    g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                    y += lineHeight;
                    g.DrawString("UKUPNO", font, brush, col1, y);
                    g.DrawString(ukupno, font, brush, pageWidth - 10 - sizePazarRad.Width , y);
                    y += lineHeight * 2;
                    g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                    y += lineHeight;
                }


                string col2Title = "Cij.";
                string col3Title = "Iznos";
                string col1Title = "Kol.";
                SizeF sizeCol1Title = g.MeasureString (col1Title, bold);
                SizeF sizeCol2Title = g.MeasureString (col2Title, bold);
                SizeF sizeCol3Title = g.MeasureString (col3Title, bold);


                //PROMET ŠANK
                //

                var textSizeTitlePrometSank = g.MeasureString("PROMET ŠANK", bold);
                float xTitlePrometSank = (pageWidth - textSizeTitlePrometSank.Width) / 2;

                g.DrawString("PROMET ŠANK", bold, brush, xTitlePrometSank, y);
                y += lineHeight * 2;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                var textSizeTitleProdano = g.MeasureString ("Prodano", bold);
                float xTitleProdano = (pageWidth - textSizeTitleProdano.Width) / 2;

                g.DrawString ("Prodano", bold, brush, xTitleProdano, y);
                y += lineHeight * 2;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                g.DrawString("Artikal", bold, brush, col1, y);
                y += lineHeight;
                g.DrawString(col1Title, bold, brush, col1 + 60 - sizeCol1Title.Width, y );
                g.DrawString(col2Title, bold, brush, col2 + widthCol2 / 2 - sizeCol2Title.Width , y);
                g.DrawString(col3Title, bold, brush, pageWidth - 10 - sizeCol3Title.Width , y);
                y += lineHeight + 3 ;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y); ;
                y += lineHeight;
                foreach (var item in rekapitulacija.SankProdano)
                {
                    string naziv = item.NazivArtikla ?? "";
                    string kolicina = item.Kolicina?.ToString("0.00") ?? "";
                    string cijena = item.Cijena?.ToString("0.00") ?? "";
                    string iznos = item.Iznos?.ToString("0.00") ?? "";

                    g.DrawString(naziv, font, brush, col1, y); y += lineHeight;

                    // Desno poravnanje vrijednosti
                    SizeF sizeKolicina = g.MeasureString(kolicina, font);
                    SizeF sizeCijena = g.MeasureString(cijena, font);
                    SizeF sizeIznos = g.MeasureString(iznos, font);

                    g.DrawString(kolicina, font, brush, col1 + 60 - sizeKolicina.Width, y);
                    g.DrawString(cijena, font, brush, col2 + widthCol2 / 2 - sizeCijena.Width , y);
                    g.DrawString(iznos, font, brush, pageWidth - 10 - sizeIznos.Width , y);

                    y += lineHeight;
                }
                y += lineHeight;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

            

                var textSizeTitleRek = g.MeasureString ("Reklamirano", bold);
                float xTitleRek = (pageWidth - textSizeTitleRek.Width) / 2;

                g.DrawString ("Reklamirano", bold, brush, xTitleRek, y);
                y += lineHeight * 2;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;
                g.DrawString ("Artikal", bold, brush, col1, y);
                y += lineHeight;
                g.DrawString (col1Title, bold, brush, col1 + 60 - sizeCol1Title.Width, y );
                g.DrawString (col2Title, bold, brush, col2 + widthCol2 / 2 - sizeCol2Title.Width, y );
                g.DrawString (col3Title, bold, brush, pageWidth - 10 - sizeCol3Title.Width, y );
                y += lineHeight + 3;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;
                foreach (var item in rekapitulacija.SankReklamirano)
                {
                    string naziv = item.NazivArtikla ?? "";
                    string kolicina = item.Kolicina?.ToString ("0.00") ?? "";
                    string cijena = item.Cijena?.ToString ("0.00") ?? "";
                    string iznos = item.Iznos?.ToString ("0.00") ?? "";

                    g.DrawString (naziv, font, brush, col1, y); y += lineHeight;

                    // Desno poravnanje vrijednosti
                    SizeF sizeKolicina = g.MeasureString (kolicina, font);
                    SizeF sizeCijena = g.MeasureString (cijena, font);
                    SizeF sizeIznos = g.MeasureString (iznos, font);

                    g.DrawString (kolicina, font, brush, col1 + 60 - sizeKolicina.Width, y);
                    g.DrawString (cijena, font, brush, col2 + widthCol2 / 2 - sizeCijena.Width, y);
                    g.DrawString (iznos, font, brush, pageWidth - 10 - sizeIznos.Width, y);

                    y += lineHeight;
                }
                y += lineHeight;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                string totalStr = $"Ukupno: {rekapitulacija.SankUkupno:0.00} KM";
                SizeF sizeTotal = g.MeasureString (totalStr, bold);
                g.DrawString (totalStr, bold, brush, pageWidth - 10 - sizeTotal.Width - 5, y);
                y += lineHeight * 2;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                //PROMET KUHINJA
                //

                var textSizeTitlePrometKuhinja= g.MeasureString("PROMET KUHINJA", bold);
                float xTitlePrometKuhinja = (pageWidth - textSizeTitlePrometKuhinja.Width) / 2;

                g.DrawString("PROMET KUHINJA", bold, brush, xTitlePromet, y);
                y += lineHeight * 2;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                g.DrawString ("Prodano", bold, brush, xTitleProdano, y);
                y += lineHeight * 2;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                g.DrawString("Artikal", bold, brush, col1, y);
                y += lineHeight;
                g.DrawString(col1Title, bold, brush, col1 + 60 - sizeCol1Title.Width, y );
                g.DrawString(col2Title, bold, brush, col2 + widthCol2 / 2 - sizeCol2Title.Width , y );
                g.DrawString(col3Title, bold, brush, pageWidth - 10 - sizeCol3Title.Width , y );
                y += lineHeight + 3;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y); y += 3;
                y += lineHeight;
                foreach (var item in rekapitulacija.KuhinjaProdano)
                {
                    string naziv = item.NazivArtikla ?? "";
                    string kolicina = item.Kolicina?.ToString("0.00") ?? "";
                    string cijena = item.Cijena?.ToString("0.00") ?? "";
                    string iznos = item.Iznos?.ToString("0.00") ?? "";

                    g.DrawString(naziv, font, brush, col1, y); y += lineHeight;

                    // Desno poravnanje vrijednosti
                    SizeF sizeKolicina = g.MeasureString(kolicina, font);
                    SizeF sizeCijena = g.MeasureString(cijena, font);
                    SizeF sizeIznos = g.MeasureString(iznos, font);

                    g.DrawString(kolicina, font, brush, col1 + 60 - sizeKolicina.Width, y);
                    g.DrawString(cijena, font, brush, col2 + widthCol2 / 2 - sizeCijena.Width , y);
                    g.DrawString(iznos, font, brush, pageWidth - 10 - sizeIznos.Width , y);

                    y += lineHeight;
                }
                y += lineHeight;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                g.DrawString ("Reklamirano", bold, brush, xTitleRek, y);
                y += lineHeight * 2;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                g.DrawString ("Artikal", bold, brush, col1, y);
                y += lineHeight;
                g.DrawString (col1Title, bold, brush, col1 + 60 - sizeCol1Title.Width, y);
                g.DrawString (col2Title, bold, brush, col2 + widthCol2 / 2 - sizeCol2Title.Width, y);
                g.DrawString (col3Title, bold, brush, pageWidth - 10 - sizeCol3Title.Width, y );
                y += lineHeight + 3;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;
                foreach (var item in rekapitulacija.KuhinjaReklamirano)
                {
                    string naziv = item.NazivArtikla ?? "";
                    string kolicina = item.Kolicina?.ToString ("0.00") ?? "";
                    string cijena = item.Cijena?.ToString ("0.00") ?? "";
                    string iznos = item.Iznos?.ToString ("0.00") ?? "";

                    g.DrawString (naziv, font, brush, col1, y); y += lineHeight;

                    // Desno poravnanje vrijednosti
                    SizeF sizeKolicina = g.MeasureString (kolicina, font);
                    SizeF sizeCijena = g.MeasureString (cijena, font);
                    SizeF sizeIznos = g.MeasureString (iznos, font);

                    g.DrawString (kolicina, font, brush, col1 + 60 - sizeKolicina.Width, y);
                    g.DrawString (cijena, font, brush, col2 + widthCol2 / 2 - sizeCijena.Width, y);
                    g.DrawString (iznos, font, brush, pageWidth - 10 - sizeIznos.Width, y);

                    y += lineHeight;
                }
                y += lineHeight;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                string totalStrKuhinja = $"Ukupno: {rekapitulacija.KuhinjaUkupno:0.00} KM";
                SizeF sizeTotalKuhinja = g.MeasureString(totalStrKuhinja, bold);
                g.DrawString(totalStrKuhinja, bold, brush, pageWidth - 10 - sizeTotalKuhinja.Width - 5, y);
                y += lineHeight*2;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                //PROMET Ostalo
                //

                var textSizeTitlePrometOstalo = g.MeasureString("PROMET OSTALO", bold);
                float xTitlePrometOstalo = (pageWidth - textSizeTitlePrometOstalo.Width) / 2;

                g.DrawString("PROMET OSTALO", bold, brush, xTitlePromet, y);
                y += lineHeight*2;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;


                g.DrawString ("Prodano", bold, brush, xTitleProdano, y);
                y += lineHeight * 2;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;


                g.DrawString("Artikal", bold, brush, col1, y);
                y += lineHeight;
                g.DrawString(col1Title, bold, brush, col1 + 60 - sizeCol1Title.Width, y);
                g.DrawString(col2Title, bold, brush, col2 + widthCol2 / 2 - sizeCol2Title.Width , y);
                g.DrawString(col3Title, bold, brush, pageWidth - 10 - sizeCol3Title.Width , y );
                y += lineHeight +3;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y); y += 3;
                y += lineHeight ;
                foreach (var item in rekapitulacija.OstaloProdano)
                {
                    string naziv = item.NazivArtikla ?? "";
                    string kolicina = item.Kolicina?.ToString("0.00") ?? "";
                    string cijena = item.Cijena?.ToString("0.00") ?? "";
                    string iznos = item.Iznos?.ToString("0.00") ?? "";

                    g.DrawString(naziv, font, brush, col1, y); y += lineHeight;

                    // Desno poravnanje vrijednosti
                    SizeF sizeKolicina = g.MeasureString(kolicina, font);
                    SizeF sizeCijena = g.MeasureString(cijena, font);
                    SizeF sizeIznos = g.MeasureString(iznos, font);

                    g.DrawString(kolicina, font, brush, col1 + 60 - sizeKolicina.Width, y);
                    g.DrawString(cijena, font, brush, col2 + widthCol2 / 2 - sizeCijena.Width , y);
                    g.DrawString(iznos, font, brush, pageWidth - 10 - sizeIznos.Width , y);

                    y += lineHeight;
                }
                y += lineHeight;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                g.DrawString ("Reklamirano", bold, brush, xTitleRek, y);
                y += lineHeight * 2;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                g.DrawString ("Artikal", bold, brush, col1, y);
                y += lineHeight;
                g.DrawString (col1Title, bold, brush, col1 + 60 - sizeCol1Title.Width, y);
                g.DrawString (col2Title, bold, brush, col2 + widthCol2 / 2 - sizeCol2Title.Width, y);
                g.DrawString (col3Title, bold, brush, pageWidth - 10 - sizeCol3Title.Width, y);
                y += lineHeight +3;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y); y += 3;

                foreach (var item in rekapitulacija.OstaloReklamirano)
                {
                    string naziv = item.NazivArtikla ?? "";
                    string kolicina = item.Kolicina?.ToString ("0.00") ?? "";
                    string cijena = item.Cijena?.ToString ("0.00") ?? "";
                    string iznos = item.Iznos?.ToString ("0.00") ?? "";

                    g.DrawString (naziv, font, brush, col1, y); y += lineHeight;

                    // Desno poravnanje vrijednosti
                    SizeF sizeKolicina = g.MeasureString (kolicina, font);
                    SizeF sizeCijena = g.MeasureString (cijena, font);
                    SizeF sizeIznos = g.MeasureString (iznos, font);

                    g.DrawString (kolicina, font, brush, col1 + 60 - sizeKolicina.Width, y);
                    g.DrawString (cijena, font, brush, col2 + widthCol2 / 2 - sizeCijena.Width, y);
                    g.DrawString (iznos, font, brush, pageWidth - 10 - sizeIznos.Width, y);

                    y += lineHeight;
                }
                y += lineHeight;
                g.DrawLine (Pens.Black, 0, y, pageWidth, y);
                y += lineHeight;

                string totalStrOstalo = $"Ukupno: {rekapitulacija.OstaloUkupno:0.00} KM";
                SizeF sizeTotalOstalo = g.MeasureString(totalStrOstalo, bold);
                g.DrawString(totalStrOstalo, bold, brush, pageWidth - 10 - sizeTotalOstalo.Width - 5, y);
                y += lineHeight*2;
                g.DrawLine(Pens.Black, 0, y, pageWidth, y);


            }
        }

    }
}
