using Caupo.Data;
using Caupo.Properties;
using Caupo.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class NormsViewModel : INotifyPropertyChanged
    {

        private decimal? proizvodnaCijena;
        public decimal? ProizvodnaCijena
        {
            get { return proizvodnaCijena; }
            set
            {
                proizvodnaCijena = value;
                OnPropertyChanged (nameof (ProizvodnaCijena));
            }
        }

        private decimal? prodajnaCijena;
        public decimal? ProdajnaCijena
        {
            get { return prodajnaCijena; }
            set
            {
                prodajnaCijena = value;
                OnPropertyChanged (nameof (ProdajnaCijena));
            }
        }

        private string? marza;
        public string? Marza
        {
            get { return marza; }
            set
            {
                marza = value;
                OnPropertyChanged (nameof (Marza));
            }
        }

        private string? _imagePathFirstButton;
        public string? ImagePathFirstButton
        {
            get { return _imagePathFirstButton; }
            set
            {
                _imagePathFirstButton = value;
                OnPropertyChanged (nameof (ImagePathFirstButton));
            }
        }

        private string? _imagePathLastButton;
        public string? ImagePathLastButton
        {
            get { return _imagePathLastButton; }
            set
            {
                _imagePathLastButton = value;
                OnPropertyChanged (nameof (ImagePathLastButton));
            }
        }

        private string? _imagePathPhotoButton;
        public string? ImagePathPhotoButton
        {
            get { return _imagePathPhotoButton; }
            set
            {
                _imagePathPhotoButton = value;
                OnPropertyChanged (nameof (ImagePathPhotoButton));
            }
        }
        private string? _imagePathEditButton;
        public string? ImagePathEditButton
        {
            get { return _imagePathEditButton; }
            set
            {
                _imagePathEditButton = value;
                OnPropertyChanged (nameof (ImagePathEditButton));
            }
        }
        private string? _imagePathAddButton;
        public string? ImagePathAddButton
        {
            get { return _imagePathAddButton; }
            set
            {
                _imagePathAddButton = value;
                OnPropertyChanged (nameof (ImagePathAddButton));
            }
        }
        private string? _imagePathDeleteButton;
        public string? ImagePathDeleteButton
        {
            get { return _imagePathDeleteButton; }
            set
            {
                _imagePathDeleteButton = value;
                OnPropertyChanged (nameof (ImagePathDeleteButton));
            }
        }

        private string? _imagePathCancelButton;
        public string? ImagePathCancelButton
        {
            get { return _imagePathCancelButton; }
            set
            {
                _imagePathCancelButton = value;
                OnPropertyChanged (nameof (ImagePathCancelButton));
            }
        }

        private string? _imagePathSaveButton;
        public string? ImagePathSaveButton
        {
            get { return _imagePathSaveButton; }
            set
            {
                _imagePathSaveButton = value;
                OnPropertyChanged (nameof (ImagePathSaveButton));
            }
        }

        private string? _imagePathExportButton;
        public string? ImagePathExportButton
        {
            get { return _imagePathExportButton; }
            set
            {
                _imagePathExportButton = value;
                OnPropertyChanged (nameof (ImagePathExportButton));
            }
        }

        private string? _imagePathImportButton;
        public string? ImagePathImportButton
        {
            get { return _imagePathImportButton; }
            set
            {
                _imagePathImportButton = value;
                OnPropertyChanged (nameof (ImagePathImportButton));
            }
        }
        private string? _imagePathSearchTextBox;
        public string? ImagePathSearchTextBox
        {
            get { return _imagePathSearchTextBox; }
            set
            {
                _imagePathSearchTextBox = value;
                OnPropertyChanged (nameof (ImagePathSearchTextBox));
            }
        }

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

        private Brush? _backColor;
        public Brush? BackColor
        {
            get { return _backColor; }
            set
            {
                if(_backColor != value)
                {
                    _backColor = value;
                    OnPropertyChanged (nameof (BackColor));
                }
            }
        }

        private ObservableCollection<TblArtikli> _jela;
        public ObservableCollection<TblArtikli> Jela
        {
            get => _jela;
            set { _jela = value; OnPropertyChanged (); }
        }

        private ObservableCollection<TblArtikli> _suggestions;
        public ObservableCollection<TblArtikli> Suggestions
        {
            get => _suggestions;
            set { _suggestions = value; OnPropertyChanged (); }
        }

        public ObservableCollection<DatabaseTables.TblRepromaterijal> Ingredients { get; set; } = new ObservableCollection<DatabaseTables.TblRepromaterijal> ();
        public ObservableCollection<DatabaseTables.TblJediniceMjere> JediniceMjere { get; set; } = new ObservableCollection<DatabaseTables.TblJediniceMjere> ();

        private DatabaseTables.TblRepromaterijal? _selectedIngredient;
        public DatabaseTables.TblRepromaterijal? SelectedIngredient
        {
            get => _selectedIngredient;
            set
            {
                if(_selectedIngredient != value)
                {
                    _selectedIngredient = value;
                    OnPropertyChanged (nameof (SelectedIngredient));

                }
            }
        }

        private DatabaseTables.TblNormativ? _selectedNorm;
        public DatabaseTables.TblNormativ? SelectedNorm
        {
            get => _selectedNorm;
            set
            {
                if(_selectedNorm != value)
                {
                    _selectedNorm = value;
                    OnPropertyChanged (nameof (SelectedNorm));

                }
            }
        }

        private DatabaseTables.TblJediniceMjere? _selectedJedinicaMjere;
        public DatabaseTables.TblJediniceMjere? SelectedJedinicaMjere
        {
            get => _selectedJedinicaMjere;
            set
            {
                if(_selectedJedinicaMjere != value)
                {
                    _selectedJedinicaMjere = value;
                    OnPropertyChanged (nameof (SelectedJedinicaMjere));
                }
            }
        }

        private DatabaseTables.TblArtikli? _selectedJelo;
        public DatabaseTables.TblArtikli? SelectedJelo
        {
            get => _selectedJelo;
            set
            {
                if(_selectedJelo != value)
                {
                    _selectedJelo = value;
                    OnPropertyChanged (nameof (SelectedJelo));
                }
            }
        }


        private ObservableCollection<TblNormativ>? _norm;
        public ObservableCollection<TblNormativ>? Norm
        {
            get => _norm;
            set
            {
                _norm = value;
                OnPropertyChanged (nameof (Norm));
            }
        }


        private ObservableCollection<TblRepromaterijal>? _ingredientFilter;
        public ObservableCollection<TblRepromaterijal>? IngredientFilter
        {
            get => _ingredientFilter;
            set
            {
                _ingredientFilter = value;
                OnPropertyChanged (nameof (IngredientFilter));
            }
        }

        private bool _isSuggestionsOpen;
        public bool IsSuggestionsOpen
        {
            get => _isSuggestionsOpen;
            set
            {
                if(_isSuggestionsOpen != value)
                {
                    _isSuggestionsOpen = value;
                    OnPropertyChanged ();
                }
            }
        }

        public string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if(_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged (nameof (SearchText));
                    Debug.WriteLine ($"SearchText changed to: ");  // Dodaj log za testiranje
                    FilterItems (_searchText);
                    IsSuggestionsOpen = !string.IsNullOrWhiteSpace (_searchText) && Suggestions.Any ();
                }
            }
        }

        public string? _searchTextIngredient;
        public string? SearchTextIngredient
        {
            get => _searchTextIngredient;
            set
            {
                if(_searchTextIngredient != value)
                {
                    _searchTextIngredient = value;
                    OnPropertyChanged (nameof (SearchTextIngredient));
                    Debug.WriteLine ($"SearchTextIngredient changed to: " + _searchTextIngredient);  // Dodaj log za testiranje
                    FilterItemsIngredient (_searchTextIngredient);

                }
            }
        }

        public ICommand DeleteCommand { get; }

        public NormsViewModel()
        {
            Start ();
            Suggestions = new ObservableCollection<TblArtikli> ();
            Jela = new ObservableCollection<TblArtikli> ();
            Ingredients = new ObservableCollection<TblRepromaterijal> ();
            IngredientFilter = new ObservableCollection<TblRepromaterijal> ();
            Norm = new ObservableCollection<TblNormativ> ();
            SelectedIngredient = IngredientFilter?.FirstOrDefault ();
            DeleteCommand = new AsyncRelayCommand (async () => await DeleteNorm (SelectedNorm));
            // Debug.WriteLine("SelectedArticle = " + SelectedIngredient.Repromaterijal);
            Debug.WriteLine ("SearchText = " + SearchText);
            ProizvodnaCijena = 0;
            ProdajnaCijena = 0;
            Marza = "0.00 KM        0%";
        }



        private async void Start()
        {
            await SetImage ();
            await LoadJela ();
            await LoadIngredientsAsync ();
            await LoadJediniceMjere ();
            await LoadNormativ (SelectedJelo);

        }

        public async Task DeleteNorm(TblNormativ norm)
        {


            YesNoPopup myMessageBox = new YesNoPopup ();

            myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            myMessageBox.MessageTitle.Text = "POTVRDA BRISANJA";
            myMessageBox.MessageText.Text = "Da li ste sigurni da želite obrisati normativ:" + Environment.NewLine + norm.Repromaterijal + ", količina: " + norm.Kolicina + " ?";
            myMessageBox.ShowDialog ();
            if(myMessageBox.Kliknuo == "Da")
            {
                try
                {
                    using(var db = new AppDbContext ())
                    {
                        var stavka = await db.Normativ.FindAsync (norm.IdStavkeNormativa);
                        if(stavka != null)
                        {
                            db.Normativ.Remove (stavka);
                            await db.SaveChangesAsync ();
                            await LoadNormativ (SelectedJelo);
                        }
                    }
                }
                catch(Exception d)
                {
                    Debug.WriteLine ("Brisanje normativa error: " + d.Message + ", " + Environment.NewLine + "Stack: " + d.StackTrace);
                }
            }
        }

        public async Task LoadJela()
        {
            try
            {

                using(var db = new AppDbContext ())
                {
                    var artikli = await db.Artikli.ToListAsync ();

                    var jela = artikli.Where (a => a.VrstaArtikla == 1).OrderBy (a => a.Pozicija).ToList ();


                    Jela?.Clear ();

                    foreach(var jelo in jela)
                    {
                        Jela?.Add (jelo);

                    }
                    Debug.WriteLine ("Jela ima >>>>>> " + Jela.Count);
                    SelectedJelo = Jela?[0];

                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }

        public async Task InsertNorm(TblNormativ normativ, TblArtikli jelo)
        {

            using(var db = new AppDbContext ())
            {
                var duplicateNormativ = await db.Normativ
                    .Where (a => a.Repromaterijal == normativ.Repromaterijal && a.IdProizvoda == normativ.IdProizvoda)
                    .FirstOrDefaultAsync ();

                if(duplicateNormativ != null)
                {

                    string poruka = normativ.Repromaterijal + " se već koristi u izradi ovog jela.";
                    OnErrorOccurred (poruka);
                    return;


                }
                else
                {
                    await db.Normativ.AddAsync (normativ);
                    db.SaveChanges ();
                    Debug.WriteLine ("Novi zapis je uspešno dodat!");
                    await LoadNormativ (jelo);

                }
            }
        }

        public async Task UpdateNorm(TblNormativ normativ, TblArtikli jelo)
        {
            using(var db = new AppDbContext ())
            {
                // Pronađi postojeći zapis po ID-u
                var existingNorm = await db.Normativ
                    .FirstOrDefaultAsync (a => a.IdStavkeNormativa == normativ.IdStavkeNormativa);

                if(existingNorm == null)
                {
                    OnErrorOccurred ("Normativ nije pronađen u bazi.");
                    return;
                }

                // Ažuriraj vrijednosti
                existingNorm.Repromaterijal = normativ.Repromaterijal;
                existingNorm.Kolicina = normativ.Kolicina;
                existingNorm.JedinicaMjere = normativ.JedinicaMjere;
                existingNorm.IdProizvoda = normativ.IdProizvoda;
                existingNorm.Cijena = normativ.Cijena;

                await db.SaveChangesAsync ();
                Debug.WriteLine ("Zapis je uspješno ažuriran!");

                // Ponovno učitaj podatke za prikaz
                await LoadNormativ (jelo);
            }
        }


        public async Task LoadNormativ(TblArtikli jelo)
        {
            SelectedJelo = jelo;
            Norm.Clear ();
            ProizvodnaCijena = 0;
            ProdajnaCijena = 0;
            Marza = string.Empty;
            Debug.WriteLine ("Jelo : " + jelo.Artikl + " ima ID: " + jelo.IdArtikla);
            try
            {

                using(var db = new AppDbContext ())
                {
                    var norms = await db.Normativ.ToListAsync ();

                    var norm = norms.Where (a => a.IdProizvoda == jelo.IdArtikla).OrderBy (a => a.IdStavkeNormativa).ToList ();

                    if(norm.Count != 0)
                    {

                        foreach(var ingredient in norm)
                        {
                            Norm?.Add (ingredient);
                            ProizvodnaCijena += ingredient.Ukupno;
                        }
                        Debug.WriteLine ("Jelo ima >>>>>> " + Norm.Count + " sastojaka");
                        decimal marzarazlika = (jelo.Cijena ?? 0) - (proizvodnaCijena ?? 0);
                        decimal marzapostotak = ((jelo.Cijena ?? 0) / (proizvodnaCijena ?? 0)) * 100;
                        Marza = marzarazlika.ToString ("F2") + " KM    " + marzapostotak.ToString ("F2") + "%";
                        ProdajnaCijena = jelo.Cijena;
                        SelectedNorm = Norm[0];
                    }
                    else
                    {
                        ProdajnaCijena = jelo.Cijena;
                        ProizvodnaCijena = 0;
                        Marza = string.Empty;
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }

        public async Task LoadJediniceMjere()
        {
            try
            {

                using(var db = new AppDbContext ())
                {
                    var JM = await db.JediniceMjere.ToListAsync ();

                    JediniceMjere.Clear ();
                    foreach(var jm in JM)
                    {
                        JediniceMjere.Add (jm);
                        Debug.WriteLine (jm.JedinicaMjere + ", JM Count = " + JediniceMjere.Count);
                    }

                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }





        public event EventHandler<string?>? ErrorOccurred;
        protected virtual void OnErrorOccurred(string? message)
        {
            ErrorOccurred?.Invoke (this, message);
        }

        public async Task SetImage()
        {
            await Task.Delay (1);
            string tema = Settings.Default.Tema;

            if(tema == "Tamna")
            {
                Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema);
                //ImagePathFirstButton = "pack://application:,,,/Images/Dark/first.svg";
                //ImagePathLastButton = "pack://application:,,,/Images/Dark/last.svg";
                //ImagePathPhotoButton = "pack://application:,,,/Images/Dark/image.png";
                //ImagePathSearchTextBox = "pack://application:,,,/Images/Dark/search.svg";
                //ImagePathEditButton = "pack://application:,,,/Images/Dark/edit.svg";
                // ImagePathAddButton = "pack://application:,,,/Images/Dark/plus.svg";
                // ImagePathDeleteButton = "pack://application:,,,/Images/Dark/delete.svg";
                // ImagePathCancelButton = "pack://application:,,,/Images/Dark/cancel.svg";
                //ImagePathSaveButton = "pack://application:,,,/Images/Dark/save.svg";
                //ImagePathExportButton = "pack://application:,,,/Images/Dark/export.svg";
                //ImagePathImportButton = "pack://application:,,,/Images/Dark/import.svg";
                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
                BackColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));


            }
            else
            {
                Debug.WriteLine ("Aktivna tema koju vidi viewmodel je : " + tema);
                // ImagePathFirstButton = "pack://application:,,,/Images/Light/first.svg";
                //  ImagePathLastButton = "pack://application:,,,/Images/Light/last.svg";
                // ImagePathPhotoButton = "pack://application:,,,/Images/Light/image.svg";
                // ImagePathSearchTextBox = "pack://application:,,,/Images/Light/search.svg";
                // ImagePathEditButton = "pack://application:,,,/Images/Light/edit.svg";
                // ImagePathAddButton = "pack://application:,,,/Images/Light/plus.svg";
                //   ImagePathDeleteButton = "pack://application:,,,/Images/Light/delete.svg";
                //   ImagePathCancelButton = "pack://application:,,,/Images/Light/cancel.svg";
                // ImagePathSaveButton = "pack://application:,,,/Images/Light/save.svg";
                //ImagePathExportButton = "pack://application:,,,/Images/Light/export.svg";
                //ImagePathImportButton = "pack://application:,,,/Images/Light/import.svg";
                FontColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (50, 50, 50));
                Application.Current.Resources["GlobalFontColor"] = FontColor;
                BackColor = new SolidColorBrush (System.Windows.Media.Color.FromRgb (212, 212, 212));
                //FontColorAdv = new System.Windows.Media.Color();
                //FontColorAdv = System.Windows.Media.Color.FromRgb(50, 50, 50);
            }
        }
        public async Task LoadIngredientsAsync()
        {
            try
            {

                using(var db = new AppDbContext ())
                {
                    var ingredients = await db.Repromaterijal.ToListAsync ();
                    Ingredients.Clear ();
                    IngredientFilter?.Clear ();
                    foreach(var ingredient in ingredients)
                    {

                        Ingredients.Add (ingredient);
                        IngredientFilter?.Add (ingredient);

                    }

                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }


        public void FilterItems(string? searchtext)
        {
            Debug.WriteLine ("Okida search");

            if(string.IsNullOrWhiteSpace (SearchText))
            {
                Suggestions.Clear ();
                return;
            }
            var filtered = Jela
                 .Where (a => (a.Artikl ?? string.Empty)
                 .ToLower ()
                 .Contains ((SearchText ?? string.Empty).ToLower ()))
                 .ToList ();


            Suggestions.Clear ();
            foreach(var item in filtered)
                Suggestions.Add (item);
        }

        public void FilterItemsIngredient(string? searchtext)
        {
            Debug.WriteLine ("Okida search");

            var filtered = Ingredients
                 .Where (a => (a.Repromaterijal ?? string.Empty)
                 .ToLower ()
                 .Contains ((SearchTextIngredient ?? string.Empty).ToLower ()))
                 .ToList ();


            IngredientFilter = new ObservableCollection<DatabaseTables.TblRepromaterijal> (filtered);
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }
    }
}
