using Caupo.Data;
using Caupo.Properties;
using Caupo.Views;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class BeverageInPageViewModel : INotifyPropertyChanged
    {

        public TblFirma Klijent = new TblFirma ();
        //public TblDobavljaci Supplier = new TblDobavljaci();
        public TblDobavljaci SelectedSupplier
        {
            get
            {
                if(SelectedStockIn == null || string.IsNullOrEmpty (SelectedStockIn.Dobavljac))
                    return null;

                return Suppliers.FirstOrDefault (s => s.Dobavljac == SelectedStockIn.Dobavljac);
            }
        }
        public ObservableCollection<DatabaseTables.TblDobavljaci> Suppliers { get; set; } = new ObservableCollection<DatabaseTables.TblDobavljaci> ();
        public ObservableCollection<DatabaseTables.TblUlaz> StockIn { get; set; } = new ObservableCollection<DatabaseTables.TblUlaz> ();
        public ObservableCollection<DatabaseTables.TblUlazStavke> StockInItems { get; set; } = new ObservableCollection<DatabaseTables.TblUlazStavke> ();

        private DatabaseTables.TblUlaz? _selectedStockIn;
        public DatabaseTables.TblUlaz? SelectedStockIn
        {
            get => _selectedStockIn;
            set
            {
                if(_selectedStockIn != value)
                {
                    _selectedStockIn = value;
                    OnPropertyChanged (nameof (SelectedStockIn));

                }
            }
        }

        private DatabaseTables.TblUlazStavke? _selectedStockInItem;
        public DatabaseTables.TblUlazStavke? SelectedStockInItem
        {
            get => _selectedStockInItem;
            set
            {
                if(_selectedStockInItem != value)
                {
                    _selectedStockInItem = value;
                    OnPropertyChanged (nameof (SelectedStockInItem));

                }
            }
        }




        private ObservableCollection<TblUlaz>? stockInFilter;
        public ObservableCollection<TblUlaz>? StockInFilter
        {
            get => stockInFilter;
            set
            {
                stockInFilter = value;
                OnPropertyChanged (nameof (StockInFilter));
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

        public string? _searchArticleText;
        public string? SearchArticleText
        {
            get => _searchArticleText;
            set
            {
                if(_searchArticleText != value)
                {
                    _searchArticleText = value;
                    OnPropertyChanged (nameof (SearchArticleText));
                    Debug.WriteLine ($"SearchText changed to: ");  // Dodaj log za testiranje
                    FilterArticleItems (_searchArticleText);
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
                    Debug.WriteLine ($"SearchText changed to: " + _searchText);
                    FilterItems (_searchText);
                }
            }
        }

        private decimal? iznosRacuna = 0;
        public decimal? IznosRacuna
        {
            get { return iznosRacuna; }
            set
            {
                iznosRacuna = value;
                OnPropertyChanged (nameof (IznosRacuna));
            }
        }



        public decimal EnteredPrice { get; set; }
        public decimal EnteredQuantity { get; set; }
        public decimal EnteredDiscount { get; set; }

        public ICommand AddNewStockInCommand { get; }
        public ICommand SaveStockInCommand { get; }
        public ICommand DeleteArticleCommand { get; }

        public bool HasUnsavedChanges { get; set; }

        public event Func<string, bool>? ShowDeletePopupRequested;

        public BeverageInPageViewModel()
        {
            AddNewStockInCommand = new RelayCommand (AddNewStockIn);
            SaveStockInCommand = new RelayCommand (async () => await SaveSelectedStockInAsync ());
            DeleteArticleCommand = new RelayCommand<TblUlazStavke> (DeleteStockInItem);


            StockIn = new ObservableCollection<TblUlaz> ();
            StockInFilter = new ObservableCollection<TblUlaz> ();
            StockInItems = new ObservableCollection<TblUlazStavke> ();
            Suppliers = new ObservableCollection<TblDobavljaci> ();
            Artikli = new ObservableCollection<TblArtikli> ();
            ArtikliFilter = new ObservableCollection<TblArtikli> ();
            Start ();
        }
        public async Task Start()
        {

            await LoadArticlesAsync ();
            await LoadSuppliersAsync ();

            //await LoadStockInAsync(0);
            await LoadFirmaAsync ();
            HasUnsavedChanges = false;
            Debug.WriteLine ("BeverageInViewModel ---------------------------------------------------------------------------------------------------------------------------");

        }



        public async Task LoadFirmaAsync()
        {
            try
            {
                using var db = new AppDbContext ();
                Klijent = await db.Firma.FirstOrDefaultAsync ();
                Debug.WriteLine ("BeverageInViewModel ----------------------------Ucitao firmu -----------------------------------------------------------------------------------------------");
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex);
            }
        }
        private async void DeleteStockInItem(TblUlazStavke stavka)
        {
            if(stavka == null)
                return;

            // Pozovi event da popup otvori View i vrati rezultat
            bool confirmDelete = ShowDeletePopupRequested?.Invoke (stavka.Artikl) ?? false;

            if(!confirmDelete)
                return;

            using var db = new AppDbContext ();

            // 1. Obriši iz baze
            var item = await db.UlazStavke.FindAsync (stavka.RedniBroj);
            if(item != null)
            {
                db.UlazStavke.Remove (item);
                await db.SaveChangesAsync ();
            }

            StockInItems.Remove (stavka);
        }


        public async Task UpdateStockInItem()
        {
            Debug.WriteLine ("Selektovana stavka u ViewModelu u UpdateStockInItem: " + SelectedStockInItem.Artikl);
            if(SelectedStockInItem == null)
                return;
            Debug.WriteLine ("Radimo UPDATE stavke: " + SelectedStockInItem.Artikl);
            using var db = new AppDbContext ();

            // 1. Pronađi stavku u bazi
            var item = await db.UlazStavke.FindAsync (SelectedStockInItem.RedniBroj);
            // 
            if(item != null)
            {
                Debug.WriteLine ("Našao je u bazi tu stavku redni broj: " + item.RedniBroj);
                item.CijenaBezUPDV = EnteredPrice;
                item.Kolicina = EnteredQuantity;
                item.Rabat = EnteredDiscount;
                item.Cijena = SelectedStockInItem.Cijena;
                item.BrojUlaza = SelectedStockIn.BrojUlaza;
                item.Artikl = SelectedStockInItem.Artikl;
                item.Sifra = SelectedStockInItem.Sifra;
                item.VrstaArtikla = SelectedStockInItem.VrstaArtikla;
                item.JedinicaMjere = SelectedStockInItem.JedinicaMjere;
                item.PoreskaStopa = SelectedStockInItem.PoreskaStopa;
                item.IznosBezUPDV = EnteredQuantity * EnteredPrice;
                item.CijenaSaUPDV = EnteredPrice * (decimal)1.17;
                item.IznosUPDVa = EnteredQuantity * (item.CijenaSaUPDV * (decimal)0.14529);


                item.NivelacijaID = 0;
                await db.SaveChangesAsync ();


                SelectedStockInItem.Cijena = item.Cijena;
                SelectedStockInItem.BrojUlaza = item.BrojUlaza;
                SelectedStockInItem.Artikl = item.Artikl;
                SelectedStockInItem.Sifra = item.Sifra;
                SelectedStockInItem.Kolicina = item.Kolicina;
                SelectedStockInItem.VrstaArtikla = item.VrstaArtikla;
                SelectedStockInItem.JedinicaMjere = item.JedinicaMjere;
                SelectedStockInItem.CijenaBezUPDV = item.CijenaBezUPDV;
                SelectedStockInItem.PoreskaStopa = item.PoreskaStopa;
                SelectedStockInItem.IznosBezUPDV = item.IznosBezUPDV;
                SelectedStockInItem.Rabat = item.Rabat;
                SelectedStockInItem.IznosUPDVa = item.IznosUPDVa;
                SelectedStockInItem.CijenaSaUPDV = item.CijenaSaUPDV;
                SelectedStockInItem.NivelacijaID = 0;
                CollectionViewSource.GetDefaultView (StockInItems).Refresh ();
                Debug.WriteLine ("Trebalo bi da je snimio kol: " + item.Kolicina + ", cijena: " + item.CijenaBezUPDV);

                //StockInItems.Clear();
                //await LoadStockInItems(SelectedStockIn);
            }
            else
            {
                Debug.WriteLine ("Nije našao je u bazi tu stavku : " + SelectedStockInItem.Artikl);
                SelectedStockInItem.CijenaBezUPDV = EnteredPrice;
                SelectedStockInItem.Kolicina = EnteredQuantity;
                SelectedStockInItem.Rabat = EnteredDiscount;
                SelectedStockInItem.Cijena = SelectedStockInItem.Cijena;
                SelectedStockInItem.BrojUlaza = SelectedStockIn.BrojUlaza;
                SelectedStockInItem.Artikl = SelectedStockInItem.Artikl;
                SelectedStockInItem.Sifra = SelectedStockInItem.Sifra;
                SelectedStockInItem.VrstaArtikla = SelectedStockInItem.VrstaArtikla;
                SelectedStockInItem.JedinicaMjere = SelectedStockInItem.JedinicaMjere;
                SelectedStockInItem.PoreskaStopa = SelectedStockInItem.PoreskaStopa;
                SelectedStockInItem.IznosBezUPDV = EnteredQuantity * EnteredPrice;
                SelectedStockInItem.CijenaSaUPDV = EnteredPrice * (decimal)1.17;
                SelectedStockInItem.IznosUPDVa = EnteredQuantity * (SelectedStockInItem.CijenaSaUPDV * (decimal)0.14529);
                SelectedStockInItem.NivelacijaID = 0;
                CollectionViewSource.GetDefaultView (StockInItems).Refresh ();
                HasUnsavedChanges = true;
            }


        }


        public void ProcessArticle()
        {
            Debug.WriteLine ("Kreće ProcessArticle()");
            bool pdv = Settings.Default.PDVKorisnik == "DA";
            Debug.WriteLine ("PDVKorisnik   " + Settings.Default.PDVKorisnik);
            if(SelectedArticle == null)
                return;

            Debug.WriteLine ("imamo SelectedArticle " + SelectedArticle.Artikl);
            TblUlazStavke stavka = new TblUlazStavke ();
            stavka.ObracunPDV = pdv;
            stavka.BrojUlaza = SelectedStockIn.BrojUlaza;
            stavka.Artikl = SelectedArticle.Artikl;
            stavka.Sifra = SelectedArticle.Sifra;
            stavka.Kolicina = EnteredQuantity;
            stavka.Cijena = SelectedArticle.Cijena / SelectedArticle.Normativ;
            stavka.VrstaArtikla = SelectedArticle.VrstaArtikla;
            stavka.JedinicaMjere = SelectedArticle.JedinicaMjere;
            if(pdv)
            {
                stavka.CijenaBezUPDV = EnteredPrice;
                stavka.PoreskaStopa = SelectedArticle.PoreskaStopa;
                stavka.IznosBezUPDV = EnteredQuantity * EnteredPrice;
                stavka.Rabat = EnteredDiscount;
                stavka.IznosUPDVa = EnteredQuantity * (stavka.CijenaSaUPDV * (decimal)0.14529);
                stavka.CijenaSaUPDV = EnteredPrice * (decimal)1.17;
            }
            else
            {
                stavka.CijenaBezUPDV = EnteredPrice * (decimal)1.17;
                stavka.PoreskaStopa = 1;
                stavka.IznosBezUPDV = EnteredQuantity * stavka.CijenaBezUPDV;
                stavka.Rabat = 0;
                stavka.IznosUPDVa = 0;
                stavka.CijenaSaUPDV = EnteredPrice * (decimal)1.17;
            }
            stavka.NivelacijaID = 0;
            StockInItems.Add (stavka);
            HasUnsavedChanges = true;
            Debug.WriteLine ("Dodao stavku: : " + Environment.NewLine +
             "Artikl : " + stavka.Artikl + Environment.NewLine +
              "BrojUlaza : " + stavka.BrojUlaza + Environment.NewLine +
              "Sifra : " + stavka.Sifra + Environment.NewLine +
              "Kolicina : " + stavka.Kolicina + Environment.NewLine +
                "Cijena : " + stavka.Cijena + Environment.NewLine +
                "VrstaArtikla : " + stavka.VrstaArtikla + Environment.NewLine +
                 "JedinicaMjere : " + stavka.JedinicaMjere + Environment.NewLine +
                 "CijenaBezUPDV : " + stavka.CijenaBezUPDV + Environment.NewLine +
                   "PoreskaStopa : " + stavka.PoreskaStopa + Environment.NewLine +
                    "IznosBezUPDV : " + stavka.IznosBezUPDV + Environment.NewLine +
                      "Rabat : " + stavka.Rabat + Environment.NewLine +
                        "IznosUPDVa : " + stavka.IznosUPDVa + Environment.NewLine +
                        "CijenaSaUPDV : " + stavka.CijenaSaUPDV + Environment.NewLine +
                         "IznosUPDVa : " + stavka.IznosUPDVa + Environment.NewLine

              );
            Debug.WriteLine ("Imamo stavki:  " + StockInItems.Count);
        }

        public async Task SaveSelectedStockInAsync()
        {
            if(string.IsNullOrWhiteSpace (SelectedStockIn.Dobavljac))
                return;
            if(string.IsNullOrWhiteSpace (SelectedStockIn.BrojFakture))
                return;

            try
            {
                using var db = new AppDbContext ();
                using var transaction = await db.Database.BeginTransactionAsync ();

                // 1. Učitamo postojeći ulaz (ako postoji)
                var existing = await db.Ulaz
                    .FirstOrDefaultAsync (u => u.BrojUlaza == SelectedStockIn.BrojUlaza);

                // 2. Učitamo stavke za taj ulaz (ako postoji)
                List<TblUlazStavke> existingItems = new List<TblUlazStavke> ();

                if(existing != null)
                {
                    existingItems = await db.UlazStavke
                        .Where (s => s.BrojUlaza == existing.BrojUlaza)
                        .ToListAsync ();
                }

                bool isNew = existing == null;

                // --------------------------------------
                // 3. INSERT potpuno novog ulaza
                // --------------------------------------
                if(isNew)
                {
                    await db.Ulaz.AddAsync (SelectedStockIn);
                    await db.SaveChangesAsync ();

                    // Ubacimo stavke
                    foreach(var s in StockInItems)
                    {
                        TblUlazStavke stavka = new TblUlazStavke
                        {
                            BrojUlaza = SelectedStockIn.BrojUlaza,
                            ObracunPDV = s.ObracunPDV,
                            Artikl = s.Artikl,
                            Sifra = s.Sifra,
                            Kolicina = s.Kolicina,
                            Cijena = s.Cijena,
                            VrstaArtikla = s.VrstaArtikla,
                            JedinicaMjere = s.JedinicaMjere,
                            CijenaBezUPDV = s.CijenaBezUPDV,
                            PoreskaStopa = s.PoreskaStopa,
                            IznosBezUPDV = s.IznosBezUPDV,
                            Rabat = s.Rabat,
                            IznosUPDVa = s.IznosUPDVa,
                            CijenaSaUPDV = s.CijenaSaUPDV,
                            NivelacijaID = s.NivelacijaID
                        };

                        await db.UlazStavke.AddAsync (stavka);
                    }
                }
                else
                {
                    // --------------------------------------
                    // 4. UPDATE postojećeg ulaza
                    // --------------------------------------
                    db.Entry (existing).CurrentValues.SetValues (SelectedStockIn);

                    // --------------------------------------
                    // 5. DODAVANJE novih stavki
                    // (postojeće ostaju iste )
                    // --------------------------------------
                    foreach(var s in StockInItems)
                    {
                        // Ako stavka nema ID/RedniBroj ili je nova -> dodaj
                        bool isExisting = existingItems.Any (st => st.RedniBroj == s.RedniBroj);

                        if(!isExisting)
                        {
                            TblUlazStavke nova = new TblUlazStavke
                            {
                                BrojUlaza = existing.BrojUlaza,
                                ObracunPDV = s.ObracunPDV,
                                Artikl = s.Artikl,
                                Sifra = s.Sifra,
                                Kolicina = s.Kolicina,
                                Cijena = s.Cijena,
                                VrstaArtikla = s.VrstaArtikla,
                                JedinicaMjere = s.JedinicaMjere,
                                CijenaBezUPDV = s.CijenaBezUPDV,
                                PoreskaStopa = s.PoreskaStopa,
                                IznosBezUPDV = s.IznosBezUPDV,
                                Rabat = s.Rabat,
                                IznosUPDVa = s.IznosUPDVa,
                                CijenaSaUPDV = s.CijenaSaUPDV,
                                NivelacijaID = s.NivelacijaID
                            };

                            await db.UlazStavke.AddAsync (nova);
                        }
                    }
                }

                // --------------------------------------
                // 6. Spasimo sve odjednom (optimizovano)
                // --------------------------------------
                await db.SaveChangesAsync ();
                await transaction.CommitAsync ();
                HasUnsavedChanges = false;
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("Greška u SaveSelectedStockInAsync: " + ex);
                throw;
            }
        }


        private async void AddNewStockIn()
        {
            if(string.IsNullOrWhiteSpace (SelectedStockIn.Dobavljac))
                return;
            if(string.IsNullOrWhiteSpace (SelectedStockIn.BrojFakture))
                return;
            var newStockIn = new TblUlaz
            {
                Datum = DateTime.Now,
                BrojUlaza = GenerateNextBrojUlaza (),
                Radnik = Globals.ulogovaniKorisnik.IdRadnika.ToString (),
                RadnikName = Globals.ulogovaniKorisnik.Radnik,
            };

            StockIn.Add (newStockIn);
            StockInFilter.Add (newStockIn);
            SelectedStockIn = newStockIn;
            await LoadStockInItems (SelectedStockIn);
            OnPropertyChanged (nameof (SelectedStockIn));

        }


        /*   public void Kalkulacija(TblUlazStavke stavka, decimal pdvStopa = 0.17m)
           {
               // MPVrijednost = Kolicina * Cijena
               stavka.MPVrijednost = stavka.Kolicina * stavka.Cijena;

               // IPDV = Cijena * PDVStopa
               stavka.IPDV = stavka.Cijena * pdvStopa;

               // IznosIPDV = Kolicina * IPDV
               stavka.IznosIPDV = stavka.Kolicina * stavka.IPDV;

               // ProdajnaVrijBezIPDV = MPVrijednost - IznosIPDV
               stavka.ProdajnaVrijBezIPDV = stavka.MPVrijednost - stavka.IznosIPDV;

               // FakVrBezPDV = CijenaBezUPDV * Kolicina
               stavka.FakVrBezPDV = stavka.CijenaBezUPDV * stavka.Kolicina;

               // ProdVrBezPDV = MPVrijednost - IznosIPDV
               stavka.ProdVrBezPDV = stavka.ProdajnaVrijBezIPDV;

               // IznosRabata = FakVrBezPDV * Rabat / 100
               stavka.IznosRabata = stavka.FakVrBezPDV * stavka.Rabat / 100;

               // FakturisanaVrijednost = FakVrBezPDV - IznosRabata
               stavka.FakturisanaVrijednost = stavka.FakVrBezPDV - stavka.IznosRabata;

               // NabavnaVrijednostSaPDV = FakturisanaVrijednost * (1 + pdvStopa)
               stavka.NabavnaVrijednostSaPDV = stavka.FakturisanaVrijednost * (1 + pdvStopa);

               // PDVMarza = (MPVrijednost - NabavnaVrijednostSaPDV) * pdvStopa
               stavka.PDVMarza = (stavka.MPVrijednost - stavka.NabavnaVrijednostSaPDV) * pdvStopa;

               // RazlikaCijBezPDV = MPVrijednost - NabavnaVrijednostSaPDV - PDVMarza
               stavka.RazlikaCijBezPDV = stavka.MPVrijednost - stavka.NabavnaVrijednostSaPDV - stavka.PDVMarza;

               // Marza (%) = (ProdajnaVrijBezIPDV - FakturisanaVrijednost) * 100 / ProdajnaVrijBezIPDV
               stavka.MarzaPostotak = (stavka.ProdajnaVrijBezIPDV - stavka.FakturisanaVrijednost) * 100 / stavka.ProdajnaVrijBezIPDV;
           }*/

        public void PrintKalkulacija(
    ObservableCollection<TblUlazStavke> stavke, TblFirma klijent, TblUlaz ulaz, TblDobavljaci dobavljac)
        {
            FlowDocument doc = new FlowDocument
            {
                PageWidth = 1100, // A4 Landscape width
                PageHeight = 793, // A4 Landscape height
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily ("Segoe UI"),
                FontSize = 10,
                PagePadding = new Thickness (30)
            };

            // ===== HEADER =====
            Table headerTable = new Table ();
            headerTable.Columns.Add (new TableColumn { Width = new GridLength (300) }); // lijeva kolona
            headerTable.Columns.Add (new TableColumn { Width = new GridLength (400) }); // srednja kolona
            headerTable.Columns.Add (new TableColumn { Width = new GridLength (300) }); // desna kolona

            TableRowGroup trg = new TableRowGroup ();
            headerTable.RowGroups.Add (trg);
            TableRow row = new TableRow ();
            trg.Rows.Add (row);

            // --- Lijevo: korisnik ---
            Paragraph korisnikPar = new Paragraph { TextAlignment = TextAlignment.Left };
            korisnikPar.Inlines.Add ("   " + klijent.NazivFirme + Environment.NewLine);
            korisnikPar.Inlines.Add ("   " + klijent.Adresa + Environment.NewLine);
            korisnikPar.Inlines.Add ("   " + klijent.Grad + Environment.NewLine);
            korisnikPar.Inlines.Add ("   " + "JIB: " + klijent.JIB + Environment.NewLine);
            korisnikPar.Inlines.Add ("   " + "PDV: " + klijent.PDV);
            row.Cells.Add (new TableCell (korisnikPar) { BorderThickness = new Thickness (0) });

            // --- Sredina: naslov i datum/faktura ---
            Paragraph sredinaPar = new Paragraph { TextAlignment = TextAlignment.Center };

            // Veći font za naslov
            Run naslovRun = new Run ("KALKULACIJA CIJENA: " + ulaz.BrojUlaza + "/" + DateTime.Now.ToString ("yy") + Environment.NewLine)
            {
                FontSize = 20,   // veći font
                FontWeight = FontWeights.Normal
            };
            sredinaPar.Inlines.Add (naslovRun);
            // Razmak
            Run razmakRun = new Run ("" + Environment.NewLine)
            {
                FontSize = 10
            };
            sredinaPar.Inlines.Add (razmakRun);

            // Manji font za datum
            Run datumRun = new Run ("Datum: " + ulaz.Datum.ToString ("dd.MM.yyyy") + Environment.NewLine)
            {
                FontSize = 15
            };
            sredinaPar.Inlines.Add (datumRun);

            // Manji font za broj fakture
            Run fakturaRun = new Run ("Broj fakture: " + ulaz.BrojFakture)
            {
                FontSize = 15
            };
            sredinaPar.Inlines.Add (fakturaRun);

            row.Cells.Add (new TableCell (sredinaPar) { BorderThickness = new Thickness (0) });

            // --- Desno: dobavljač ---
            Paragraph dobavljacPar = new Paragraph { TextAlignment = TextAlignment.Left };
            dobavljacPar.Inlines.Add ("   " + dobavljac.Dobavljac + Environment.NewLine);
            dobavljacPar.Inlines.Add ("   " + dobavljac.Adresa + Environment.NewLine);
            dobavljacPar.Inlines.Add ("   " + dobavljac.Mjesto + Environment.NewLine);
            dobavljacPar.Inlines.Add ("   " + "JIB: " + dobavljac.JIB + Environment.NewLine);
            dobavljacPar.Inlines.Add ("   " + "PDV: " + dobavljac.PDV);
            row.Cells.Add (new TableCell (dobavljacPar) { BorderThickness = new Thickness (0) });

            doc.Blocks.Add (headerTable);
            doc.Blocks.Add (new Paragraph (new Run (" ")) { FontSize = 6 }); // razmak ispod headera

            // ===== TABELA =====
            Table table = new Table { CellSpacing = 0 };
            doc.Blocks.Add (table);

            string[] kolone = {
        "#", "Naziv artikla", "JM", "Količina", "Fakturna cijena po jedinici mjere bez PDV-a", "Fakturna vrijednost bez PDV-a",
        "Zavisni troškovi bez PDV", "Nabavna cijena po jedinici mjere bez PDV", "Nabavna vrijednost bez PDV -a", "Stopa razlike u cijeni",
        "Iznos razlike u cijeni", "Prodajna vrijednost bez PDV-a", "Stopa PDV-a", "Iznos PDV-a", "Maloprodajna vrijednost sa PDV-om", "Maloprodajna cijena sa PDV-om"
    };

            //foreach (var col in kolone)
            table.Columns.Add (new TableColumn { Width = new GridLength (40) });
            table.Columns.Add (new TableColumn { Width = new GridLength (200) });
            table.Columns.Add (new TableColumn { Width = new GridLength (55) });
            table.Columns.Add (new TableColumn { Width = new GridLength (55) });
            table.Columns.Add (new TableColumn { Width = new GridLength (55) });
            table.Columns.Add (new TableColumn { Width = new GridLength (55) });
            table.Columns.Add (new TableColumn { Width = new GridLength (55) });
            table.Columns.Add (new TableColumn { Width = new GridLength (55) });
            table.Columns.Add (new TableColumn { Width = new GridLength (55) });
            table.Columns.Add (new TableColumn { Width = new GridLength (55) });
            table.Columns.Add (new TableColumn { Width = new GridLength (55) });
            table.Columns.Add (new TableColumn { Width = new GridLength (55) });
            table.Columns.Add (new TableColumn { Width = new GridLength (50) });
            table.Columns.Add (new TableColumn { Width = new GridLength (50) });
            table.Columns.Add (new TableColumn { Width = new GridLength (75) });
            table.Columns.Add (new TableColumn { Width = new GridLength (75) });
            // Header row
            TableRowGroup headerGroup = new TableRowGroup ();
            table.RowGroups.Add (headerGroup);
            TableRow headerRow = new TableRow ();
            headerGroup.Rows.Add (headerRow);

            foreach(var col in kolone)
            {
                TextBlock tb = new TextBlock
                {
                    Text = col,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness (0)
                };

                TableCell cell = new TableCell (new BlockUIContainer (tb))
                {
                    Padding = new Thickness (3),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness (0.5)
                };

                headerRow.Cells.Add (cell);
            }

            // Podaci
            TableRowGroup bodyGroup = new TableRowGroup ();
            table.RowGroups.Add (bodyGroup);

            // Sume
            decimal sumFakVr = 0, sumNabavna = 0, sumZavisni = 0, sumRabat = 0, sumProdVr = 0, sumIznosPDV = 0, sumMP = 0;

            int index = 1;
            foreach(var s in stavke)
            {
                TableRow r = new TableRow ();
                bodyGroup.Rows.Add (r);

                r.Cells.Add (CreateCell (index.ToString (), TextAlignment.Center));
                r.Cells.Add (CreateCell (s.Artikl ?? "", TextAlignment.Left));
                r.Cells.Add (CreateCell (s.JedinicaMjereName?.ToString () ?? "", TextAlignment.Center));
                r.Cells.Add (CreateCell (s.Kolicina.ToString ("F2"), TextAlignment.Right));
                r.Cells.Add (CreateCell (s.CijenaBezUPDV.ToString ("F2"), TextAlignment.Right));
                r.Cells.Add (CreateCell (s.FakVrBezPDV.ToString ("F2"), TextAlignment.Right));
                r.Cells.Add (CreateCell ("0.00", TextAlignment.Right)); // Zavisni troškovi
                r.Cells.Add (CreateCell (s.NabavnaCijenaBezPDV.ToString ("F2"), TextAlignment.Right));
                r.Cells.Add (CreateCell (s.NabavnaVrijednostBezPDV.ToString ("F2"), TextAlignment.Right));
                r.Cells.Add (CreateCell (s.MarzaPostotak.ToString ("F2"), TextAlignment.Right));
                r.Cells.Add (CreateCell (s.RazlikaCijBezPDV.ToString ("F2"), TextAlignment.Right));
                r.Cells.Add (CreateCell (s.ProdajnaVrijBezIPDV.ToString ("F2"), TextAlignment.Right));
                r.Cells.Add (CreateCell (s.PoreskaStopaPostotak?.ToString ("F2") ?? "0", TextAlignment.Center));
                r.Cells.Add (CreateCell (s.IznosIPDV.ToString ("F2"), TextAlignment.Right));
                r.Cells.Add (CreateCell (s.MPVrijednost.ToString ("F2"), TextAlignment.Right));
                r.Cells.Add (CreateCell (s.Cijena?.ToString ("F2") ?? "0", TextAlignment.Right));

                // sumiranje
                sumFakVr += s.FakVrBezPDV;
                sumNabavna += s.NabavnaVrijednostBezPDV;
                sumZavisni += 0; // uvijek 0
                sumRabat += s.Rabat;
                sumProdVr += s.ProdajnaVrijBezIPDV;
                sumIznosPDV += s.IznosIPDV;
                sumMP += s.MPVrijednost;

                index++;
            }

            // === RED SA SUMAMA ===
            TableRow sumRow = new TableRow ();
            bodyGroup.Rows.Add (sumRow);
            sumRow.Cells.Add (CreateCell ("", TextAlignment.Center)); // prazno # kolona
            sumRow.Cells.Add (CreateCell ("UKUPNO", TextAlignment.Left));
            sumRow.Cells.Add (CreateCell ("", TextAlignment.Center));
            sumRow.Cells.Add (CreateCell ("", TextAlignment.Right));
            sumRow.Cells.Add (CreateCell ("", TextAlignment.Right));
            sumRow.Cells.Add (CreateCell (sumFakVr.ToString ("F2"), TextAlignment.Right));
            sumRow.Cells.Add (CreateCell (sumZavisni.ToString ("F2"), TextAlignment.Right));
            sumRow.Cells.Add (CreateCell (sumRabat.ToString ("F2"), TextAlignment.Right));
            sumRow.Cells.Add (CreateCell (sumNabavna.ToString ("F2"), TextAlignment.Right));
            sumRow.Cells.Add (CreateCell ("", TextAlignment.Right));
            sumRow.Cells.Add (CreateCell ("", TextAlignment.Right));
            sumRow.Cells.Add (CreateCell (sumProdVr.ToString ("F2"), TextAlignment.Right));
            sumRow.Cells.Add (CreateCell ("", TextAlignment.Center));
            sumRow.Cells.Add (CreateCell (sumIznosPDV.ToString ("F2"), TextAlignment.Right));
            sumRow.Cells.Add (CreateCell (sumMP.ToString ("F2"), TextAlignment.Right));
            sumRow.Cells.Add (CreateCell ("", TextAlignment.Right));

            // ===== FOOTER =====
            Table footerTable = new Table ();
            footerTable.Columns.Add (new TableColumn { Width = new GridLength (350) });
            footerTable.Columns.Add (new TableColumn { Width = new GridLength (400) });
            footerTable.Columns.Add (new TableColumn { Width = new GridLength (350) });

            TableRowGroup footerGroup = new TableRowGroup ();
            footerTable.RowGroups.Add (footerGroup);
            TableRow footerRow = new TableRow ();
            footerGroup.Rows.Add (footerRow);

            footerRow.Cells.Add (new TableCell (new Paragraph ()) { BorderThickness = new Thickness (0) }); // prazno

            Paragraph sumaPar = new Paragraph ();

            sumaPar.Inlines.Add (new Run ($"UKUPNO: {sumFakVr:F2}")
            {
                FontSize = 12
            });
            sumaPar.Inlines.Add (new LineBreak ());

            sumaPar.Inlines.Add (new Run ($"PDV: {sumIznosPDV:F2}")
            {
                FontSize = 12
            });
            sumaPar.Inlines.Add (new LineBreak ());

            sumaPar.Inlines.Add (new Run ($"UKUPNO ZA PLAĆANJE: {(sumNabavna + sumIznosPDV):F2}")
            {
                FontSize = 12
            });

            // footerRow.Cells.Add(new TableCell(sumaPar));

            Paragraph potpisPar = new Paragraph ();
            potpisPar.Inlines.Add ("                                     Kalkulaciju sačinio: " + Environment.NewLine);
            potpisPar.Inlines.Add ("M.P.                                                          " + Environment.NewLine);
            potpisPar.Inlines.Add ("                                     ____________________");
            footerRow.Cells.Add (new TableCell (potpisPar));

            doc.Blocks.Add (footerTable);

            try
            {
                PrintDialog printDlg = new PrintDialog ();

                // DODAJ OVO PRIJE ShowDialog():
                // 1. Eksplicitno postavi PrintQueue
                LocalPrintServer server = new LocalPrintServer ();
                PrintQueue defaultQueue = server.DefaultPrintQueue;

                // 2. Refresh printer status
                defaultQueue.Refresh ();

                // 3. Ako printer nije dostupan, koristi PDF printer kao fallback
                if(defaultQueue.IsNotAvailable || defaultQueue.IsOffline)
                {
                    var pdfPrinter = server.GetPrintQueues ()
                        .FirstOrDefault (p => p.FullName.Contains ("Microsoft Print to PDF"));

                    if(pdfPrinter != null)
                        printDlg.PrintQueue = pdfPrinter;
                }
                else
                {
                    printDlg.PrintQueue = defaultQueue;
                }

                // 4. Sada pokušaj ShowDialog
                bool? result = printDlg.ShowDialog ();

                if(result == true)
                {
                    printDlg.PrintDocument (((IDocumentPaginatorSource)doc).DocumentPaginator, "Kalkulacija cijena");
                }
            }
            catch(System.Printing.PrintQueueException ex)
            {
                MessageBox.Show ("Ne može se otvoriti dijalog za štampu:\n" + ex.Message);
            }
        }

        private TableCell CreateCell(string text, TextAlignment alignment)
        {
            return new TableCell (new Paragraph (new Run (text)))
            {
                TextAlignment = alignment,
                Padding = new Thickness (5),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness (0.5)
            };
        }

        private int GenerateNextBrojUlaza()
        {
            // Ako već postoje ulazi, uzmemo najveći broj i +1
            return StockIn.Any () ? StockIn.Max (x => x.BrojUlaza) + 1 : 1;
        }

        public async Task LoadArticlesAsync()
        {
            try
            {

                using(var db = new AppDbContext ())
                {
                    var artikli = await db.Artikli.Where (a => a.VrstaArtikla == 0).ToListAsync ();
                    Artikli?.Clear ();
                    ArtikliFilter?.Clear ();
                    foreach(var artikl in artikli)
                    {
                        Artikli?.Add (artikl);
                        ArtikliFilter?.Add (artikl);


                    }

                    SelectedArticle = ArtikliFilter?.FirstOrDefault ();
                    Debug.WriteLine ("BeverageInViewModel ----------------------------Ucitao artikle -----------------------------------------------------------------------------------------------");

                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine ("Exception na LoadArticlesAsync():  " + ex.ToString ());
            }
        }

        public async Task LoadStockInItems(TblUlaz selectedulaz)
        {
            if(selectedulaz == null)
                return;
            try
            {

                using(var db = new AppDbContext ())
                {
                    IznosRacuna = 0;
                    var allStockInitems = await db.UlazStavke.ToListAsync ();
                    var stockInitems = allStockInitems.Where (a => a.BrojUlaza == selectedulaz.BrojUlaza).OrderBy (a => a.RedniBroj).ToList ();
                    StockInItems.Clear ();
                    foreach(var ri in stockInitems)
                    {
                        StockInItems.Add (ri);
                        Debug.WriteLine ("-------------------------------------------------------------------------------------------------------------------Ubacuje u stavke broj ulaza: " + ri.BrojUlaza);

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


        public async Task LoadStockInAsync(int brojulaza)
        {
            Debug.WriteLine ($"LoadStockInAsync started. BrojUlaza: {brojulaza}");

            try
            {
                using(var db = new AppDbContext ())
                {
                    // JOIN ulaza i radnika u jednom upitu
                    var stockIns = await (from u in db.Ulaz
                                          join r in db.Radnici
                                              on u.Radnik equals r.IdRadnika.ToString () into rad
                                          from r2 in rad.DefaultIfEmpty ()
                                          select new TblUlaz
                                          {
                                              IdUlaza = u.IdUlaza,
                                              BrojUlaza = u.BrojUlaza,
                                              Datum = u.Datum,
                                              Dobavljac = u.Dobavljac,
                                              BrojFakture = u.BrojFakture,
                                              IznosFakture = u.IznosFakture,
                                              Locked = u.Locked,
                                              Radnik = u.Radnik,
                                              RadnikName = r2 != null ? r2.Radnik : string.Empty
                                          }).ToListAsync ();

                    StockIn.Clear ();
                    StockInFilter?.Clear ();

                    foreach(var receipt in stockIns)
                    {
                        StockIn.Add (receipt);
                        StockInFilter?.Add (receipt);

                        Debug.WriteLine ($"Adding Ulaz: BrojUlaza={receipt.BrojUlaza}, RadnikName={receipt.RadnikName}");
                    }

                    if(brojulaza != 0)
                    {
                        SelectedStockIn = StockInFilter?.FirstOrDefault (x => x.BrojUlaza == brojulaza);
                        Debug.WriteLine ($"SelectedStockIn for BrojUlaza={brojulaza} is {SelectedStockIn?.BrojUlaza}");
                    }
                    else
                    {
                        SelectedStockIn = StockInFilter?.LastOrDefault ();
                        Debug.WriteLine ($"SelectedStockIn (last) is {SelectedStockIn?.BrojUlaza}");
                    }

                    Debug.WriteLine ($"StockInFilter Count: {StockInFilter?.Count}");

                    await LoadStockInItems (SelectedStockIn);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }


        public async Task LoadSuppliersAsync()
        {
            try
            {

                using(var db = new AppDbContext ())
                {
                    var suppliers = await db.Dobavljaci.ToListAsync ();
                    Suppliers.Clear ();

                    foreach(var supplier in suppliers)
                    {
                        Suppliers.Add (supplier);
                    }
                    Debug.WriteLine ("BeverageInViewModel ----------------------------Ucitao dobavljace-----------------------------------------------------------------------------------------------");

                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine (ex.ToString ());
            }
        }

        public async void FilterItems(string? searchtext)
        {
            Debug.WriteLine ("Okida search");

            var lowerSearch = (searchtext ?? string.Empty).ToLower ();

            var filtered = StockIn
                .Where (a =>
                    (a.BrojUlaza.ToString () ?? string.Empty).ToLower ().Contains (lowerSearch) ||
                    (a.Datum.ToString ("dd.MM.yyyy") ?? string.Empty).ToLower ().Contains (lowerSearch) ||
                    (a.Dobavljac ?? string.Empty).ToLower ().Contains (lowerSearch)
                )
                .ToList ();

            StockInFilter = new ObservableCollection<DatabaseTables.TblUlaz> (filtered);
            // Debug.WriteLine("StockInFilter.Count: " + StockInFilter.Count);
            SelectedStockIn = StockInFilter?.FirstOrDefault (x => x.BrojUlaza != 0);
            // Debug.WriteLine("SelectedStockIn : " + SelectedStockIn.Dobavljac);
            await LoadStockInItems (SelectedStockIn);
        }

        public void FilterArticleItems(string? searcharticletext)
        {
            Debug.WriteLine ("Okida article search");

            var lowerSearch = (searcharticletext ?? string.Empty).ToLower ();

            var filtered = Artikli
                .Where (a =>
                    (a.Sifra.ToString () ?? string.Empty).ToLower ().Contains (lowerSearch) ||
                     (a.Artikl ?? string.Empty).ToLower ().Contains (lowerSearch)
                )
                .ToList ();

            ArtikliFilter = new ObservableCollection<DatabaseTables.TblArtikli> (filtered);
        }



        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
        }
    }
}
