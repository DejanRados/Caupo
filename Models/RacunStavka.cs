using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Caupo.Models
{
    public class RacunStavka : INotifyPropertyChanged
    {
        private decimal? _quantity;
        private decimal? _unitPrice;
        private string? _note;
        private string? _printed;

        // ============================================================
        // LOKALNI / DOMENSKI PODACI
        // ============================================================

        public int? ArtiklId { get; set; }

        public int? BrojRacuna { get; set; }

        public string? Name { get; set; }

        public string? Sifra { get; set; }

        public string? Naziv { get; set; }


        // ============================================================
        // KOLIČINA
        // ============================================================

        public decimal? Quantity
        {
            get => _quantity;
            set
            {
                if(_quantity == value)
                    return;

                _quantity = value;

                OnPropertyChanged ();
                OnPropertyChanged (
                    nameof (TotalAmount));
            }
        }


        // ============================================================
        // CIJENA
        // ============================================================

        public decimal? UnitPrice
        {
            get => _unitPrice;
            set
            {
                if(_unitPrice == value)
                    return;

                _unitPrice = value;

                OnPropertyChanged ();
                OnPropertyChanged (
                    nameof (TotalAmount));
            }
        }


        // ============================================================
        // UKUPNO
        // ============================================================

        public decimal? TotalAmount
        {
            get
            {
                if(!UnitPrice.HasValue ||
                   !Quantity.HasValue)
                {
                    return null;
                }

                return Math.Round (
                    UnitPrice.Value *
                    Quantity.Value,
                    2,
                    MidpointRounding.AwayFromZero);
            }
        }


        // ============================================================
        // PORESKA STOPA
        //
        // OVDJE SE ČUVA LOKALNI ID PORESKE STOPE.
        //
        // NEMA Labels.
        // Regionalni fiskalni mapper pretvara ovaj ID u:
        // Srbija / RS / Hrvatska / Federacija oznaku.
        // ============================================================

        public int? PoreskaStopa { get; set; }


        // ============================================================
        // JEDINICA MJERE
        // ============================================================

        public int? JedinicaMjere { get; set; }

        public string JedinicaMjereName
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


        // ============================================================
        // 0 = ŠANK
        // 1 = KUHINJA
        // ============================================================

        public int? Proizvod { get; set; }


        // ============================================================
        // STANJE PRINTANJA BLOKA
        //
        // Ovo nije stanje fiskalnog računa.
        // Koristi se za kuhinju / šank.
        // ============================================================

        public string? Printed
        {
            get => _printed;
            set
            {
                if(_printed == value)
                    return;

                _printed = value;

                OnPropertyChanged ();
            }
        }


        // ============================================================
        // NAPOMENA
        // ============================================================

        public string? Note
        {
            get => _note;
            set
            {
                if(_note == value)
                    return;

                _note = value;

                OnPropertyChanged ();
            }
        }


        // ============================================================
        // PROPERTY CHANGED
        // ============================================================

        public event PropertyChangedEventHandler?
            PropertyChanged;


        protected virtual void OnPropertyChanged(
            [CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke (
                this,
                new PropertyChangedEventArgs (
                    propertyName));
        }
    }
}