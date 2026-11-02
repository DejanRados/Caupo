
using Caupo.Data;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Caupo.ViewModels
{
    public sealed class DashboardViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private DateTime? _odDatuma = DateTime.Today;
        private DateTime? _doDatuma = DateTime.Today;
        private string _odVrijeme = "00:00";
        private string _doVrijeme = "23:59";
        private string _errorMessage = string.Empty;
        private bool _isLoading;
        private bool _isPeriodicMode;
        private string _selectedReport = "DnevniPromet";
        private decimal _bruto;
        private decimal _reklamirano;
        private decimal _gotovina;
        private decimal _kartica;
        private decimal _cek;
        private decimal _virman;
        private decimal _sank;
        private decimal _kuhinja;
        private decimal _ostalo;
        private decimal _artikliUkupno;

        public ObservableCollection<WorkerSummary> Radnici { get; } = new();
        public ObservableCollection<ArticleSummary> Artikli { get; } = new();

        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand TodayCommand { get; }

        public DashboardViewModel()
        {
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            TodayCommand = new AsyncRelayCommand(ShowTodayAsync);
        }

        public DateTime? OdDatuma
        {
            get => _odDatuma;
            set => Set(ref _odDatuma, value);
        }

        public DateTime? DoDatuma
        {
            get => _doDatuma;
            set => Set(ref _doDatuma, value);
        }

        public string OdVrijeme
        {
            get => _odVrijeme;
            set => Set(ref _odVrijeme, value);
        }

        public string DoVrijeme
        {
            get => _doVrijeme;
            set => Set(ref _doVrijeme, value);
        }

        public bool IsPeriodicMode
        {
            get => _isPeriodicMode;
            set => Set(ref _isPeriodicMode, value);
        }

        public string SelectedReport
        {
            get => _selectedReport;
            set
            {
                if (_selectedReport == value) return;
                _selectedReport = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsArticleReport));
                OnPropertyChanged(nameof(ReportTitle));
            }
        }

        public bool IsArticleReport => SelectedReport.EndsWith("PoArtiklima", StringComparison.Ordinal) ||
            SelectedReport.EndsWith("Sanka", StringComparison.Ordinal) ||
            SelectedReport.EndsWith("Kuhinje", StringComparison.Ordinal) ||
            SelectedReport.EndsWith("Ostalo", StringComparison.Ordinal);

        public string ReportTitle => SelectedReport.EndsWith("Sanka", StringComparison.Ordinal) ? "Promet šanka" :
            SelectedReport.EndsWith("Kuhinje", StringComparison.Ordinal) ? "Promet kuhinje" :
            SelectedReport.EndsWith("Ostalo", StringComparison.Ordinal) ? "Promet ostalo" : "Promet po artiklima";

        public decimal ArtikliUkupno => _artikliUkupno;
        public bool NemaArtikala => Artikli.Count == 0;

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => Set(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => Set(ref _isLoading, value);
        }

        public decimal Bruto => _bruto;
        public decimal Reklamirano => _reklamirano;
        public decimal Neto => Bruto - Reklamirano;
        public decimal Gotovina => _gotovina;
        public decimal Kartica => _kartica;
        public decimal Cek => _cek;
        public decimal Virman => _virman;
        public decimal Sank => _sank;
        public decimal Kuhinja => _kuhinja;
        public decimal Ostalo => _ostalo;
        public bool NemaPrometa => Radnici.Count == 0;

        private async Task ShowTodayAsync()
        {
            if (IsLoading) return;

            OdDatuma = DateTime.Today;
            DoDatuma = DateTime.Today;
            OdVrijeme = "00:00";
            DoVrijeme = "23:59";

            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            if (IsLoading) return;
            if (!TryGetPeriod(out DateTime od, out DateTime doExclusive)) return;

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                await using var db = new AppDbContext();

                var workers = await db.Radnici.AsNoTracking()
                    .Select(x => new { x.IdRadnika, x.Radnik })
                    .ToListAsync();

                var workerNames = workers.ToDictionary(
                    x => x.IdRadnika.ToString(CultureInfo.InvariantCulture),
                    x => x.Radnik ?? string.Empty);

                var summaries = new Dictionary<string, WorkerSummary>();
                var articles = new List<ArticleSummary>();

                decimal bruto = 0m;
                decimal reklamirano = 0m;
                decimal gotovina = 0m;
                decimal kartica = 0m;
                decimal cek = 0m;
                decimal virman = 0m;
                decimal sank = 0m;
                decimal kuhinja = 0m;
                decimal ostalo = 0m;

                await db.Database.OpenConnectionAsync();

                try
                {
                    DbConnection connection = db.Database.GetDbConnection();

                    await using (var command = CreateCommand(connection, PaymentsSql, od, doExclusive))
                    {
                        await using var reader = await command.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            string workerId = reader.IsDBNull(0)
                                ? string.Empty
                                : Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture) ?? string.Empty;

                            int paymentType = reader.IsDBNull(1)
                                ? -1
                                : Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);

                            decimal grossAmount = ReadDecimal(reader, 2);
                            decimal refundedAmount = ReadDecimal(reader, 3);
                            decimal netAmount = grossAmount - refundedAmount;

                            bruto += grossAmount;
                            reklamirano += refundedAmount;

                            switch (paymentType)
                            {
                                case 0: gotovina += netAmount; break;
                                case 1: kartica += netAmount; break;
                                case 2: cek += netAmount; break;
                                case 3: virman += netAmount; break;
                            }

                            if (!summaries.TryGetValue(workerId, out WorkerSummary? worker))
                            {
                                string name = workerNames.TryGetValue(workerId, out string? found)
                                    && !string.IsNullOrWhiteSpace(found)
                                    ? found
                                    : string.IsNullOrWhiteSpace(workerId)
                                        ? "Nepoznat radnik"
                                        : $"Radnik #{workerId}";

                                worker = new WorkerSummary { Ime = name };
                                summaries.Add(workerId, worker);
                            }

                            worker.Bruto += grossAmount;
                            worker.Reklamirano += refundedAmount;

                            switch (paymentType)
                            {
                                case 0: worker.Gotovina += netAmount; break;
                                case 1: worker.Kartica += netAmount; break;
                                case 2: worker.Cek += netAmount; break;
                                case 3: worker.Virman += netAmount; break;
                            }
                        }
                    }

                    await using (var command = CreateCommand(connection, CategoriesSql, od, doExclusive))
                    {
                        await using var reader = await command.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            int type = reader.IsDBNull(0)
                                ? 2
                                : Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);

                            decimal amount = ReadDecimal(reader, 1);

                            switch (type)
                            {
                                case 0: sank += amount; break;
                                case 1: kuhinja += amount; break;
                                default: ostalo += amount; break;
                            }
                        }
                    }

                    if (IsArticleReport)
                    {
                        await using var command = CreateCommand(connection, ArticlesSql, od, doExclusive);
                        await using var reader = await command.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            int type = reader.IsDBNull(4) ? 2 :
                                Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture);

                            if (SelectedReport.EndsWith("Sanka", StringComparison.Ordinal) && type != 0 ||
                                SelectedReport.EndsWith("Kuhinje", StringComparison.Ordinal) && type != 1 ||
                                SelectedReport.EndsWith("Ostalo", StringComparison.Ordinal) && type != 2)
                                continue;

                            int? unit = reader.IsDBNull(1) ? null :
                                Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);

                            articles.Add(new ArticleSummary
                            {
                                Naziv = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                                JedinicaMjere = UnitName(unit),
                                Cijena = ReadDecimal(reader, 2),
                                Kolicina = ReadDecimal(reader, 3)
                            });
                        }
                    }
                }
                finally
                {
                    await db.Database.CloseConnectionAsync();
                }

                _bruto = Math.Round(bruto, 2, MidpointRounding.AwayFromZero);
                _reklamirano = Math.Round(reklamirano, 2, MidpointRounding.AwayFromZero);
                _gotovina = Math.Round(gotovina, 2, MidpointRounding.AwayFromZero);
                _kartica = Math.Round(kartica, 2, MidpointRounding.AwayFromZero);
                _cek = Math.Round(cek, 2, MidpointRounding.AwayFromZero);
                _virman = Math.Round(virman, 2, MidpointRounding.AwayFromZero);
                _sank = Math.Round(sank, 2, MidpointRounding.AwayFromZero);
                _kuhinja = Math.Round(kuhinja, 2, MidpointRounding.AwayFromZero);
                _ostalo = Math.Round(ostalo, 2, MidpointRounding.AwayFromZero);

                Radnici.Clear();

                foreach (var worker in summaries.Values.OrderByDescending(x => x.Neto).ThenBy(x => x.Ime))
                {
                    worker.RoundAmounts();
                    Radnici.Add(worker);
                }

                Artikli.Clear();
                foreach (var article in articles.OrderBy(x => x.Naziv).ThenBy(x => x.Cijena))
                    Artikli.Add(article);

                _artikliUkupno = Artikli.Sum(x => x.Iznos);
                OnPropertyChanged(nameof(ArtikliUkupno));
                OnPropertyChanged(nameof(NemaArtikala));

                NotifyTotals();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Greška pri učitavanju Dashboarda: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool TryGetPeriod(out DateTime od, out DateTime doExclusive)
        {
            od = default;
            doExclusive = default;

            if (!OdDatuma.HasValue || (IsPeriodicMode && !DoDatuma.HasValue))
            {
                ErrorMessage = "Odaberite početni i završni datum.";
                return false;
            }

            if (!IsPeriodicMode)
            {
                od = OdDatuma.Value.Date;
                doExclusive = od.AddDays(1);
                return true;
            }

            const string format = @"hh\:mm";

            if (!TimeSpan.TryParseExact(OdVrijeme, format, CultureInfo.InvariantCulture, out TimeSpan startTime) ||
                !TimeSpan.TryParseExact(DoVrijeme, format, CultureInfo.InvariantCulture, out TimeSpan endTime) ||
                startTime >= TimeSpan.FromDays(1) || endTime >= TimeSpan.FromDays(1))
            {
                ErrorMessage = "Vrijeme mora biti u formatu HH:mm, npr. 18:30.";
                return false;
            }

            od = OdDatuma.Value.Date.Add(startTime);
            doExclusive = DoDatuma.Value.Date.Add(endTime).AddMinutes(1);

            if (doExclusive <= od)
            {
                ErrorMessage = "Završetak perioda mora biti poslije početka.";
                return false;
            }

            return true;
        }

        private static DbCommand CreateCommand(DbConnection connection, string sql, DateTime od, DateTime doExclusive)
        {
            DbCommand command = connection.CreateCommand();
            command.CommandText = sql;

            DbParameter start = command.CreateParameter();
            start.ParameterName = "@od";
            start.DbType = DbType.DateTime;
            start.Value = od;
            command.Parameters.Add(start);

            DbParameter end = command.CreateParameter();
            end.ParameterName = "@do";
            end.DbType = DbType.DateTime;
            end.Value = doExclusive;
            command.Parameters.Add(end);

            return command;
        }

        private static decimal ReadDecimal(DbDataReader reader, int column)
        {
            if (reader.IsDBNull(column)) return 0m;
            return Convert.ToDecimal(reader.GetValue(column), CultureInfo.InvariantCulture);
        }

        private const string PaymentsSql = @"
            WITH ReceiptTotals AS
            (
                SELECT
                    r.BrojRacuna,
                    r.Radnik,
                    r.NacinPlacanja,
                    r.Reklamiran,
                    ROUND(SUM(
                        COALESCE(s.Kolicina, 0) * COALESCE(s.Cijena, 0)
                    ), 2) AS Iznos
                FROM tblRacuni r
                INNER JOIN tblRacunStavke s ON s.BrojRacuna = r.BrojRacuna
                WHERE r.Datum >= @od
                  AND r.Datum < @do
                  AND (r.TipRacuna IS NULL OR r.TipRacuna <> 'Storno' COLLATE NOCASE)
                GROUP BY
                    r.BrojRacuna,
                    r.Radnik,
                    r.NacinPlacanja,
                    r.Reklamiran
            )
            SELECT
                Radnik,
                NacinPlacanja,
                COALESCE(SUM(Iznos), 0) AS Bruto,
                COALESCE(SUM(
                    CASE WHEN Reklamiran = 'DA' COLLATE NOCASE THEN Iznos ELSE 0 END
                ), 0) AS Reklamirano
            FROM ReceiptTotals
            GROUP BY Radnik, NacinPlacanja;";

        private const string CategoriesSql = @"
            SELECT
                COALESCE(s.VrstaArtikla, 2) AS Vrsta,
                COALESCE(SUM(
                    CASE WHEN r.Reklamiran = 'DA' COLLATE NOCASE THEN 0
                    ELSE COALESCE(s.Kolicina, 0) * COALESCE(s.Cijena, 0)
                    END
                ), 0) AS Iznos
            FROM tblRacunStavke s
            INNER JOIN tblRacuni r ON r.BrojRacuna = s.BrojRacuna
            WHERE r.Datum >= @od
              AND r.Datum < @do
              AND (r.TipRacuna IS NULL OR r.TipRacuna <> 'Storno' COLLATE NOCASE)
            GROUP BY COALESCE(s.VrstaArtikla, 2);";

        private const string ArticlesSql = @"
            SELECT COALESCE(s.Artikl, ''), s.JedinicaMjere, COALESCE(s.Cijena, 0),
                   SUM(COALESCE(s.Kolicina, 0)), COALESCE(s.VrstaArtikla, 2)
            FROM tblRacunStavke s
            INNER JOIN tblRacuni r ON r.BrojRacuna = s.BrojRacuna
            WHERE r.Datum >= @od AND r.Datum < @do
              AND (r.TipRacuna IS NULL OR r.TipRacuna <> 'Storno' COLLATE NOCASE)
              AND (r.Reklamiran IS NULL OR r.Reklamiran <> 'DA' COLLATE NOCASE)
            GROUP BY s.IdArtikla, s.Sifra, s.Artikl, s.JedinicaMjere,
                     s.Cijena, COALESCE(s.VrstaArtikla, 2);";

        private static string UnitName(int? unit) => unit switch
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

        public sealed class ArticleSummary
        {
            public string Naziv { get; set; } = string.Empty;
            public string JedinicaMjere { get; set; } = string.Empty;
            public decimal Cijena { get; set; }
            public decimal Kolicina { get; set; }
            public decimal Iznos => Cijena * Kolicina;
        }

        private void NotifyTotals()
        {
            OnPropertyChanged(nameof(Bruto));
            OnPropertyChanged(nameof(Reklamirano));
            OnPropertyChanged(nameof(Neto));
            OnPropertyChanged(nameof(Gotovina));
            OnPropertyChanged(nameof(Kartica));
            OnPropertyChanged(nameof(Cek));
            OnPropertyChanged(nameof(Virman));
            OnPropertyChanged(nameof(Sank));
            OnPropertyChanged(nameof(Kuhinja));
            OnPropertyChanged(nameof(Ostalo));
            OnPropertyChanged(nameof(NemaPrometa));
        }

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnPropertyChanged(name);
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public sealed class WorkerSummary
        {
            public string Ime { get; set; } = string.Empty;
            public decimal Bruto { get; set; }
            public decimal Reklamirano { get; set; }
            public decimal Neto => Bruto - Reklamirano;
            public decimal Gotovina { get; set; }
            public decimal Kartica { get; set; }
            public decimal Cek { get; set; }
            public decimal Virman { get; set; }

            public void RoundAmounts()
            {
                Bruto = Math.Round(Bruto, 2, MidpointRounding.AwayFromZero);
                Reklamirano = Math.Round(Reklamirano, 2, MidpointRounding.AwayFromZero);
                Gotovina = Math.Round(Gotovina, 2, MidpointRounding.AwayFromZero);
                Kartica = Math.Round(Kartica, 2, MidpointRounding.AwayFromZero);
                Cek = Math.Round(Cek, 2, MidpointRounding.AwayFromZero);
                Virman = Math.Round(Virman, 2, MidpointRounding.AwayFromZero);
            }
        }
    }
}
