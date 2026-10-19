
using Caupo.Data;
using Caupo.Models;
using Caupo.Properties;
using Caupo.Services;
using Caupo.Views;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Caupo.ViewModels
{
    public sealed class KnjigaKuhinjeViewModel : INotifyPropertyChanged
    {
        private readonly KnjigaKuhinjeService _service;
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private bool _initialized;
        private int _requestVersion;
        private DateTime _odabraniDatum = DateTime.Today;
        private decimal _total;
        private DatabaseTables.TblFirma? _firma;
        private bool _isBusy;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<StavkaKnjigeKuhinje> Knjiga { get; } = new();
        public ObservableCollection<Jelo> ListaHrane { get; } = new();
        public DatabaseTables.TblFirma? Firma
        {
            get => _firma;
            private set { _firma = value; OnPropertyChanged(); }
        }
        public DateTime OdabraniDatum
        {
            get => _odabraniDatum;
            set
            {
                if (_odabraniDatum.Date == value.Date) return;
                _odabraniDatum = value.Date;
                OnPropertyChanged();
                _ = RefreshAsync();
            }
        }
        public decimal Total
        {
            get => _total;
            private set { _total = value; OnPropertyChanged(); }
        }
        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); }
        }
        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand PrintCommand { get; }

        public KnjigaKuhinjeViewModel(KnjigaKuhinjeService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            PreviousDayCommand = new RelayCommand(() => OdabraniDatum = OdabraniDatum.AddDays(-1));
            NextDayCommand = new RelayCommand(() => OdabraniDatum = OdabraniDatum.AddDays(1));
            TodayCommand = new RelayCommand(() => OdabraniDatum = DateTime.Today);
            PrintCommand = new RelayCommand(PrintReport);
            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            if (_initialized) return;
            _initialized = true;
            LoadFirma();
            try
            {
                IsBusy = true;
                await _service.SinhronizujDoDanasAsync();
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                _initialized = false;
                Debug.WriteLine("[KNJIGA KUHINJE] Inicijalizacija: " + ex);
                ShowError("Knjiga kuhinje nije mogla biti obračunata.", ex);
            }
            finally { IsBusy = false; }
        }

        public async Task RefreshAsync()
        {
            int version = Interlocked.Increment(ref _requestVersion);
            DateTime dan = OdabraniDatum.Date;
            await _loadLock.WaitAsync();
            try
            {
                if (version != _requestVersion) return;
                IsBusy = true;
                var redovi = await _service.GetKnjigaZaDanAsync(dan);
                var jela = await _service.GetJelaZaDanAsync(dan);
                if (version != _requestVersion) return;

                Knjiga.Clear();
                foreach (var r in redovi) Knjiga.Add(r);
                ListaHrane.Clear();
                int i = 1;
                foreach (var j in jela.OrderBy(x => x.Naziv))
                {
                    ListaHrane.Add(new Jelo
                    {
                        RedniBroj = i++,
                        Naziv = j.Naziv,
                        Kolicina = j.Kolicina,
                        Vrijednost = j.Vrijednost,
                        Cijena = j.Kolicina != 0 ? j.Vrijednost / j.Kolicina : 0m
                    });
                }
                Total = ListaHrane.Sum(x => x.Vrijednost ?? 0m);
                Debug.WriteLine($"[KNJIGA KUHINJE] {dan:dd.MM.yyyy}: {Knjiga.Count} namirnica, {ListaHrane.Count} jela");
            }
            catch (Exception ex)
            {
                if (version == _requestVersion)
                {
                    Debug.WriteLine("[KNJIGA KUHINJE] Učitavanje: " + ex);
                    ShowError("Nije moguće učitati dnevni list kuhinje.", ex);
                }
            }
            finally
            {
                if (version == _requestVersion) IsBusy = false;
                _loadLock.Release();
            }
        }

        // Kompatibilnost s postojećim code-behind dok ne prilagodimo XAML/stranicu.
        public Task GetJelaZaOdabraniDatumAsync() => RefreshAsync();

        private void LoadFirma()
        {
            Firma = new DatabaseTables.TblFirma
            {
                NazivFirme = Settings.Default.Firma,
                Adresa = Settings.Default.Adresa,
                Grad = Settings.Default.Mjesto,
                JIB = Settings.Default.JIB,
                PDV = Settings.Default.PDV
            };
        }

        private static void ShowError(string message, Exception ex)
        {
            var popup = new MyMessageBox { WindowStartupLocation = WindowStartupLocation.CenterScreen };
            popup.MessageTitle.Text = "GREŠKA - KNJIGA KUHINJE";
            popup.MessageText.Text = message + "\n\n" + ex.Message;
            popup.ShowDialog();
        }

        public void PrintReport() => PrintReport(Knjiga, Firma?.NazivFirme ?? "",
            Firma?.Adresa ?? "", Firma?.Grad ?? "", Firma?.JIB ?? "",
            Firma?.PDV ?? "", OdabraniDatum, Total);

        // Stari potpis ostaje dostupan dok code-behind ne prebacimo na PrintCommand.
        public void PrintReport(ObservableCollection<StavkaKnjigeKuhinje> knjiga,
            string? nazivFirme, string? adresa, string? grad, string? jib,
            string? pdv, DateTime datum, decimal? total)
        {
            try
            {
                var doc = new FlowDocument
                {
                    PageWidth = 1122,
                    PageHeight = 793,
                    ColumnWidth = double.PositiveInfinity,
                    PagePadding = new Thickness(32),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 10
                };
                var zaglavlje = new Paragraph
                {
                    TextAlignment = TextAlignment.Center,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                zaglavlje.Inlines.Add("DNEVNI LIST KUHINJE");
                doc.Blocks.Add(zaglavlje);
                doc.Blocks.Add(new Paragraph(new Run(
                    $"{nazivFirme}  |  {adresa}, {grad}  |  JIB: {jib}  |  PDV: {pdv}  |  Datum: {datum:dd.MM.yyyy}")));

                var glavni = new Table { CellSpacing = 4 };
                glavni.Columns.Add(new TableColumn { Width = new GridLength(655) });
                glavni.Columns.Add(new TableColumn { Width = new GridLength(375) });
                var rg = new TableRowGroup(); glavni.RowGroups.Add(rg);
                var row = new TableRow(); rg.Rows.Add(row);

                var namirnice = MakeTable(new[] { "#", "Namirnica", "JM", "Preneseno", "Primljeno", "Dobavljač", "Dokument", "Na stanju", "Utrošeno", "Ostatak" },
                    new[] { 22d, 112, 28, 62, 60, 88, 65, 65, 70, 75 });
                foreach (var s in knjiga)
                {
                    AddRow(namirnice, new[] {
                        s.RedniBroj?.ToString() ?? "", s.Namirnica ?? "", s.JedinicaMjere ?? "",
                        F(s.OstatakOdJuce), F(s.NabavljenoDanas), s.Dobavljac ?? "",
                        s.Dokument ?? "", F(s.NaStanju), F(s.UtrosenoDanas), F(s.OstatakZaSutra)
                    });
                }
                var hrana = MakeTable(new[] { "#", "Jelo", "Kol.", "Cijena", "Vrijednost" },
                    new[] { 28d, 157, 56, 63, 71 });
                foreach (var j in ListaHrane)
                    AddRow(hrana, new[] { j.RedniBroj?.ToString() ?? "", j.Naziv ?? "",
                        F(j.Kolicina), F(j.Cijena), F(j.Vrijednost) });
                AddRow(hrana, new[] { "", "UKUPNO", "", "", F(total) });

                var left = new TableCell(); left.Blocks.Add(namirnice); row.Cells.Add(left);
                var right = new TableCell(); right.Blocks.Add(hrana); row.Cells.Add(right);
                doc.Blocks.Add(glavni);
                doc.Blocks.Add(new Paragraph(new Run("POTPIS: ______________________________"))
                { TextAlignment = TextAlignment.Right });

                var dialog = new PrintDialog();
                if (dialog.PrintTicket != null)
                    dialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
                if (dialog.ShowDialog() == true)
                    dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator,
                        $"KnjigaKuhinje_{datum:dd_MM_yyyy}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[KNJIGA KUHINJE] Štampa: " + ex);
                ShowError("Štampanje nije uspjelo.", ex);
            }
        }

        private static string F(decimal? x) => (x ?? 0m).ToString("0.00#");
        private static Table MakeTable(string[] headers, double[] widths)
        {
            var table = new Table { CellSpacing = 0 };
            foreach (var w in widths) table.Columns.Add(new TableColumn { Width = new GridLength(w) });
            var group = new TableRowGroup(); table.RowGroups.Add(group);
            AddRow(table, headers, true);
            return table;
        }
        private static void AddRow(Table table, string[] values, bool header = false)
        {
            var row = new TableRow(); table.RowGroups[0].Rows.Add(row);
            foreach (var v in values)
            {
                var cell = new TableCell(new Paragraph(new Run(v)) { Margin = new Thickness(0) })
                {
                    Padding = new Thickness(2),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(.5),
                    TextAlignment = header ? TextAlignment.Center : TextAlignment.Left,
                    FontWeight = header ? FontWeights.Bold : FontWeights.Normal
                };
                row.Cells.Add(cell);
            }
        }

        public sealed class Jelo
        {
            public int? RedniBroj { get; set; }
            public string? Naziv { get; set; }
            public decimal? Kolicina { get; set; }
            public decimal? Cijena { get; set; }
            public decimal? Vrijednost { get; set; }
        }
    }
}
