//using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace Caupo.Data
{
    public class DatabaseTables
    {
        [Table ("tblArtikli")]
        public class TblArtikli : INotifyPropertyChanged
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdArtikla { get; set; }
            public string? Sifra { get; set; }
            public string? Artikl { get; set; }
            public override string ToString()
            {
                return Artikl ?? string.Empty;
            }
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
            public decimal? Cijena { get; set; }
            public string? InternaSifra { get; set; }
            public int? PoreskaStopa { get; set; }
            [NotMapped]
            [JsonIgnore]
            public decimal? PoreskaStopaPostotak
            {
                get
                {
                    return PoreskaStopa switch
                    {
                        1 => 0,
                        2 => 17,
                        3 => 0,
                        4 => 0,
                        _ => 0
                    };
                }
            }
            public string? Slika { get; set; }

            [NotMapped]
            [JsonIgnore]
            public ImageSource SlikaSource
            {
                get
                {
                    if(string.IsNullOrWhiteSpace (Slika)|| Slika == "bez_slike")
                        return null; // ili pack URI placeholder slike
                    try
                    {
                        return new BitmapImage (new Uri (Slika, UriKind.Absolute));
                    }
                    catch
                    {
                        return null; // ne postoji fajl ili krivi path
                    }
                }
            }
            public decimal? Normativ { get; set; }
            public int? VrstaArtikla { get; set; }
            [NotMapped]
            [JsonIgnore]
            public string? VrstaArtiklaName
            {
                get
                {
                    return VrstaArtikla switch
                    {
                        0 => "Piće",
                        1 => "Hrana",
                        2 => "Ostalo",
                        _ => "Nepoznata vrsta"
                    };
                }
            }
            public int? Kategorija { get; set; }
            public int? Pozicija { get; set; }
            public string? ArtiklNormativ { get; set; }
            public string? PrikazatiNaDispleju { get; set; }
            [NotMapped]
            [JsonIgnore]
            public decimal ButtonWidth { get; set; }

            private bool _isVisible = true;
            [NotMapped]
            [JsonIgnore]
            public bool IsVisible
            {
                get => _isVisible;
                set
                {
                    _isVisible = value;
                    OnPropertyChanged (nameof (IsVisible));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
            }
        }

        [Table ("tblBrojBlokaSank")]
        public class TblBrojBlokaSank
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public int BrojBloka { get; set; }
        }
        [Table ("tblBrojPokretanja")]
        public class TblBrojPokretanja
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int BrojPokretanja { get; set; }
            public string? DatumPokretanja { get; set; }
        }
        [Table ("tblDobavljaci")]

        public class TblDobavljaci : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            protected void OnPropertyChanged(string name)
                => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));

            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdDobavljaca { get; set; }

            private string? _dobavljac;
            public string? Dobavljac
            {
                get => _dobavljac;
                set
                {
                    if(_dobavljac != value)
                    {
                        _dobavljac = value;
                        OnPropertyChanged (nameof (Dobavljac));
                    }
                }
            }

            public override string ToString() => Dobavljac ?? string.Empty;

            private string? _adresa;
            public string? Adresa
            {
                get => _adresa;
                set
                {
                    if(_adresa != value)
                    {
                        _adresa = value;
                        OnPropertyChanged (nameof (Adresa));
                    }
                }
            }

            private string? _mjesto;
            public string? Mjesto
            {
                get => _mjesto;
                set
                {
                    if(_mjesto != value)
                    {
                        _mjesto = value;
                        OnPropertyChanged (nameof (Mjesto));
                    }
                }
            }

            private string? _pdv;
            public string? PDV
            {
                get => _pdv;
                set
                {
                    if(_pdv != value)
                    {
                        _pdv = value;
                        OnPropertyChanged (nameof (PDV));
                    }
                }
            }

            private string? _jib;
            public string? JIB
            {
                get => _jib;
                set
                {
                    if(_jib != value)
                    {
                        _jib = value;
                        OnPropertyChanged (nameof (JIB));
                    }
                }
            }

            private string? _zr;
            public string? ZR
            {
                get => _zr;
                set
                {
                    if(_zr != value)
                    {
                        _zr = value;
                        OnPropertyChanged (nameof (ZR));
                    }
                }
            }

            private decimal? _pocetnoStanje;
            public decimal? PocetnoStanje
            {
                get => _pocetnoStanje;
                set
                {
                    if(_pocetnoStanje != value)
                    {
                        _pocetnoStanje = value;
                        OnPropertyChanged (nameof (PocetnoStanje));
                    }
                }
            }
        }

        [Table ("tblFakturaStavka")]
        public class TblFakturaStavka
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdStavke { get; set; }
            [NotNull]
            public string? Artikl { get; set; }
            [NotNull]
            public decimal Kolicina { get; set; }
            [NotNull]
            public decimal Cijena { get; set; }
            public int JedinicaMjere { get; set; }

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
            public int IdFakture { get; set; }
        }
        [Table ("tblFaktura")]
        public class TblFaktura
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdFakture { get; set; }
            [NotNull]
            public DateTime Datum { get; set; }
            public string? Kupac { get; set; }
            public int NacinPlacanja { get; set; }
            public int Radnik { get; set; }
            public string? Racuni { get; set; }
            public string? Printed { get; set; }
        }
        [Table ("tblFirma")]
        public class TblFirma
        {
            [NotNull]
            public string? NazivFirme { get; set; }
            public string? Adresa { get; set; }
            public string? Grad { get; set; }
            public string? JIB { get; set; }
            public string? PDV { get; set; }
            public string? ZiroRacun { get; set; }
            public string? Telefon { get; set; }
            public string? Web { get; set; }
        }
        [Table ("tblJediniceMjere")]
        public class TblJediniceMjere
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdJedinice { get; set; }
            [NotNull]
            public string? JedinicaMjere { get; set; }
            public override string ToString()
            {
                return JedinicaMjere ?? string.Empty;
            }
        }
        [Table ("tblKategorije")]
        public class TblKategorije : INotifyPropertyChanged
        {

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string prop)
                => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (prop));

            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdKategorije { get; set; }

            private string? _kategorija;
            [NotNull]
            public string? Kategorija
            {
                get => _kategorija;
                set { _kategorija = value; OnPropertyChanged (nameof (Kategorija)); }
            }
            public override string ToString()
            {
                return Kategorija ?? string.Empty;
            }
            private int _vrstaArtikla;
            public int VrstaArtikla
            {
                get => _vrstaArtikla;
                set
                {
                    _vrstaArtikla = value;
                    OnPropertyChanged (nameof (VrstaArtikla));
                    OnPropertyChanged (nameof (VrstaArtiklaName)); // 👈 BITNO
                }
            }

            [NotMapped]
            public string? VrstaArtiklaName
            {
                get
                {
                    return VrstaArtikla switch
                    {
                        0 => "Piće",
                        1 => "Hrana",
                        2 => "Ostalo",
                        _ => "Nepoznata vrsta"
                    };
                }
            }
        }


        [Table ("tblKnjigaKuhinje")]
        public class TblKnjigaKuhinje
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int RedniBroj { get; set; }
            public string? Artikl { get; set; }
            public int JedinicaMjere { get; set; }
            public decimal? Ostatak { get; set; }
            public string? BrojFakture { get; set; }
            public string? Dobavljac { get; set; }
            public decimal? Primljeno { get; set; }
            public decimal? Stanje { get; set; }
            public decimal? Utroseno { get; set; }
            public decimal? Cijena { get; set; }
            public decimal? Zaliha { get; set; }
            public DateTime Datum { get; set; }
            public string? Sifra { get; set; }
        }
        [Table ("tblKnjigaSanka")]
        public class TblKnjigaSanka
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int RedniBroj { get; set; }
            public string? Artikl { get; set; }
            public int JedinicaMjere { get; set; }
            public decimal? Ostatak { get; set; }
            public decimal? Primljeno { get; set; }
            public decimal? Stanje { get; set; }
            public decimal? Utroseno { get; set; }
            public decimal? Cijena { get; set; }
            public decimal? Zaliha { get; set; }
            public DateTime Datum { get; set; }
            public string? Sifra { get; set; }
        }
        [Table ("tblKuhinja")]
        public class TblKuhinja
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdKuhinje { get; set; }
            public DateTime Datum { get; set; }
            public string? Sto { get; set; }
            public string? Radnik { get; set; }
            public string? NazivStola { get; set; }
        }
        [Table ("tblKuhinjaStavke")]
        public class TblKuhinjaStavke
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdStavke { get; set; }
            public string? Artikl { get; set; }
            public string? Sifra { get; set; }
            public decimal Kolicina { get; set; }
            public decimal Cijena { get; set; }
            public string? Zavrseno { get; set; }
            public int IdKuhinje { get; set; }
            public string? Note { get; set; }
        }
        [Table ("tblKupci")]
        public class TblKupci : INotifyPropertyChanged
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdKupca { get; set; }
            private string? kupac;
            public string? Kupac
            {
                get => kupac;
                set
                {
                    if(kupac != value)
                    {
                        kupac = value;
                        OnPropertyChanged (nameof (Kupac));
                    }
                }
            }

            private string? adresa;
            public string? Adresa
            {
                get => adresa;
                set
                {
                    if(adresa != value)
                    {
                        adresa = value;
                        OnPropertyChanged (nameof (Adresa));
                    }
                }
            }

            private string? mjesto;
            public string? Mjesto
            {
                get => mjesto;
                set
                {
                    if(mjesto != value)
                    {
                        mjesto = value;
                        OnPropertyChanged (nameof (Mjesto));
                    }
                }
            }

            private string? pdv;
            public string? PDV
            {
                get => pdv;
                set
                {
                    if(pdv != value)
                    {
                        pdv = value;
                        OnPropertyChanged (nameof (PDV));
                    }
                }
            }

            private string? jib;
            public string? JIB
            {
                get => jib;
                set
                {
                    if(jib != value)
                    {
                        jib = value;
                        OnPropertyChanged (nameof (JIB));
                    }
                }
            }


            public override string ToString()
            {
                return Kupac ?? string.Empty;
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged(string name) =>
                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));
        }
        [Table ("tblKupciNarudzba")]
        public class TblKupciNarudzba
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdNarudzbe { get; set; }
            public string? Kupac { get; set; }
            public string? JIB { get; set; }
            public DateTime Datum { get; set; }
            public string? Radnik { get; set; }
            public string? Fakturisano { get; set; }
        }
        [Table ("tblKupciNarudzbeStavka")]
        public class TblKupciNarudzbeStavka
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdStavke { get; set; }
            public string? Artikl { get; set; }
            public decimal Kolicina { get; set; }
            public decimal Cijena { get; set; }
            public int JedinicaMjere { get; set; }
            public int PoreskaStopa { get; set; }
            public string? ArtiklNormativ { get; set; }
            public int IdNarudzbe { get; set; }
        }

        [Table ("tblNarudzbe")]
        public class TblNarudzbe
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdNarudzbe { get; set; }
            public DateTime Datum { get; set; }
            public string? Vrijeme { get; set; }
            public string? Konobar { get; set; }
        }


        [Table ("tblNarudzbeStavke")]
        public class TblNarudzbeStavke : INotifyPropertyChanged
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdStavke { get; set; }

            public string? Name { get; set; }
            public string? Label { get; set; }

            public decimal? TotalAmount
            {
                get
                {
                    // Calculate Total based on Qty and Price
                    if(UnitPrice.HasValue && Quantity > 0)
                    {
                        return UnitPrice.Value * Quantity;
                    }
                    return null;
                }
            }

            private decimal? unitPrice;
            public decimal? UnitPrice
            {
                get => unitPrice;
                set
                {
                    if(unitPrice != value)
                    {
                        unitPrice = value;
                        OnPropertyChanged (nameof (UnitPrice));
                        OnPropertyChanged (nameof (TotalAmount)); // Notify that Total might have changed
                    }
                }
            }
            private decimal? _Quantity;
            public decimal? Quantity
            {
                get => _Quantity;
                set
                {
                    if(_Quantity != value)
                    {
                        _Quantity = value;
                        OnPropertyChanged (nameof (Quantity));
                        OnPropertyChanged (nameof (TotalAmount));
                    }
                }
            }
            public int? BrojRacuna { get; set; }
            public string? Sifra { get; set; }
            public int? Proizvod { get; set; }
            public int? JedinicaMjere { get; set; }
            public string? Naziv { get; set; }
            public string? Printed { get; set; }
            public string? Konobar { get; set; }
            public int? IdNarudzbe { get; set; }

            public string? Sala { get; set; }
            public event PropertyChangedEventHandler? PropertyChanged;

            protected void OnPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }

        [Table ("tblNormativPica")]
        public class TblNormativPica
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdNormativa { get; set; }
            public string? Normativ { get; set; }
            public override string ToString()
            {
                return Normativ ?? string.Empty;
            }
        }
        [Table ("tblNormativi")]
        public class TblNormativ
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdStavkeNormativa { get; set; }
            public string? Repromaterijal { get; set; }
            public string? JedinicaMjere { get; set; }
            public decimal? Kolicina { get; set; }
            public decimal? Cijena { get; set; }
            public decimal? Ukupno => Kolicina * Cijena;
            public int IdProizvoda { get; set; }
        }
        [Table ("tblOtpis")]
        public class TblOtpis
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int idIdOtpisa { get; set; }
            public int BrojOtpisa { get; set; }
            public DateTime Datum { get; set; }
            public string? Locked { get; set; }
            public string? Radnik { get; set; }
        }
        [Table ("tblOtpisStavka")]
        public class TblOtpisStavka
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int RedniBroj { get; set; }
            public int BrojOtpisa { get; set; }
            public string? Artikl { get; set; }
            public decimal Kolicina { get; set; }
            public decimal Cijena { get; set; }
            public int JedinicaMjere { get; set; }
        }
        [Table ("tblPoreskeStope")]
        public class TblPoreskeStope
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdStope { get; set; }
            public decimal? Postotak { get; set; }
            public string? Opis { get; set; }
            public override string ToString()
            {
                return Opis ?? string.Empty;
            }
        }
        [Table ("tblRacunStavke")]
        public class TblRacunStavka : INotifyPropertyChanged
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdStavke { get; set; }
            public int? BrojRacuna { get; set; }
            public string? Artikl { get; set; }
            public string? Sifra { get; set; }

            private decimal? _kolicina;
            public decimal? Kolicina
            {
                get => _kolicina;
                set
                {
                    if(_kolicina != value)
                    {
                        _kolicina = value;
                        OnPropertyChanged (nameof (Kolicina));
                        OnPropertyChanged (nameof (Iznos));
                    }
                }
            }
            private decimal? cijena;
            public decimal? Cijena
            {
                get => cijena;
                set
                {
                    if(cijena != value)
                    {
                        cijena = value;
                        OnPropertyChanged (nameof (Cijena));
                        OnPropertyChanged (nameof (Iznos)); // Notify that Total might have changed
                    }
                }
            }
            public decimal? Iznos
            {
                get
                {
                    // Calculate Total based on Qty and Price
                    if(Cijena.HasValue && Kolicina > 0)
                    {
                        return Cijena.Value * (decimal)Kolicina;
                    }
                    return null;
                }
            }
            public int? PoreskaStopa { get; set; }
            [NotMapped]
            [JsonIgnore]
            public decimal? PoreskaStopaPostotak
            {
                get
                {
                    return PoreskaStopa switch
                    {
                        1 => 0,
                        2 => 17,
                        3 => 0,
                        4 => 0,
                        _ => 0
                    };
                }
            }
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
            public int? VrstaArtikla { get; set; }
            public string? ArtiklNormativ { get; set; }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected void OnPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }
        [Table ("tblRacuni")]
        public class TblRacuni : INotifyPropertyChanged
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int BrojRacuna { get; set; }
            public DateTime Datum { get; set; }
            public string? Kupac { get; set; }
            public int NacinPlacanja { get; set; }

            [NotMapped]
            [JsonIgnore]
            public string? NacinPlacanjaName
            {
                get
                {
                    return NacinPlacanja switch
                    {
                        0 => "Gotovinski",
                        1 => "Kartica",
                        2 => "Ček",
                        3 => "Virman",
                        _ => ""
                    };
                }
            }
            public string? BrojFiskalnogRacuna { get; set; }
            public string? Radnik { get; set; }
            [NotMapped]
            public string? RadnikName { get; set; }
            public string? Fiskalizovan { get; set; } = "NE";

            private string? reklamiran = "NE";
            public string? Reklamiran
            {
                get => reklamiran;
                set
                {
                    if(reklamiran != value)
                    {
                        reklamiran = value;
                        OnPropertyChanged (nameof (Reklamiran));
                        OnPropertyChanged (nameof (IsReklamiran));
                    }
                }
            }
            [NotMapped]
            public bool IsReklamiran => Reklamiran == "DA";
            public string? Fakturisan { get; set; } = "NE";

            public string? Jir { get; set; } 
            public string? Zki { get; set; }
            public string? BrojRacunaHr { get; set; }
            [NotMapped]
            [JsonIgnore]
            public decimal? Iznos { get; set; }
            [NotMapped]
            [JsonIgnore]
            public string? KupacId { get; set; }


            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged(string name) =>
                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (name));
        }

        [Table ("tblRadnici")]
        public class TblRadnici
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdRadnika { get; set; }
            public string? Radnik { get; set; }
            public override string ToString()
            {
                return Radnik ?? string.Empty;
            }
            public string? Lozinka { get; set; }
            public string? Dozvole { get; set; }
            public string? IB { get; set; }

        }

        [Table ("tblReklamiraniRacuni")]
        public class TblReklamiraniRacun
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int BrojRekRacuna { get; set; }
            public int BrojRacuna { get; set; }
            public string? BrojFiskalnogRacuna { get; set; }
            public DateTime Datum { get; set; }
            public int NacinPlacanja { get; set; }
            public string? Kupac { get; set; }
            public string? Fiskalizovan { get; set; } = "NE";
            public string? Radnik { get; set; }
        }
        [Table ("tblReklamiraniStavke")]
        public class TblReklamiraniStavka
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdStavke { get; set; }
            public int BrojReklamiranogRacuna { get; set; }
            public string? Artikl { get; set; }
            public string? Sifra { get; set; }
            public decimal Kolicina { get; set; }
            public decimal Cijena { get; set; }
            public int BrojRacuna { get; set; }
            public int JedinicaMjere { get; set; }
            public int IdProizvoda { get; set; }
            public int Proizvod { get; set; }
        }
        [Table ("tblRepromaterijal")]
        public class TblRepromaterijal
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int? IdRepromaterijala { get; set; }
            public string? Repromaterijal { get; set; }
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
            public decimal? NabavnaCijena { get; set; }
            public decimal? PlanskaCijena { get; set; }
            public decimal? Zaliha { get; set; }
        }
        [Table ("tblUlaz")]
        public class TblUlaz
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdUlaza { get; set; }
            public int BrojUlaza { get; set; }
            public DateTime Datum { get; set; }
            public string? Dobavljac { get; set; }
            public string? BrojFakture { get; set; }
            public decimal IznosFakture { get; set; }
            public string? Locked { get; set; }
            public string? Radnik { get; set; }
            [NotMapped]
            [JsonIgnore]
            public string? RadnikName { get; set; }
        }
        [Table ("tblUlazRepromaterijal")]
        public class TblUlazRepromaterijal
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdUlaza { get; set; }
            public int BrojUlaza { get; set; }
            public DateTime Datum { get; set; }
            public string? Dobavljac { get; set; }
            public string? BrojFakture { get; set; }
            public string? Locked { get; set; }
            public string? Radnik { get; set; }
            [NotMapped]
            [JsonIgnore]
            public string? RadnikName { get; set; }
        }
        [Table ("tblUlazRepromaterijalStavke")]
        public class TblUlazRepromaterijalStavka
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int RedniBroj { get; set; }
            public int BrojUlaza { get; set; }
            public string? Artikl { get; set; }

            private decimal _kolicina;
            public decimal Kolicina
            {
                get => _kolicina;
                set
                {
                    if(_kolicina != value)
                    {
                        _kolicina = value;
                        OnPropertyChanged (nameof (Kolicina));
                        OnPropertyChanged (nameof (MPVrijednost));
                        OnPropertyChanged (nameof (IznosIPDV));
                        OnPropertyChanged (nameof (ProdajnaVrijBezIPDV));
                        OnPropertyChanged (nameof (FakVrBezPDV));
                        OnPropertyChanged (nameof (PDVMarza));
                        OnPropertyChanged (nameof (RazlikaCijBezPDV));
                        OnPropertyChanged (nameof (MarzaPostotak));
                    }
                }
            }

            private decimal _cijenaBezUPDV;
            public decimal CijenaBezUPDV
            {
                get => _cijenaBezUPDV;
                set
                {
                    if(_cijenaBezUPDV != value)
                    {
                        _cijenaBezUPDV = value;
                        OnPropertyChanged (nameof (CijenaBezUPDV));
                        OnPropertyChanged (nameof (FakVrBezPDV));

                    }
                }
            }

            public decimal IznosBezUPDV { get; set; }

            private decimal _rabat;
            public decimal Rabat
            {
                get => _rabat;
                set
                {
                    if(_rabat != value)
                    {
                        _rabat = value;
                        OnPropertyChanged (nameof (Rabat));

                    }
                }
            }

            public int? PoreskaStopa { get; set; }

            [NotMapped]
            public decimal? PoreskaStopaPostotak => PoreskaStopa switch
            {
                1 => 0,
                2 => 17,
                3 => 0,
                4 => 0,
                _ => 0
            };

            public decimal IznosUPDVa { get; set; }
            public decimal CijenaSaUPDV { get; set; }

            public decimal? Cijena { get; set; }

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
            // ---------------------
            // Kalkulacije koje nisu u bazi
            // ---------------------

            [NotMapped]
            public decimal MPVrijednost => Kolicina * (Cijena ?? CijenaBezUPDV);

            [NotMapped]
            public decimal IPDV => (Cijena ?? CijenaBezUPDV) * 0.1453m;

            [NotMapped]
            public decimal IznosIPDV => Kolicina * IPDV;

            [NotMapped]
            public decimal ProdajnaVrijBezIPDV => MPVrijednost - IznosIPDV;

            [NotMapped]
            public decimal FakVrBezPDV => CijenaBezUPDV * Kolicina;

            [NotMapped]
            public decimal NabavnaCijenaBezPDV => CijenaBezUPDV;
            [NotMapped]
            public decimal NabavnaVrijednostBezPDV => FakVrBezPDV;

            [NotMapped]
            public decimal PDVMarza => (MPVrijednost - NabavnaVrijednostBezPDV) * 0.1453m;

            [NotMapped]
            public decimal RazlikaCijBezPDV => MPVrijednost - NabavnaVrijednostBezPDV - PDVMarza;




            [NotMapped]
            public decimal MarzaPostotak => (ProdajnaVrijBezIPDV - FakVrBezPDV) * 100 / ProdajnaVrijBezIPDV;

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }

        [Table ("tblUlazStavke")]
        public class TblUlazStavke : INotifyPropertyChanged
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int RedniBroj { get; set; }
            public int BrojUlaza { get; set; }
            public string? Artikl { get; set; }

            private decimal _kolicina;
            public decimal Kolicina
            {
                get => _kolicina;
                set
                {
                    if(_kolicina != value)
                    {
                        _kolicina = value;
                        OnPropertyChanged (nameof (Kolicina));
                        OnPropertyChanged (nameof (MPVrijednost));
                        OnPropertyChanged (nameof (IznosIPDV));
                        OnPropertyChanged (nameof (ProdajnaVrijBezIPDV));
                        OnPropertyChanged (nameof (FakVrBezPDV));
                        OnPropertyChanged (nameof (PDVMarza));
                        OnPropertyChanged (nameof (RazlikaCijBezPDV));
                        OnPropertyChanged (nameof (MarzaPostotak));
                    }
                }
            }

            private decimal _cijenaBezUPDV;
            public decimal CijenaBezUPDV
            {
                get => _cijenaBezUPDV;
                set
                {
                    if(_cijenaBezUPDV != value)
                    {
                        _cijenaBezUPDV = value;
                        OnPropertyChanged (nameof (CijenaBezUPDV));
                        OnPropertyChanged (nameof (FakVrBezPDV));

                    }
                }
            }

            public decimal IznosBezUPDV { get; set; }

            private decimal _rabat;
            public decimal Rabat
            {
                get => _rabat;
                set
                {
                    if(_rabat != value)
                    {
                        _rabat = value;
                        OnPropertyChanged (nameof (Rabat));

                    }
                }
            }

            public int? PoreskaStopa { get; set; }

            [NotMapped]
            public decimal? PoreskaStopaPostotak => PoreskaStopa switch
            {
                1 => 0,
                2 => 17,
                3 => 0,
                4 => 0,
                _ => 0
            };

            public decimal IznosUPDVa { get; set; }
            public decimal CijenaSaUPDV { get; set; }
            public string? Sifra { get; set; }
            public decimal? Cijena { get; set; }
            public int NivelacijaID { get; set; }
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
            public int? VrstaArtikla { get; set; }

            // ---------------------
            // Kalkulacije koje nisu u bazi
            // ---------------------
            [NotMapped]
            public bool ObracunPDV { get; set; }

            [NotMapped]
            public decimal MPVrijednost => Kolicina * (Cijena ?? CijenaBezUPDV);

            [NotMapped]
            public decimal IPDV => ObracunPDV ? (Cijena ?? CijenaBezUPDV) * 0.1453m : 0;


            [NotMapped]
            public decimal IznosIPDV => Kolicina * IPDV;

            [NotMapped]
            public decimal ProdajnaVrijBezIPDV => MPVrijednost - IznosIPDV;

            [NotMapped]
            public decimal FakVrBezPDV => CijenaBezUPDV * Kolicina;

            [NotMapped]
            public decimal NabavnaCijenaBezPDV => CijenaBezUPDV;
            [NotMapped]
            public decimal NabavnaVrijednostBezPDV => FakVrBezPDV;

            [NotMapped]
            public decimal PDVMarza => ObracunPDV ? (MPVrijednost - NabavnaVrijednostBezPDV) * 0.1453m : 0;

            [NotMapped]
            public decimal RazlikaCijBezPDV => MPVrijednost - NabavnaVrijednostBezPDV - PDVMarza;

            [NotMapped]
            public decimal MarzaPostotak => (ProdajnaVrijBezIPDV - FakVrBezPDV) * 100 / ProdajnaVrijBezIPDV;

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }


        [Table ("tblUplateDobavljacima")]
        public class TblUplateDobavljacima
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdUplate { get; set; }
            public DateTime Datum { get; set; }
            public string? Dobavljac { get; set; }
            public decimal Iznos { get; set; }
        }
        [Table ("tblUplateKupaca")]
        public class TblUplateKupaca
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdUplate { get; set; }
            public DateTime Datum { get; set; }
            public string? Kupac { get; set; }
            public string? Faktura { get; set; }
            public decimal Iznos { get; set; }
            public string? Radnik { get; set; }
        }

        [Table ("NarudzbeStavke")]
        public class NarudzbeStavke : INotifyPropertyChanged
        {
            [Key]
            [DatabaseGenerated (DatabaseGeneratedOption.Identity)]
            public int IdStavke { get; set; }
            public string? Artikl { get; set; }
            public string? Sifra { get; set; }

            private decimal _kolicina;
            public decimal Kolicina
            {
                get => _kolicina;
                set
                {
                    if(_kolicina != value)
                    {
                        _kolicina = value;
                        OnPropertyChanged (nameof (Kolicina));
                        OnPropertyChanged (nameof (Iznos));
                    }
                }
            }
            private decimal? cijena;
            public decimal? Cijena
            {
                get => cijena;
                set
                {
                    if(cijena != value)
                    {
                        cijena = value;
                        OnPropertyChanged (nameof (Cijena));
                        OnPropertyChanged (nameof (Iznos)); // Notify that Total might have changed
                    }
                }
            }
            public decimal? Iznos
            {
                get
                {
                    // Calculate Total based on Qty and Price
                    if(Cijena.HasValue && Kolicina > 0)
                    {
                        return Cijena.Value * Kolicina;
                    }
                    return null;
                }
            }
            public int? PoreskaStopa { get; set; }
            public int? JedinicaMjere { get; set; }
            public int? VrstaArtikla { get; set; }
            public string? ArtiklNormativ { get; set; }
            public string? IdStola { get; set; }
            public string? Konobar { get; set; }
            public int Tura { get; set; }
            public event PropertyChangedEventHandler? PropertyChanged;

            protected void OnPropertyChanged(string propertyName) =>
                PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

        }


    }
}
