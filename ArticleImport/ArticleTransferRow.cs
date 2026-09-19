using Caupo.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ArticleImport
{
    public sealed class ArticleTransferRow : INotifyPropertyChanged
    {
        private readonly List<TblKategorije> _categories;

        private string _vrsta = string.Empty;
        private string _kategorija = string.Empty;
        private string _jedinica = string.Empty;
        private string _normativ = "1";
        private string _slika = string.Empty;
        private string _cijena = string.Empty;
        private bool _forceEditNormativ;

        public ArticleTransferRow(IEnumerable<TblKategorije> categories)
        {
            _categories = categories.ToList();
        }

        
        public string Sifra { get; set; } = string.Empty;
        public string InternaSifra { get; set; } = string.Empty;
        public string Artikl { get; set; } = string.Empty;
        public string Cijena
        {
            get => _cijena;
            set
            {
                if (_cijena == value)
                    return;

                _cijena = value;
                OnPropertyChanged();
            }
        }

        public string Vrsta
        {
            get => _vrsta;
            set
            {
                if (_vrsta == value)
                    return;

                _vrsta = value;

                if(_vrsta != "Piće")
                {
                    Jedinica = "kom";
                    Normativ = "1";
                }
                else if(string.Equals (Jedinica, "kom", StringComparison.OrdinalIgnoreCase))
                {
                    Normativ = "1";
                }

                OnPropertyChanged ();
                OnPropertyChanged(nameof(CanEditNormativ));
                RefreshCategories();
            }
        }

        public string Kategorija
        {
            get => _kategorija;
            set
            {
                if (_kategorija == value)
                    return;

                _kategorija = value;
                OnPropertyChanged();
            }
        }

        public string Jedinica
        {
            get => _jedinica;
            set
            {
                if (_jedinica == value)
                    return;

                _jedinica = value;

                if (string.Equals(_jedinica, "kom", StringComparison.OrdinalIgnoreCase))
                    Normativ = "1";

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanEditNormativ));
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                    return;

                _isSelected = value;
                OnPropertyChanged();
            }
        }

        private string _porez = string.Empty;

        public string Porez
        {
            get => _porez;
            set
            {
                if(_porez == value)
                    return;

                _porez = value;
                OnPropertyChanged ();
            }
        }

        public string Normativ
        {
            get => _normativ;
            set
            {
                if (_normativ == value)
                    return;

                _normativ = value;
                OnPropertyChanged();
            }
        }

        public string Pozicija { get; set; } = string.Empty;
        public bool Aktivan { get; set; } = true;

        private bool _porezNaPotrosnju;
        public bool PorezNaPotrosnju
        {
            get => _porezNaPotrosnju;
            set
            {
                if(_porezNaPotrosnju == value)
                    return;

                _porezNaPotrosnju = value;
                OnPropertyChanged ();
            }
        }

        public string Slika
        {
            get => _slika;
            set
            {
                if (_slika == value)
                    return;

                _slika = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SlikaZaPrikaz));
            }
        }

        public string SlikaZaPrikaz
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Slika) && File.Exists(Slika))
                    return Slika;

                return "pack://application:,,,/Images/noimage.png";
            }
        }

        public bool CanEditNormativ => _forceEditNormativ || (Vrsta == "Piće" && !string.Equals(Jedinica, "kom", StringComparison.OrdinalIgnoreCase));

        public HashSet<string> DefaultedFields { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> ErrorFields { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> WarningFields { get; } = new(StringComparer.OrdinalIgnoreCase);

       

        public bool SifraError => ErrorFields.Contains("Šifra");
        public bool SifraWarning => WarningFields.Contains("Šifra");

        public bool InternaSifraError => ErrorFields.Contains("Interna šifra");
        public bool InternaSifraWarning => WarningFields.Contains("Interna šifra");

        public bool ArtiklError => ErrorFields.Contains("Naziv");
        public bool ArtiklWarning => WarningFields.Contains("Naziv");

        public bool CijenaError => ErrorFields.Contains("Cijena");
        public bool CijenaWarning => WarningFields.Contains("Cijena");

        public bool VrstaError => ErrorFields.Contains("Vrsta");
        public bool VrstaWarning => WarningFields.Contains("Vrsta");

        public bool KategorijaError => ErrorFields.Contains("Kategorija");
        public bool KategorijaWarning => WarningFields.Contains("Kategorija");

        public bool JedinicaError => ErrorFields.Contains("Jedinica");
        public bool JedinicaWarning => WarningFields.Contains("Jedinica");

        public bool PorezError => ErrorFields.Contains("Porez");
        public bool PorezWarning => WarningFields.Contains("Porez");

        public bool NormativError => ErrorFields.Contains("Normativ");
        public bool NormativWarning => WarningFields.Contains("Normativ");

        public bool PozicijaError => ErrorFields.Contains("Pozicija");
        public bool PozicijaWarning => WarningFields.Contains("Pozicija");

        public void RefreshValidationState()
        {
           

            OnPropertyChanged(nameof(SifraError));
            OnPropertyChanged(nameof(SifraWarning));

            OnPropertyChanged(nameof(InternaSifraError));
            OnPropertyChanged(nameof(InternaSifraWarning));

            OnPropertyChanged(nameof(ArtiklError));
            OnPropertyChanged(nameof(ArtiklWarning));

            OnPropertyChanged(nameof(CijenaError));
            OnPropertyChanged(nameof(CijenaWarning));

            OnPropertyChanged(nameof(VrstaError));
            OnPropertyChanged(nameof(VrstaWarning));

            OnPropertyChanged(nameof(KategorijaError));
            OnPropertyChanged(nameof(KategorijaWarning));

            OnPropertyChanged(nameof(JedinicaError));
            OnPropertyChanged(nameof(JedinicaWarning));

            OnPropertyChanged(nameof(PorezError));
            OnPropertyChanged(nameof(PorezWarning));

            OnPropertyChanged(nameof(NormativError));
            OnPropertyChanged(nameof(NormativWarning));

            OnPropertyChanged(nameof(PozicijaError));
            OnPropertyChanged(nameof(PozicijaWarning));
        }

        public void EnableNormativCorrection()
        {
            _forceEditNormativ = true;
            OnPropertyChanged(nameof(CanEditNormativ));
        }

        public ObservableCollection<string> AvailableCategories { get; } = new();

        public void RefreshCategories()
        {
            string currentCategory = Kategorija;
            AvailableCategories.Clear();

            int? typeId = Vrsta switch
            {
                "Piće" => 0,
                "Hrana" => 1,
                "Ostalo" => 2,
                _ => null
            };

            if (typeId.HasValue)
            {
                foreach (var category in _categories.Where(x => x.VrstaArtikla == typeId.Value).OrderBy(x => x.IdKategorije))
                    AvailableCategories.Add(category.ToString());
            }

            if (!string.IsNullOrWhiteSpace(currentCategory) && AvailableCategories.Contains(currentCategory))
                Kategorija = currentCategory;
            else
                Kategorija = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}