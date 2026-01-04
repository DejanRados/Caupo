using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Caupo.Data;
using Caupo.Properties;
using Caupo.Views;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class IngredientsViewModel : INotifyPropertyChanged
    {

        private string? _imagePathFirstButton;
        public string? ImagePathFirstButton
        {
            get { return _imagePathFirstButton; }
            set
            {
                _imagePathFirstButton = value;
                OnPropertyChanged(nameof(ImagePathFirstButton));
            }
        }

        private string? _imagePathLastButton;
        public string? ImagePathLastButton
        {
            get { return _imagePathLastButton; }
            set
            {
                _imagePathLastButton = value;
                OnPropertyChanged(nameof(ImagePathLastButton));
            }
        }

        private string? _imagePathPhotoButton;
        public string? ImagePathPhotoButton
        {
            get { return _imagePathPhotoButton; }
            set
            {
                _imagePathPhotoButton = value;
                OnPropertyChanged(nameof(ImagePathPhotoButton));
            }
        }
        private string? _imagePathEditButton;
        public string? ImagePathEditButton
        {
            get { return _imagePathEditButton; }
            set
            {
                _imagePathEditButton = value;
                OnPropertyChanged(nameof(ImagePathEditButton));
            }
        }
        private string? _imagePathAddButton;
        public string? ImagePathAddButton
        {
            get { return _imagePathAddButton; }
            set
            {
                _imagePathAddButton = value;
                OnPropertyChanged(nameof(ImagePathAddButton));
            }
        }
        private string? _imagePathDeleteButton;
        public string? ImagePathDeleteButton
        {
            get { return _imagePathDeleteButton; }
            set
            {
                _imagePathDeleteButton = value;
                OnPropertyChanged(nameof(ImagePathDeleteButton));
            }
        }

        private string? _imagePathCancelButton;
        public string? ImagePathCancelButton
        {
            get { return _imagePathCancelButton; }
            set
            {
                _imagePathCancelButton = value;
                OnPropertyChanged(nameof(ImagePathCancelButton));
            }
        }

        private string? _imagePathSaveButton;
        public string? ImagePathSaveButton
        {
            get { return _imagePathSaveButton; }
            set
            {
                _imagePathSaveButton = value;
                OnPropertyChanged(nameof(ImagePathSaveButton));
            }
        }

        private string? _imagePathExportButton;
        public string? ImagePathExportButton
        {
            get { return _imagePathExportButton; }
            set
            {
                _imagePathExportButton = value;
                OnPropertyChanged(nameof(ImagePathExportButton));
            }
        }

        private string? _imagePathImportButton;
        public string? ImagePathImportButton
        {
            get { return _imagePathImportButton; }
            set
            {
                _imagePathImportButton = value;
                OnPropertyChanged(nameof(ImagePathImportButton));
            }
        }
        private string? _imagePathSearchTextBox;
        public string? ImagePathSearchTextBox
        {
            get { return _imagePathSearchTextBox; }
            set
            {
                _imagePathSearchTextBox = value;
                OnPropertyChanged(nameof(ImagePathSearchTextBox));
            }
        }

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
        public ObservableCollection<DatabaseTables.TblRepromaterijal> Ingredients { get; set; } = new ObservableCollection<DatabaseTables.TblRepromaterijal>();
        public ObservableCollection<DatabaseTables.TblJediniceMjere> JediniceMjere { get; set; } = new ObservableCollection<DatabaseTables.TblJediniceMjere>();

        private DatabaseTables.TblRepromaterijal? _selectedIngredient;
        public DatabaseTables.TblRepromaterijal? SelectedIngredient
        {
            get => _selectedIngredient;
            set
            {
                if (_selectedIngredient != value)
                {
                    _selectedIngredient = value;
                    OnPropertyChanged(nameof(SelectedIngredient));
                    UpdateComboBox();
                }
            }
        }

        private DatabaseTables.TblJediniceMjere? _selectedJedinicaMjere;
        public DatabaseTables.TblJediniceMjere? SelectedJedinicaMjere
        {
            get => _selectedJedinicaMjere;
            set
            {
                if (_selectedJedinicaMjere != value)
                {
                    _selectedJedinicaMjere = value;
                    OnPropertyChanged(nameof(SelectedJedinicaMjere));
                }
            }
        }


        private ObservableCollection<TblRepromaterijal>? _ingredientFilter;
        public ObservableCollection<TblRepromaterijal>? IngredientFilter
        {
            get => _ingredientFilter;
            set
            {
                _ingredientFilter = value;
                OnPropertyChanged(nameof(IngredientFilter));
            }
        }

        public string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    Debug.WriteLine($"SearchText changed to: ");  // Dodaj log za testiranje
                    FilterItems(_searchText);
                }
            }
        }

    

        public IngredientsViewModel()
        {
            Start();
            Ingredients = new ObservableCollection<TblRepromaterijal>();
           IngredientFilter= new ObservableCollection<TblRepromaterijal>();
            SelectedIngredient = IngredientFilter?.FirstOrDefault();
           // Debug.WriteLine("SelectedArticle = " + SelectedIngredient.Repromaterijal);
            Debug.WriteLine("SearchText = " + SearchText);
        }



        private async void Start()
        {
            await SetImage();
            await  LoadIngredientsAsync();
          await LoadJediniceMjere();
          
        }

       

        public async Task LoadJediniceMjere()
        {
            try
            {

                using (var db = new AppDbContext())
                {
                    var JM = await db.JediniceMjere.ToListAsync();

                    JediniceMjere.Clear();
                    foreach (var jm in JM)
                    {
                        JediniceMjere.Add(jm);
                        Debug.WriteLine(jm.JedinicaMjere + ", JM Count = " + JediniceMjere.Count);
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public void UpdateComboBox()
        {
            Debug.WriteLine("Okida UpdateComboBox() ");
            if (SelectedIngredient != null)
            {
                Debug.WriteLine("UpdateComboBox() -- SelectedIngredient != null");
                SelectedJedinicaMjere = JediniceMjere.FirstOrDefault(item => item.IdJedinice == SelectedIngredient.JedinicaMjere);
             
            }
        }
        public async Task DeleteIngredients(int ingredientId)
        {
            using (var db = new AppDbContext())
            {
                var ingredient = await db.Repromaterijal.FindAsync(ingredientId);

                if (ingredient != null)
                {
                    db.Repromaterijal.Remove(ingredient);

                    await db.SaveChangesAsync();
                    await LoadIngredientsAsync();
                    SelectedIngredient = Ingredients.FirstOrDefault();
                    MyMessageBox myMessageBox = new MyMessageBox();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "POTVRDA";
                    myMessageBox.MessageText.Text = "Namirnica " + ingredient.Repromaterijal + " je uspješno obrisana iz baze.";
                    myMessageBox.ShowDialog();
                }
                else
                {
                    MyMessageBox myMessageBox = new MyMessageBox();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "Greška";
                    myMessageBox.MessageText.Text = "Namirnica sa ID: " + ingredient?.IdRepromaterijala + " nije pronađena u bazi." + Environment.NewLine + "Neuspješno brisanje namirnice";
                    myMessageBox.ShowDialog();
                }
            }
        }


        public event EventHandler<string?>? ErrorOccurred;
        protected virtual void OnErrorOccurred(string? message)
        {
            ErrorOccurred?.Invoke(this, message);
        }
        public async Task InsertIngredient(TblRepromaterijal ingredient)
        {
            using (var db = new AppDbContext())
            {
                var duplicateIngredient = await db.Repromaterijal
                    .Where(a => a.Repromaterijal == ingredient.Repromaterijal )
                    .FirstOrDefaultAsync();

                if (duplicateIngredient != null)
                {
                    string poruka = "";
         
                    poruka = duplicateIngredient.Repromaterijal + " se već koristi u bazi." + Environment.NewLine + "Ta vrsta podataka mora biti jedinstvena za svaku namirnicu";
                    OnErrorOccurred(poruka);
                    return;
                }
                else
                { 
                    await db.Repromaterijal.AddAsync(ingredient);
                    db.SaveChanges();
                    Debug.WriteLine("Novi zapis je uspešno dodat!");
                    await LoadIngredientsAsync();
                    SelectedIngredient = Ingredients.Last();
                    MyMessageBox myMessageBox = new MyMessageBox();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "POTVRDA";
                    myMessageBox.MessageText.Text = "Namirnica " + ingredient.Repromaterijal + " je uspješno dodana u bazu.";
                    myMessageBox.ShowDialog();
                }
            }
        }

        public async Task UpdateIngredient(TblRepromaterijal ingredient)
        {
            using (var db = new AppDbContext())
            {

              

                var existingIngredient = await db.Repromaterijal.FindAsync(ingredient.IdRepromaterijala);

                if (existingIngredient != null)
                {
                    
                    db.Entry(existingIngredient).CurrentValues.SetValues(ingredient);
                    await db.SaveChangesAsync();
                 
                    await LoadIngredientsAsync();
                    SelectedIngredient = existingIngredient;
                    Debug.WriteLine("Namirnica" + existingIngredient.Repromaterijal + " je uspešno ažurirana!");
                    Debug.WriteLine("SelectedIngredient = existingIngredient; postavljen" + existingIngredient.Repromaterijal );
                    Debug.WriteLine("LoadIngredientsAsync() trigerovan");
                    MyMessageBox myMessageBox = new MyMessageBox();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "POTVRDA";
                    myMessageBox.MessageText.Text = "Namirnica " + existingIngredient.Repromaterijal + " je uspješno ažurirana.";
                    myMessageBox.ShowDialog();

                }
                else
                {
                    MyMessageBox myMessageBox = new MyMessageBox();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "Greška";
                    myMessageBox.MessageText.Text = "Namirnica sa ID: " + ingredient.IdRepromaterijala + " nije pronađena u bazi." + Environment.NewLine + "Neuspješno ažuriranje namirnice";
                    myMessageBox.ShowDialog();
                }
            }
        }

        public async Task SetImage()
        {
            await Task.Delay(1);
            string tema = Settings.Default.Tema;
         
            if (tema == "Tamna")
            {
                Debug.WriteLine("Aktivna tema koju vidi viewmodel je : " + tema);
                ImagePathFirstButton = "pack://application:,,,/Images/Dark/first.svg";
                ImagePathLastButton = "pack://application:,,,/Images/Dark/last.svg";
                ImagePathPhotoButton = "pack://application:,,,/Images/Dark/image.png";
                ImagePathSearchTextBox = "pack://application:,,,/Images/Dark/search.svg";
                ImagePathEditButton = "pack://application:,,,/Images/Dark/edit.svg";
                ImagePathAddButton = "pack://application:,,,/Images/Dark/plus.svg";
                ImagePathDeleteButton = "pack://application:,,,/Images/Dark/delete.svg";
                ImagePathCancelButton = "pack://application:,,,/Images/Dark/cancel.svg";
                ImagePathSaveButton = "pack://application:,,,/Images/Dark/save.svg";
                ImagePathExportButton = "pack://application:,,,/Images/Dark/export.svg";
                ImagePathImportButton = "pack://application:,,,/Images/Dark/import.svg";
                FontColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212));   Application.Current.Resources["GlobalFontColor"] =    FontColor;
                BackColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));


            }
            else
            {
                Debug.WriteLine("Aktivna tema koju vidi viewmodel je : " + tema);
                ImagePathFirstButton = "pack://application:,,,/Images/Light/first.svg";
                ImagePathLastButton = "pack://application:,,,/Images/Light/last.svg";
                ImagePathPhotoButton = "pack://application:,,,/Images/Light/image.svg";
                ImagePathSearchTextBox = "pack://application:,,,/Images/Light/search.svg";
                ImagePathEditButton = "pack://application:,,,/Images/Light/edit.svg";
                ImagePathAddButton = "pack://application:,,,/Images/Light/plus.svg";
                ImagePathDeleteButton = "pack://application:,,,/Images/Light/delete.svg";
                ImagePathCancelButton = "pack://application:,,,/Images/Light/cancel.svg";
                ImagePathSaveButton = "pack://application:,,,/Images/Light/save.svg";
                ImagePathExportButton = "pack://application:,,,/Images/Light/export.svg";
                ImagePathImportButton = "pack://application:,,,/Images/Light/import.svg";
                FontColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));  Application.Current.Resources["GlobalFontColor"] =    FontColor;
                BackColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212));
                //FontColorAdv = new System.Windows.Media.Color();
                //FontColorAdv = System.Windows.Media.Color.FromRgb(50, 50, 50);
            }
        }
       public async Task LoadIngredientsAsync()
        {
            try
            {

                using (var db = new AppDbContext())
                {
                    var ingredients = await db.Repromaterijal.ToListAsync();
                    Ingredients.Clear();
                    IngredientFilter?.Clear();
                    foreach (var ingredient in ingredients)
                    {
                 
                        Ingredients.Add(ingredient);
                        IngredientFilter?.Add(ingredient);
                      
                    }
                  
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    
    
       public void FilterItems(string? searchtext)
        {
            Debug.WriteLine("Okida search");

			var filtered = Ingredients
	             .Where(a => (a.Repromaterijal ?? string.Empty)
	             .ToLower()
	             .Contains((SearchText ?? string.Empty).ToLower()))
	             .ToList();


			IngredientFilter = new ObservableCollection<DatabaseTables.TblRepromaterijal>(filtered);
        }


        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
