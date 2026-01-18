using Caupo.Data;
using Caupo.Fiscal;
using Caupo.Helpers;
using Caupo.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace Caupo.Views
{
    /// <summary>
    /// Interaction logic for OrderPage.xaml
    /// </summary>
    public partial class OrderPage : UserControl
    {

        private OrderViewModel orderViewModel;
        private OrdersViewModel ordersViewModel;
        public OrderPage(OrdersViewModel _ordersViewModel)
        {
            ordersViewModel = _ordersViewModel;
            orderViewModel = new OrderViewModel (ordersViewModel);
            this.DataContext = orderViewModel;

            InitializeComponent ();
            lblUlogovaniKorisnik.Content = Globals.ulogovaniKorisnik.Radnik;
        }


        private void ListnarudzbeStavke_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var listView = sender as DataGrid;
            if(listView != null)
            {
                var clickedItem = listView.SelectedItem as DatabaseTables.TblNarudzbeStavke;
                if(clickedItem != null && DataContext is OrderViewModel viewModel)
                {
                    BorderRacunStavke.Visibility = Visibility.Visible;
                    ViewKolicina.Visibility = Visibility.Visible;
                    lblIznosGostRacun.Visibility = Visibility.Visible;
                    ListGostRacunStavke.Visibility = Visibility.Visible;

                    if(decimal.TryParse (txtKolicina.Text, out decimal kolicina))
                    {
                        viewModel.PrebaciStavku (clickedItem, kolicina);
                    }
                    else
                    {
                        MessageBox.Show ("Molimo unesite validnu količinu.");
                    }
                }
            }

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if(Globals.forma == "Kasa")
            {

                var page = new KasaPage ();
                page.DataContext = new KasaViewModel ();

                PageNavigator.NavigateWithFade (page);


            }
            else
            {
                var page = new OrdersPage (null);
                page.DataContext = new OrdersViewModel (null);
                PageNavigator.NavigateWithFade (page);

            }

        }


        private async void BtnRacun_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine ("[Start] BtnRacun_Click pokrenut");

            if(DataContext is OrderViewModel viewModel)
            {
                RacunIdikator.Visibility = Visibility.Visible;
                Debug.WriteLine ("[Info] DataContext je OrderViewModel");

                var racun = new FiskalniRacun ();
                bool result = false;

                if(viewModel.GostRacunStavke != null && viewModel.GostRacunStavke.Count > 0)
                {
                    Debug.WriteLine ($"[Info] GostRacunStavke ima {viewModel.GostRacunStavke.Count} stavki, pozivam KreirajStavkeRacunaPodjela");
                    await viewModel.KreirajStavkeRacunaPodjela ();
                    result = await racun.IzdajFiskalniRacun ("Training", "Sale", null, null, viewModel.StavkeRacuna, viewModel.SelectedKupac, cmbNacinPlacanja.SelectedIndex, viewModel.TotalSumGostRacun);

                    if(result)
                    {
                        using(var db = new AppDbContext ())
                        {
                            foreach(var stavka in viewModel.GostRacunStavke)
                            {
                                var stavkaZaBrisanje = db.NarudzbeStavke.Where (x => x.Name == stavka.Name && x.IdNarudzbe == viewModel.IdStola).FirstOrDefault ();
                                if(stavkaZaBrisanje != null)
                                {
                                    Debug.WriteLine ($"[DB] Nalazim {stavkaZaBrisanje.Name} stavki za brisanje iz NarudzbeStavke");
                                    db.NarudzbeStavke.Remove (stavkaZaBrisanje);
                                }
                                else
                                {
                                    Debug.WriteLine ($"[DB] Stavka sa Id {stavka.IdStavke} nije pronađena");
                                }
                                Debug.WriteLine ("[DB] Stavka uspešno obrisane iz baze");
                            }
                            await db.SaveChangesAsync ();
                        }
                        Debug.WriteLine ("[Info] Brišem GostRacunStavke i sakrivam UI elemente");
                        viewModel.GostRacunStavke.Clear ();
                        viewModel.StavkeRacuna.Clear ();
                        await viewModel.UpdateTotalSum ();
                        BorderRacunStavke.Visibility = Visibility.Collapsed;
                        ListGostRacunStavke.Visibility = Visibility.Collapsed;
                        lblKolicina.Visibility = Visibility.Collapsed;
                        txtKolicina.Visibility = Visibility.Collapsed;
                        lblIznosGostRacun.Visibility = Visibility.Collapsed;
                        RacunIdikator.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    Debug.WriteLine ("[Info] GostRacunStavke je prazan ili null, pozivam KreirajStavkeRacunaUkupno");
                    await viewModel.KreirajStavkeRacunaUkupno ();
                    result = await racun.IzdajFiskalniRacun ("Training", "Sale", null, null, viewModel.StavkeRacuna, viewModel.SelectedKupac, cmbNacinPlacanja.SelectedIndex, viewModel.TotalSum);
                    if(result)
                    {
                        Debug.WriteLine ("[Info] Uspešno izdavanje racuna, čistim StavkeRacuna i GostRacunStavke");
                        viewModel.StavkeRacuna.Clear ();
                        viewModel.GostRacunStavke.Clear ();
                        Debug.WriteLine ("[Info] Otvaram AppDbContext za brisanje stavki iz baze");
                        using(var db = new AppDbContext ())
                        {

                            var stavkeZaBrisanje = db.NarudzbeStavke.Where (x => x.IdNarudzbe == viewModel.IdStola).ToList ();

                            Debug.WriteLine ($"[DB] Nalazim {stavkeZaBrisanje.Count} stavki za brisanje iz NarudzbeStavke");

                            db.NarudzbeStavke.RemoveRange (stavkeZaBrisanje);
                            await db.SaveChangesAsync ();
                            Debug.WriteLine ("[DB] Stavke uspešno obrisane iz baze");
                            await viewModel.UpdateTotalSum ();
                            Debug.WriteLine ($"[Info] Izdavanje fiskalnog racuna završeno, rezultat: {result}");
                            Debug.WriteLine ("[Info] Pozivam CloseButton_Click da zatvorim prozor");
                            RacunIdikator.Visibility = Visibility.Collapsed;
                            CloseButton_Click (null, null);
                            Debug.WriteLine ("[End] BtnRacun_Click završeno");
                        }
                    }
                }
                RacunIdikator.Visibility = Visibility.Collapsed;
            }
            else
            {
                RacunIdikator.Visibility = Visibility.Collapsed;
                Debug.WriteLine ("[Warning] DataContext NIJE OrderViewModel!");
            }
        }


        private void ListGostRacunStavke_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var listView = sender as DataGrid;
            if(listView != null)
            {
                var clickedItem = listView.SelectedItem as DatabaseTables.TblNarudzbeStavke;
                if(clickedItem != null && DataContext is OrderViewModel viewModel)
                {
                    if(decimal.TryParse (txtKolicina.Text, out decimal kolicina))
                    {
                        viewModel.VratiStavku (clickedItem, kolicina);
                    }
                    else
                    {
                        MessageBox.Show ("Molimo unesite validnu količinu.");
                    }
                }

                // Nakon vraćanja stavke, proveravamo da li je lista prazna
                if(listView.Items.Count == 0)
                {
                    BorderRacunStavke.Visibility = Visibility.Collapsed;
                    ViewKolicina.Visibility = Visibility.Collapsed;
                    lblIznosGostRacun.Visibility = Visibility.Collapsed;
                }
            }

        }
    }
}
