using Caupo.Data;
using Caupo.Views;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class ArticlesViewModel : INotifyPropertyChanged
    {


        public ObservableCollection<DatabaseTables.TblNormativPica> Normativi { get; set; } = new ObservableCollection<DatabaseTables.TblNormativPica> ();
        public ObservableCollection<DatabaseTables.TblKategorije> Kategorije { get; set; } = new ObservableCollection<DatabaseTables.TblKategorije> ();
        public ObservableCollection<DatabaseTables.TblPoreskeStope> PoreskeStope { get; set; } = new ObservableCollection<DatabaseTables.TblPoreskeStope> ();
        public ObservableCollection<DatabaseTables.TblJediniceMjere> JediniceMjere { get; set; } = new ObservableCollection<DatabaseTables.TblJediniceMjere> ();
        public List<string> VrstaArtikla { get; set; } = new List<string> ();

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

        private DatabaseTables.TblKategorije? _selectedKategorija;
        public DatabaseTables.TblKategorije? SelectedKategorija
        {
            get => _selectedKategorija;
            set
            {
                if(_selectedKategorija != value)
                {
                    _selectedKategorija = value;
                    OnPropertyChanged (nameof (SelectedKategorija));
                }
            }
        }

        private DatabaseTables.TblPoreskeStope? _selectedPoreskaStopa;
        public DatabaseTables.TblPoreskeStope? SelectedPoreskaStopa
        {
            get => _selectedPoreskaStopa;
            set
            {
                if(_selectedPoreskaStopa != value)
                {
                    _selectedPoreskaStopa = value;
                    OnPropertyChanged (nameof (SelectedPoreskaStopa));
                }
            }
        }

        private DatabaseTables.TblNormativPica? _selectedNormativ;
        public DatabaseTables.TblNormativPica? SelectedNormativ
        {
            get => _selectedNormativ;
            set
            {
                if(_selectedNormativ != value)
                {
                    _selectedNormativ = value;
                    OnPropertyChanged (nameof (SelectedNormativ));
                }
            }
        }

        private string? _selectedVrstaArtikla;
        public string? SelectedVrstaArtikla
        {
            get => _selectedVrstaArtikla;
            set
            {
                if(_selectedVrstaArtikla != value)
                {
                    _selectedVrstaArtikla = value;
                    OnPropertyChanged (nameof (SelectedVrstaArtikla));
                }
            }
        }

        private DatabaseTables.TblArtikli? _selectedArticle;
        public DatabaseTables.TblArtikli? SelectedArticle
        {
            get => _selectedArticle;
            set
            {
                _selectedArticle = value;
                OnPropertyChanged (nameof (SelectedArticle));
                UpdateComboBoxes ();
            }
        }

        private ObservableCollection<TblArtikli>? _artikli;
        public ObservableCollection<TblArtikli>? Artikli
        {
            get => _artikli;
            set
            {
                _artikli = value;
                OnPropertyChanged (nameof (Artikli));
            }
        }

        private ObservableCollection<TblArtikli>? _artikliFilter;
        public ObservableCollection<TblArtikli>? ArtikliFilter
        {
            get => _artikliFilter;
            set
            {
                _artikliFilter = value;
                OnPropertyChanged (nameof (ArtikliFilter));
            }
        }

        public string? _searchText = "";
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
                }
            }
        }

        public string? _novaSifra;
        public string? NovaSifra
        {
            get => _novaSifra;
            set
            {
                if(_novaSifra != value)
                {
                    _novaSifra = value;
                    OnPropertyChanged (nameof (NovaSifra));

                }
            }
        }

        public ArticlesViewModel()
        {
            Artikli = new ObservableCollection<TblArtikli> ();
            ArtikliFilter = new ObservableCollection<TblArtikli> ();
            Start ();


            Debug.WriteLine ("SearchText = " + SearchText);
        }



        private async void Start()
        {

            await LoadJediniceMjere ();
            await LoadKategorije ();
            await LoadNormativi ();
            await LoadVrsteArtikla ();
            await LoadPoreskeStope ();
            await LoadArticlesAsync ();

        }

        public async Task DeleteArticle(int articleId)
        {
            using(var db = new AppDbContext ())
            {
                var artikl = await db.Artikli.FindAsync (articleId);

                if(artikl != null)
                {
                    db.Artikli.Remove (artikl);

                    await db.SaveChangesAsync ();
                    await LoadArticlesAsync ();
                    SelectedArticle = Artikli?.FirstOrDefault ();
                    MyMessageBox myMessageBox = new MyMessageBox ();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "POTVRDA";
                    myMessageBox.MessageText.Text = "Artikl " + artikl.Artikl + " je uspješno obrisan iz baze.";
                    myMessageBox.ShowDialog ();
                }
                else
                {
                    MyMessageBox myMessageBox = new MyMessageBox ();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "Greška";
                    myMessageBox.MessageText.Text = "Artikl sa ID: " + artikl?.IdArtikla + " nije pronađen u bazi." + Environment.NewLine + "Neuspješno brisanje artikla";
                    myMessageBox.ShowDialog ();
                }
            }
        }


        public event EventHandler<string?>? ErrorOccurred;
        protected virtual void OnErrorOccurred(string? message)
        {
            ErrorOccurred?.Invoke (this, message);
        }
        public async Task InsertArticle(TblArtikli artikl)
        {
            SelectedArticle = null;
            using(var db = new AppDbContext ())
            {
                var duplicateArticle = await db.Artikli
                    .Where (a => a.Artikl == artikl.Artikl || a.Sifra == artikl.Sifra || a.InternaSifra == artikl.InternaSifra)
                    .FirstOrDefaultAsync ();

                if(duplicateArticle != null)
                {
                    string poruka = "";

                    if(duplicateArticle.Sifra == artikl.Sifra)
                        poruka += "Šifra " + artikl.Sifra + ",";
                    if(duplicateArticle.InternaSifra == artikl.InternaSifra)
                        poruka += "Interna šifra " + artikl.InternaSifra + ",";
                    poruka = poruka.TrimEnd (',', ' ');
                    poruka = poruka + " se već koristi u bazi." + Environment.NewLine + "Ta vrsta podataka mora biti jedinstvena za svaki artikl";
                    OnErrorOccurred (poruka);
                    return;
                }
                else
                {
                    await db.Artikli.AddAsync (artikl);
                    db.SaveChanges ();
                    Debug.WriteLine ("Novi zapis je uspešno dodat!");
                    await LoadArticlesAsync ();
                    SelectedArticle = Artikli?.Last ();
                    MyMessageBox myMessageBox = new MyMessageBox ();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "POTVRDA";
                    myMessageBox.MessageText.Text = "Artikl " + artikl.Artikl + " je uspješno dodan u bazu.";
                    myMessageBox.ShowDialog ();
                }
            }
        }

        public async Task UpdateArticle(TblArtikli artikl)
        {
            SelectedArticle = null;
            using(var db = new AppDbContext ())
            {

                var duplicateArticle = await db.Artikli
                                                    .Where (a => (a.Sifra == artikl.Sifra || a.InternaSifra == artikl.InternaSifra) && a.IdArtikla != artikl.IdArtikla)
                                                    .FirstOrDefaultAsync ();

                if(duplicateArticle != null)
                {
                    string poruka = "";
                    if(duplicateArticle.Sifra == artikl.Sifra)
                        poruka += "Šifra " + artikl.Sifra + ",";
                    if(duplicateArticle.InternaSifra == artikl.InternaSifra)
                        poruka += "Interna šifra " + artikl.InternaSifra + ",";
                    poruka = poruka.TrimEnd (',', ' ');
                    poruka = poruka + " se već koristi u bazi." + Environment.NewLine + "Ta vrsta podataka mora biti jedinstvena za svaki artikl";
                    OnErrorOccurred (poruka);
                    return;
                }

                var existingArticle = await db.Artikli.FindAsync (artikl.IdArtikla);

                if(existingArticle != null)
                {

                    db.Entry (existingArticle).CurrentValues.SetValues (artikl);
                    await db.SaveChangesAsync ();
                    Debug.WriteLine ("Artikl " + existingArticle.Artikl + " je uspešno ažuriran!");
                    await LoadArticlesAsync ();
                    SelectedArticle = existingArticle;

                    MyMessageBox myMessageBox = new MyMessageBox ();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "POTVRDA";
                    myMessageBox.MessageText.Text = "Artikl " + existingArticle.Artikl + " je uspješno ažuriran.";
                    myMessageBox.ShowDialog ();

                }
                else
                {
                    MyMessageBox myMessageBox = new MyMessageBox ();
                    myMessageBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    myMessageBox.MessageTitle.Text = "Greška";
                    myMessageBox.MessageText.Text = "Artikl sa ID: " + artikl.IdArtikla + " nije pronađen u bazi." + Environment.NewLine + "Neuspješno ažuriranje artikla";
                    myMessageBox.ShowDialog ();
                }
            }
        }


        public async Task LoadArticlesAsync()
        {
            try
            {

                using(var db = new AppDbContext ())
                {
                    var artikli = await db.Artikli.ToListAsync ();
                    Artikli?.Clear ();
                    ArtikliFilter?.Clear ();
                    foreach(var artikl in artikli)
                    {
                        Artikli?.Add (artikl);
                        ArtikliFilter?.Add (artikl);
                        Debug.WriteLine (artikl.Artikl + ", Artikli Count = " + Artikli?.Count);

                    }
                    Debug.WriteLine ("ArtikliFilter Count = " + (ArtikliFilter?.Count ?? 0));
                    SelectedArticle = ArtikliFilter?.FirstOrDefault ();

                    if(SelectedArticle != null)
                    {
                        Debug.WriteLine ("SelectedArticle = " + SelectedArticle.ArtiklNormativ);
                    }
                    else
                    {
                        Debug.WriteLine ("SelectedArticle je null!");
                    }

                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }
        public async Task GetNewSifra()
        {
            using(var db = new AppDbContext ())
            {


                // Dobijanje poslednjeg artikla
                var poslednjiArtikl = await db.Artikli
                    .OrderByDescending (a => a.IdArtikla)
                    .FirstOrDefaultAsync ();

                if(poslednjiArtikl != null)
                {
                    string? sifraPoslednjeg = poslednjiArtikl.Sifra;
                    Debug.WriteLine ($"Poslednja šifra u bazi: {sifraPoslednjeg}");


                    NovaSifra = Convert.ToString (Convert.ToInt32 (sifraPoslednjeg) + 1);
                }
            }
        }
        public void UpdateComboBoxes()
        {
            Debug.WriteLine ("Okida UpdateComboBoxes() ");
            if(SelectedArticle != null)
            {
                Debug.WriteLine ("UpdateComboBoxes() -- SelectedArticle != null");
                SelectedJedinicaMjere = JediniceMjere.FirstOrDefault (item => item.IdJedinice == SelectedArticle.JedinicaMjere);
                SelectedKategorija = Kategorije.FirstOrDefault (item => item.IdKategorije == SelectedArticle.Kategorija);
                SelectedNormativ = Normativi.FirstOrDefault (item => item.Normativ == SelectedArticle.Normativ.ToString ());
                SelectedVrstaArtikla = VrstaArtikla[SelectedArticle.VrstaArtikla ?? 0];
                SelectedPoreskaStopa = PoreskeStope.FirstOrDefault (item => item.IdStope == SelectedArticle.PoreskaStopa);
            }
        }
        public void FilterItems(string? searchtext)
        {
            Debug.WriteLine ("Okida search");
            var filtered = Artikli?.Where (a =>
                 (a.Artikl ?? "").ToLower ().Contains (SearchText?.ToLower () ?? "") ||
                 (a.Sifra ?? "").ToLower ().Contains (SearchText?.ToLower () ?? ""))
                .ToList ();

            ArtikliFilter = new ObservableCollection<DatabaseTables.TblArtikli> ();
            if(filtered != null)
            {
                foreach(var item in filtered)
                {
                    ArtikliFilter.Add (item);
                }
            }
        }

        public async Task LoadPoreskeStope()
        {
            try
            {

                using(var db = new AppDbContext ())
                {
                    var PS = await db.PoreskeStope.ToListAsync ();

                    PoreskeStope.Clear ();
                    foreach(var ps in PS)
                    {
                        PoreskeStope.Add (ps);
                        Debug.WriteLine (ps.Postotak + ", PSCount = " + PoreskeStope.Count);
                    }

                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }

        public async Task LoadNormativi()
        {
            try
            {

                using(var db = new AppDbContext ())
                {
                    var normativi = await db.NormativPica.ToListAsync ();

                    Normativi.Clear ();
                    foreach(var normativ in normativi)
                    {
                        Normativi.Add (normativ);
                        Debug.WriteLine (normativ.Normativ + ", Normativi Count = " + Normativi.Count);
                    }

                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }

        public async Task LoadKategorije()
        {
            try
            {

                using(var db = new AppDbContext ())
                {
                    var kategorije = await db.Kategorije.ToListAsync ();

                    Kategorije.Clear ();
                    foreach(var kategorija in kategorije)
                    {
                        Kategorije.Add (kategorija);
                        Debug.WriteLine (kategorija.Kategorija + ", Kategorije Count = " + Kategorije.Count);
                    }

                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }

        public async Task LoadVrsteArtikla()
        {
            await Task.Delay (5);
            VrstaArtikla.Clear ();                 // VrstaArtikla je ObservableCollection<string>
            VrstaArtikla.Add ("Piće");
            VrstaArtikla.Add ("Hrana");
            VrstaArtikla.Add ("Ostalo");

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }
    }
}
