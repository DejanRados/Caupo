using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Properties;
using Caupo.Views;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;
using Brush = System.Windows.Media.Brush;



namespace Caupo.ViewModels
{
    public class KasaViewModel : INotifyPropertyChanged
    {
        private Brush? _fontColor;
        public Brush? FontColor
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

        private Brush? _backColor;
        public Brush? BackColor
        {
            get { return _backColor; }
            set
            {
                if (_backColor != value)
                {
                    _backColor = value;
                    OnPropertyChanged(nameof(BackColor));
                }
            }
        }

        private string? _imagePathPiceButton;
        public string? ImagePathPiceButton
        {
            get { return _imagePathPiceButton; }
            set
            {
                _imagePathPiceButton = value;
                OnPropertyChanged(nameof(ImagePathPiceButton));
            }
        }

      

        private string? _imagePathHranaButton;
        public string? ImagePathHranaButton
        {
            get { return _imagePathHranaButton; }
            set
            {
                _imagePathHranaButton = value;
                OnPropertyChanged(nameof(ImagePathHranaButton));
            }
        }

        private string? _imagePathCategoryButton;


        private string? _imagePathOstaloButton;
        public string? ImagePathOstaloButton
        {
            get { return _imagePathOstaloButton; }
            set
            {
                _imagePathOstaloButton = value;
                OnPropertyChanged (nameof (ImagePathOstaloButton));
            }
        }

        private string? _imagePathPiceSelectedButton;
        public string? ImagePathPiceSelectedButton
        {
            get { return _imagePathPiceSelectedButton; }
            set
            {
                _imagePathPiceSelectedButton = value;
                OnPropertyChanged (nameof (ImagePathPiceSelectedButton));
            }
        }



        private string? _imagePathHranaSelectedButton;
        public string? ImagePathHranaSelectedButton
        {
            get { return _imagePathHranaSelectedButton; }
            set
            {
                _imagePathHranaSelectedButton = value;
                OnPropertyChanged (nameof (ImagePathHranaSelectedButton));
            }
        }



        private string? _imagePathOstaloSelectedButton;
        public string? ImagePathOstaloSelectedButton
        {
            get { return _imagePathOstaloSelectedButton; }
            set
            {
                _imagePathOstaloSelectedButton = value;
                OnPropertyChanged (nameof (ImagePathOstaloSelectedButton));
            }
        }

        public string? ImagePathCategoryButton
        {
            get { return _imagePathCategoryButton; }
            set
            {
                _imagePathCategoryButton = value;
                OnPropertyChanged(nameof(ImagePathCategoryButton));
            }
        }



        private decimal? _totalSum;
        public decimal? TotalSum
        {
            get => _totalSum;
            set
            {
                if (_totalSum != value)
                {
                    _totalSum = value;
                    OnPropertyChanged(nameof(TotalSum));
                }
            }
        }

        public ObservableCollection<FiskalniRacun.Item>? _stavkeRacuna;
        public ObservableCollection<FiskalniRacun.Item>? StavkeRacuna
        {
            get => _stavkeRacuna;
            set
            {
                _stavkeRacuna = value;
                OnPropertyChanged(nameof(StavkeRacuna));
                
            }
        }

        private ObservableCollection<TblArtikli>? _artikliPice;
        public ObservableCollection<TblArtikli>? ArtikliPice
        {
            get => _artikliPice;
            set
            {
                _artikliPice = value;
                OnPropertyChanged(nameof(ArtikliPice));
            }
        }

        private ObservableCollection<TblArtikli>? _artikliHrana;
        public ObservableCollection<TblArtikli>? ArtikliHrana
        {
            get => _artikliHrana;
            set
            {
                _artikliHrana = value;
                OnPropertyChanged(nameof(ArtikliHrana));
            }
        }

        private ObservableCollection<TblArtikli>? _artikliOstalo;
        public ObservableCollection<TblArtikli>? ArtikliOstalo
        {
            get => _artikliOstalo;
            set
            {
                _artikliOstalo = value;
                OnPropertyChanged(nameof(ArtikliOstalo));
            }
        }

        private ObservableCollection<TblArtikli>? _artikliPiceFilter;
        public ObservableCollection<TblArtikli>? ArtikliPiceFilter
        {
            get => _artikliPiceFilter;
            set
            {
                _artikliPiceFilter = value;
                OnPropertyChanged(nameof(ArtikliPiceFilter));
            }
        }

        private ObservableCollection<TblArtikli>? _artikliHranaFilter;
        public ObservableCollection<TblArtikli>? ArtikliHranaFilter
        {
            get => _artikliHranaFilter;
            set
            {
                _artikliHranaFilter = value;
                OnPropertyChanged(nameof(ArtikliHranaFilter));
            }
        }

        private ObservableCollection<TblArtikli>? _artikliOstaloFilter;
        public ObservableCollection<TblArtikli>? ArtikliOstaloFilter
        {
            get => _artikliOstaloFilter;
            set
            {
                _artikliOstaloFilter = value;
                OnPropertyChanged(nameof(ArtikliOstaloFilter));
            }
        }

        private ObservableCollection<TblKategorije>? _kategorijePice;
        public ObservableCollection<TblKategorije>? KategorijePice
        {
            get => _kategorijePice;
            set
            {
                _kategorijePice = value;
                OnPropertyChanged(nameof(KategorijePice));
            }
        }

        private ObservableCollection<TblKategorije>? _kategorijeHrana;
        public ObservableCollection<TblKategorije>? KategorijeHrana
        {
            get => _kategorijeHrana;
            set
            {
                _kategorijeHrana = value;
                OnPropertyChanged(nameof(KategorijeHrana));
            }
        }

        private ObservableCollection<TblKategorije>? _kategorijeOstalo;
        public ObservableCollection<TblKategorije>? KategorijeOstalo
        {
            get => _kategorijeOstalo;
            set
            {
                _kategorijeOstalo = value;
                OnPropertyChanged(nameof(KategorijeOstalo));
            }
        }

        private ObservableCollection<TblKupci>? _kupci;
        public ObservableCollection<TblKupci>? Kupci
        {
            get => _kupci;
            set
            {
                _kupci = value;
                OnPropertyChanged(nameof(Kupci));
               
               // if (_kupci != null && _kupci.Count > 0)
               // {
               //     SelectedKupac = _kupci[0];  
               // }
            }
        }

        private FiskalniRacun.Item? _selectedStavka;
        public FiskalniRacun.Item? SelectedStavka
        {
            get { return _selectedStavka; }
            set
            {
                _selectedStavka = value;
                OnPropertyChanged(nameof(SelectedStavka));
            }
        }
        
        private TblKupci? _selectedKupac;
        public TblKupci? SelectedKupac
        {
            get { return _selectedKupac; }
            set
            {
                _selectedKupac = value;
                OnPropertyChanged(nameof(SelectedKupac));
                if (_selectedKupac != null)
                    Debug.WriteLine("SelectedKupac promijenjen: " + _selectedKupac.Kupac);
            }
        }

        private bool _isMultiUser = false;
        public bool IsMultiUser
        {
            get => _isMultiUser;
            set
            {
                if (_isMultiUser != value)
                {
                    _isMultiUser = value;

                    OnPropertyChanged (nameof (IsMultiUser));
                    OnPropertyChanged (nameof (IsMultiUserVisible));
                    UpdateMultiUserState ();
                }
            }
        }

        private bool _isLoggedIn = false;
        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set
            {
                if (_isLoggedIn != value)
                {
                    _isLoggedIn = value;
                    OnPropertyChanged (nameof (IsLoggedIn));
                    OnPropertyChanged (nameof (IsMultiUserVisible));
                    UpdateMultiUserState ();
                }
            }
        }

        public int pokusaj = 3;
        public bool IsMultiUserVisible =>
    IsMultiUser && !IsLoggedIn;


        private void UpdateMultiUserState()
        {
            if (IsMultiUserVisible)
            {
                pokusaj = 3;
                Debug.WriteLine ("IsMultiUserVisible = TRUE → pokusaj postavljen na 3");
            }
        }


        public enum Category { Pice, Hrana, Ostalo }

        private Category _selectedCategory;
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged (nameof (SelectedCategory));
            }
        }

        public KasaViewModel()
        {
            ArtikliPice = new ObservableCollection<TblArtikli>();
            ArtikliHrana = new ObservableCollection<TblArtikli>();
            ArtikliOstalo = new ObservableCollection<TblArtikli>();
            ArtikliPiceFilter = new ObservableCollection<TblArtikli>();
            ArtikliHranaFilter = new ObservableCollection<TblArtikli>();
            ArtikliOstaloFilter = new ObservableCollection<TblArtikli>();
            KategorijePice = new ObservableCollection<TblKategorije>();
            KategorijeHrana = new ObservableCollection<TblKategorije>();
            KategorijeOstalo = new ObservableCollection<TblKategorije>();
            Kupci = new ObservableCollection<TblKupci>();
            StavkeRacuna = new ObservableCollection<FiskalniRacun.Item>();

           
            _=Start();
          
       /*     foreach (var item in StavkeRacuna)
            {
                item.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(item.Quantity) || e.PropertyName == nameof(item.UnitPrice))
                    {
                        UpdateTotalSum();
                    }
                };
            }
            UpdateTotalSum();*/
           
        }

        public async Task Start()
        {
           
            await SetImage();
            await  CheckMultiUser();
            await  LoadArtikliAsync();
            await  LoadCategoriesAsync();
            await LoadKupciAsync();
            Debug.WriteLine("SelectedKupac u startu poslije load = " + SelectedKupac.Kupac);


        }

      

        public async Task CheckMultiUser()
        {
           
            try
            { 
            string multiuser  = Settings.Default.MultiUser;
                Debug.WriteLine("multiuser: " + multiuser);
                if (!string.IsNullOrEmpty(multiuser))
                {
                    if (multiuser == "DA")
                    {
                        Debug.WriteLine("multiuser == DA" );
                        IsMultiUser = true;
                     
                    }
                    else
                    {
                        Debug.WriteLine("multiuser == NE");
                        IsMultiUser = false;
                    }
                }


            }
            catch (Exception ex)
            {
                IsMultiUser = false;
                Debug.WriteLine(ex.ToString());
            }
        }
        public async Task SetImage()
        {
            await Task.Delay(5);
            string tema = Settings.Default.Tema;
            Debug.WriteLine("Aktivna tema koju vidi viewmodel je : " + tema);
            if (tema == "Tamna")
            {
                ImagePathPiceButton = "pack://application:,,,/Images/Dark/drink.svg";
                ImagePathHranaButton = "pack://application:,,,/Images/Dark/food.svg";
                ImagePathOstaloButton = "pack://application:,,,/Images/Dark/another.svg";
                ImagePathPiceSelectedButton = "pack://application:,,,/Images/Light/drink.svg";
                ImagePathHranaSelectedButton = "pack://application:,,,/Images/Light/food.svg";
                ImagePathOstaloSelectedButton = "pack://application:,,,/Images/Light/another.svg";
                ImagePathCategoryButton = "pack://application:,,,/Images/Dark/category.svg";
                FontColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212)); 
                Application.Current.Resources["GlobalFontColor"] =    FontColor;
                BackColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));


            }
            else
            {
                ImagePathPiceButton = "pack://application:,,,/Images/Light/drink.svg";
                ImagePathHranaButton = "pack://application:,,,/Images/Light/food.svg";
                ImagePathOstaloButton = "pack://application:,,,/Images/Light/another.svg";
                ImagePathPiceSelectedButton = "pack://application:,,,/Images/Dark/drink.svg";
                ImagePathHranaSelectedButton = "pack://application:,,,/Images/Dark/food.svg";
                ImagePathOstaloSelectedButton = "pack://application:,,,/Images/Dark/another.svg";
                ImagePathCategoryButton = "pack://application:,,,/Images/Light/category.svg";
                FontColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));
                Application.Current.Resources["GlobalFontColor"] =    FontColor;
                BackColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212));
                //FontColorAdv = new System.Windows.Media.Color();
                //FontColorAdv = System.Windows.Media.Color.FromRgb(50, 50, 50);
            }


        }


        public async Task LoadArtikliAsync()
        {
            try
            {
               
                using (var db = new AppDbContext())
                {
                    var artikli = await db.Artikli.ToListAsync();
                    var pice = artikli.Where(a => a.VrstaArtikla == 0).OrderBy(a => a.Pozicija).ToList();
                    var hrana = artikli.Where(a => a.VrstaArtikla == 1).OrderBy(a => a.Pozicija).ToList();
                    var ostalo = artikli.Where(a => a.VrstaArtikla == 2).OrderBy(a => a.Pozicija).ToList();
                    ArtikliPice?.Clear();
                    ArtikliPiceFilter?.Clear();
                    foreach (var artikl in pice)
                    {
                        ArtikliPice?.Add(artikl);
                        ArtikliPiceFilter?.Add(artikl);
                        // Debug.WriteLine(artikl.Artikl + ", Artikli Count = " + Artikli.Count);
                    }
                    ArtikliHrana?.Clear();
                    ArtikliHranaFilter?.Clear();
                    foreach (var artikl in hrana)
                    {
                        ArtikliHrana?.Add(artikl);
                        ArtikliHranaFilter?.Add(artikl);
                        // Debug.WriteLine(artikl.Artikl + ", Artikli Count = " + Artikli.Count);
                    }
                    ArtikliOstalo?.Clear();
                    ArtikliOstaloFilter?.Clear();
                    foreach (var artikl in ostalo)
                    {
                        ArtikliOstalo?.Add(artikl);
                        ArtikliOstaloFilter?.Add(artikl);
                        // Debug.WriteLine(artikl.Artikl + ", Artikli Count = " + Artikli.Count);
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public async Task LoadCategoriesAsync()
        {
            try
            {
               
                using (var db = new AppDbContext())
                {
                    var Categories = await db.Kategorije.ToListAsync();
                    KategorijePice?.Clear();
                    KategorijeHrana?.Clear();
                    KategorijeOstalo?.Clear();
                    Debug.WriteLine("Categories.Count: "+Categories.Count);
                    foreach (var kategorija in Categories)
                    {
                        if(kategorija.VrstaArtikla == 0)
                        {
                            KategorijePice?.Add(kategorija);
                        }
                       if(kategorija.VrstaArtikla == 1)
                        {
                            KategorijeHrana?.Add(kategorija);
                        }
                        if(kategorija.VrstaArtikla == 2)
                        {
                            KategorijeOstalo?.Add(kategorija);
                        }
                      

                    }
                    
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        public async Task LoadKupciAsync()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Dohvati samo one kupce koji nemaju NULL vrijednost za "Kupac"
                    var kupci = await db.Kupci
                        .Where(k => !string.IsNullOrEmpty(k.Kupac)) // Provjera za NULL ili praznu vrijednost
                        .ToListAsync();

                    Kupci?.Clear();

                    foreach (var kupac in kupci)
                    {
                        Kupci?.Add(kupac);
                    
                    }
                }
                SelectedKupac = Kupci?[0];
                Debug.WriteLine("SelectedKupac u Load = " + SelectedKupac.Kupac);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Load Kupac = "+ex.ToString());
            }
        }

        public async Task UpdateArticlePosition(TblArtikli artikl)
        {
            using (var db = new AppDbContext())
            {
                var existingArticle = await db.Artikli.FindAsync(artikl.IdArtikla);
                if (existingArticle != null)
                {
                    existingArticle.Pozicija = artikl.Pozicija;
                    db.Update(existingArticle);
                    await db.SaveChangesAsync();
                }
            }
            await LoadArtikliAsync(); 
        }

        public async void FilterDrinkByCategory(int CategoryId)
        {
            await Task.Delay(5);
            Debug.WriteLine("Radi  FilterDrinkByCategory");
            var filtered = (ArtikliPice ?? [])
             .Where(a => a.Kategorija == CategoryId)
             .ToList();


            ArtikliPiceFilter?.Clear();
            foreach (var item in filtered)
                ArtikliPiceFilter?.Add(item);
            Debug.WriteLine("Radi  FilterDrinkByCategory  Atikli count: " + ArtikliPiceFilter?.Count);
        }
        public async void FilterFoodByCategory(int CategoryId)
        {
            await Task.Delay(5);
            Debug.WriteLine("Radi  FilterFoodByCategory " + CategoryId);
            var filtered = (ArtikliHrana ?? []).Where(a => a.Kategorija == CategoryId ).ToList();
            ArtikliHranaFilter?.Clear();
            foreach (var article in filtered)
                ArtikliHranaFilter?.Add(article);
            Debug.WriteLine("FilterFoodByCategory  Atikli count: " + ArtikliHranaFilter?.Count);
        }

        public async void FilterRestByCategory(int CategoryId)
        {
            await Task.Delay(5);
            var filtered = (ArtikliOstalo ?? []).Where(a => a.Kategorija == CategoryId ).ToList();
            ArtikliOstaloFilter?.Clear();
            foreach (var article in filtered)
                ArtikliOstaloFilter?.Add(article);
        }

        public void DodajStavkuRacuna(FiskalniRacun.Item stavka)
        {
            
            StavkeRacuna?.Add(stavka);
            TotalSum = StavkeRacuna?.Sum(item => item.TotalAmount ?? 0);
        }

        public void UpdateTotalSum()
        {
            TotalSum = Math.Round((StavkeRacuna ?? []).Sum(item => item.TotalAmount ?? 0), 2);

        }

        public bool StavkaPostoji(string sifra)
        {
            bool existsBySifra = (StavkeRacuna ?? []).Any(item => item.Sifra == sifra);
            Debug.WriteLine("existsBySifra " + existsBySifra);
            return existsBySifra;
        }

        public void UpdateStavkuRacunaPlus(FiskalniRacun.Item stavkaracuna, decimal kolicina)
        {
            
            var stavka = StavkeRacuna?.FirstOrDefault(item =>( item.Sifra?? "").Equals(stavkaracuna.Sifra));
            Debug.WriteLine("--------------------------- Stavka : " + stavka?.Name);
            decimal? trenutno = stavka?.Quantity;
            if(stavka == null) 
                return;
            Debug.WriteLine("--------------------------- Stavka kolicina sto se salje : " + kolicina);
            stavka.Quantity = trenutno + kolicina;
    
            UpdateTotalSum();
            Debug.WriteLine("------------------- stavka.Quantity: " + stavka?.Quantity);
            UpdateTotalSum();

        }

        public async Task UpdateStavkuRacunaMinus(FiskalniRacun.Item stavkaracuna, decimal kolicina)
        {
            await Task.Delay(1);
            Debug.WriteLine("--------------------------- Selektovana stavka : " + SelectedStavka?.Name);
            var stavka = StavkeRacuna?.FirstOrDefault(item => (item.Sifra ?? "").Equals(stavkaracuna.Sifra));
      

            decimal? trenutno = stavka?.Quantity;
            if (stavka== null)
                return;
            stavka.Quantity = trenutno - kolicina;
            if (stavka?.Quantity == 0)
            {
               StavkeRacuna?.Remove(stavka);
                UpdateTotalSum();
            }
            else
            {
                UpdateTotalSum();
            }

        }

        public void FilterDrinkByFirstLetter(string firstLetter)
        {
            var filtered = (ArtikliPice ?? []).Where(a => (a.ArtiklNormativ ?? "").StartsWith(firstLetter, StringComparison.OrdinalIgnoreCase)).ToList();
            ArtikliPiceFilter?.Clear();
            foreach (var article in filtered)
                ArtikliPiceFilter?.Add(article);
        }

        public void FilterFoodByFirstLetter(string firstLetter)
        {
            var filtered = (ArtikliHrana ?? []).Where(a =>( a.ArtiklNormativ ?? "").StartsWith(firstLetter, StringComparison.OrdinalIgnoreCase)).ToList();
            ArtikliHranaFilter?.Clear();
            foreach (var article in filtered)
                ArtikliHranaFilter?.Add(article);
        }

        public void FilterRestByFirstLetter(string firstLetter)
        {
            var filtered = (ArtikliOstalo ?? [])
              .Where(a => (a.ArtiklNormativ ?? "")
                  .StartsWith(firstLetter, StringComparison.OrdinalIgnoreCase))
              .ToList();
            ArtikliOstaloFilter?.Clear();
            foreach (var article in filtered)
                ArtikliOstaloFilter?.Add(article);
        }

        public void ArtikliFilterReset()
        {
            ArtikliPiceFilter?.Clear();
            ArtikliHranaFilter?.Clear();
            ArtikliOstaloFilter?.Clear();
            if (ArtikliPice == null)
                return;
            foreach (var article in ArtikliPice)
            {
                ArtikliPiceFilter?.Add(article);
            }
            if (ArtikliHrana == null)
                return;
            foreach (var article in ArtikliHrana)
            {
                ArtikliHranaFilter?.Add(article);
            }
            if (ArtikliOstalo == null)
                return;
            foreach (var article in ArtikliOstalo)
            {
                ArtikliOstaloFilter?.Add(article);
            }

        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected  void OnPropertyChanged(string? propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
