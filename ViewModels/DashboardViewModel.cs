using Caupo.Data;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

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
        private decimal _bruto, _reklamirano, _gotovina, _kartica, _cek, _virman;
        private decimal _sank, _kuhinja, _ostalo, _artikliUkupno;
        private WorkerSummary? _selectedWorker;
        private Dictionary<string, List<ArticleSummary>> _workerArticles = new();
        private readonly Dictionary<(bool Yearly, int Year, int Month), decimal[,]> _turnoverCache = new();
        private DateTime _turnoverPeriod = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        private bool _turnoverYearly, _turnoverLoading;
        private int _turnoverRequest;
        private decimal[,]? _turnoverDays;
        private ImageSource? _turnoverImage;
        private string _turnoverError = string.Empty;

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
                OnPropertyChanged(nameof(IsWorkerReport));
                OnPropertyChanged(nameof(IsConsumptionReport));
                OnPropertyChanged(nameof(IsStockReport));
                OnPropertyChanged(nameof(IsQuantityReport));
                OnPropertyChanged(nameof(IsNormalArticleReport));
                OnPropertyChanged(nameof(QuantityHeader));
                OnPropertyChanged(nameof(ReportTitle));
            }
        }

        public bool IsWorkerReport => SelectedReport.EndsWith("PoRadniku", StringComparison.Ordinal);
        public bool IsConsumptionReport => SelectedReport.EndsWith("UtrosakNamirnica", StringComparison.Ordinal);
        public bool IsStockReport => SelectedReport == "ZalihaNamirnica";
        public bool IsQuantityReport => IsConsumptionReport || IsStockReport;
        public bool IsNormalArticleReport => IsArticleReport && !IsQuantityReport;
        public string QuantityHeader => IsStockReport ? "ZALIHA" : "UTROŠENO";

        public bool IsArticleReport => IsWorkerReport || IsQuantityReport ||
            SelectedReport.EndsWith("PoArtiklima", StringComparison.Ordinal) ||
            SelectedReport.EndsWith("Sanka", StringComparison.Ordinal) ||
            SelectedReport.EndsWith("Kuhinje", StringComparison.Ordinal) ||
            SelectedReport.EndsWith("Ostalo", StringComparison.Ordinal);

        public string ReportTitle => IsStockReport ? "Zaliha namirnica" :
            IsConsumptionReport ? "Utrošak namirnica" :
            IsWorkerReport ? "Promet po radniku" :
            SelectedReport.EndsWith("Sanka", StringComparison.Ordinal) ? "Promet šanka" :
            SelectedReport.EndsWith("Kuhinje", StringComparison.Ordinal) ? "Promet kuhinje" :
            SelectedReport.EndsWith("Ostalo", StringComparison.Ordinal) ? "Promet ostalo" : "Promet po artiklima";

        public WorkerSummary? SelectedWorker
        {
            get => _selectedWorker;
            set
            {
                if (ReferenceEquals(_selectedWorker, value)) return;
                _selectedWorker = value;
                OnPropertyChanged();
                if (IsWorkerReport) ShowWorkerArticles();
            }
        }

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

        public bool TurnoverYearly => _turnoverYearly;
        public bool TurnoverLoading => _turnoverLoading;
        public ImageSource? TurnoverImage => _turnoverImage;
        public string TurnoverError => _turnoverError;
        public string TurnoverModeText => _turnoverYearly ? "Godišnji" : "Mjesečni";
        public string TurnoverPeriodText => _turnoverYearly ? _turnoverPeriod.ToString("yyyy", CultureInfo.InvariantCulture) :
            _turnoverPeriod.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("bs-BA"));

        public async Task ToggleTurnoverModeAsync()
        {
            _turnoverYearly = !_turnoverYearly;
            _turnoverPeriod = new DateTime(DateTime.Today.Year, _turnoverYearly ? 1 : DateTime.Today.Month, 1);
            OnPropertyChanged(nameof(TurnoverModeText));
            OnPropertyChanged(nameof(TurnoverPeriodText));
            await LoadTurnoverAsync();
        }

        public async Task ShiftTurnoverAsync(int direction)
        {
            _turnoverPeriod = _turnoverYearly ? _turnoverPeriod.AddYears(direction) : _turnoverPeriod.AddMonths(direction);
            OnPropertyChanged(nameof(TurnoverPeriodText));
            await LoadTurnoverAsync();
        }

        public async Task LoadTurnoverAsync()
        {
            int request = ++_turnoverRequest;
            DateTime start = new(_turnoverPeriod.Year, _turnoverYearly ? 1 : _turnoverPeriod.Month, 1);
            DateTime end = _turnoverYearly ? start.AddYears(1) : start.AddMonths(1);
            var key = (_turnoverYearly, start.Year, _turnoverYearly ? 0 : start.Month);
            _turnoverLoading = true;
            _turnoverDays = null;
            _turnoverImage = null;
            OnPropertyChanged(nameof(TurnoverImage));
            _turnoverError = string.Empty;
            OnPropertyChanged(nameof(TurnoverLoading));
            OnPropertyChanged(nameof(TurnoverError));

            try
            {
                if (!_turnoverCache.TryGetValue(key, out decimal[,]? days))
                {
                    days = new decimal[(end - start).Days, 3];
                    await using var db = new AppDbContext();
                    await db.Database.OpenConnectionAsync();
                    try
                    {
                        await using DbCommand command = CreateCommand(db.Database.GetDbConnection(), TurnoverSql, start, end);
                        await using DbDataReader reader = await command.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            string dateText = reader.GetString(0);
                            if (!DateTime.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out DateTime day)) continue;
                            int dayIndex = (day - start).Days;
                            int type = reader.IsDBNull(1) ? 2 : Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);
                            if (dayIndex >= 0 && dayIndex < days.GetLength(0))
                                days[dayIndex, type is 0 or 1 ? type : 2] += ReadDecimal(reader, 2);
                        }
                    }
                    finally { await db.Database.CloseConnectionAsync(); }
                    _turnoverCache[key] = days;
                }
                if (request != _turnoverRequest) return;
                _turnoverDays = days;
                _turnoverImage = BuildTurnoverImage(days, start);
                OnPropertyChanged(nameof(TurnoverImage));
            }
            catch (Exception ex)
            {
                if (request != _turnoverRequest) return;
                _turnoverDays = null;
                _turnoverImage = null;
                _turnoverError = "Greška grafikona: " + ex.Message;
                OnPropertyChanged(nameof(TurnoverImage));
                OnPropertyChanged(nameof(TurnoverError));
            }
            finally
            {
                if (request == _turnoverRequest)
                {
                    _turnoverLoading = false;
                    OnPropertyChanged(nameof(TurnoverLoading));
                }
            }
        }

        public DateTime? GetTurnoverDate(double fraction)
        {
            if (_turnoverDays == null || !double.IsFinite(fraction)) return null;

            int count = _turnoverDays.GetLength(0);
            double slotWidth = count <= 31 ? 14d : 3d;
            double plotWidth = count * slotWidth;
            double totalWidth = 52d + plotWidth + 4d;
            double x = fraction * totalWidth - 52d;

            if (x < 0d || x >= plotWidth) return null;

            int index = (int)(x / slotWidth);
            return new DateTime(_turnoverPeriod.Year, _turnoverYearly ? 1 : _turnoverPeriod.Month, 1).AddDays(index);
        }

        public string? GetTurnoverTooltip(double fraction)
        {
            DateTime? day = GetTurnoverDate(fraction);
            if (day == null || _turnoverDays == null) return null;
            DateTime start = new(_turnoverPeriod.Year, _turnoverYearly ? 1 : _turnoverPeriod.Month, 1);
            int index = (day.Value - start).Days;
            decimal sank = _turnoverDays[index, 0], kuhinja = _turnoverDays[index, 1], ostalo = _turnoverDays[index, 2];
            return $"{day:dd.MM.yyyy.}\nŠank: {sank:N2}\nKuhinja: {kuhinja:N2}\nOstalo: {ostalo:N2}\nUkupno: {sank + kuhinja + ostalo:N2}";
        }

        private static ImageSource BuildTurnoverImage(decimal[,] days, DateTime start)
        {
            int count = days.GetLength(0);
            bool monthly = count <= 31;

            const double leftAxisWidth = 52d;
            const double topPadding = 5d;
            const double rightPadding = 4d;

            double slotWidth = monthly ? 14d : 3d;
            double barWidth = slotWidth * 0.55d;
            double chartHeight = 156d;
            double bottomLabelHeight = monthly ? 18d : 4d;

            double plotWidth = count * slotWidth;
            double totalWidth = leftAxisWidth + plotWidth + rightPadding;
            double totalHeight = topPadding + chartHeight + bottomLabelHeight;

            decimal max = 0m;
            for (int i = 0; i < count; i++)
                max = Math.Max(max, days[i, 0] + days[i, 1] + days[i, 2]);

            decimal step = max <= 2000m ? 0m : max <= 5000m ? 1000m : Math.Ceiling(max / 4000m) * 1000m;
            decimal axisTop = max <= 2000m ? 2000m : Math.Ceiling(max / step) * step;

            List<decimal> axisLabels = max <= 2000m
                ? new List<decimal> { 100m, 500m, 1000m, 2000m }
                : Enumerable.Range(1, (int)(axisTop / step)).Select(i => i * step).ToList();

            Brush[] colors =
            {
        new SolidColorBrush(Color.FromRgb(52, 138, 215)),
        new SolidColorBrush(Color.FromRgb(241, 171, 63)),
        new SolidColorBrush(Color.FromRgb(41, 174, 145))
    };

            var gridBrush = new SolidColorBrush(Color.FromArgb(45, 140, 150, 155));
            var labelBrush = new SolidColorBrush(Color.FromRgb(185, 195, 200));
            var drawing = new DrawingGroup();

            using (DrawingContext dc = drawing.Open())
            {
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, totalWidth, totalHeight));

                double baseline = topPadding + chartHeight;

                foreach (decimal value in axisLabels)
                {
                    double y = baseline - (double)(value / axisTop) * chartHeight;
                    dc.DrawRectangle(gridBrush, null, new Rect(leftAxisWidth, y, plotWidth, 0.45));

                    var label = new FormattedText(
                        value.ToString("N0", CultureInfo.InvariantCulture),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        9,
                        labelBrush,
                        1.0);

                    dc.DrawText(label, new Point(leftAxisWidth - label.Width - 7, y - label.Height / 2));
                }

                for (int i = 0; i < count; i++)
                {
                    double x = leftAxisWidth + i * slotWidth + (slotWidth - barWidth) / 2d;
                    double bottom = baseline;

                    for (int type = 0; type < 3; type++)
                    {
                        decimal value = days[i, type];
                        if (value <= 0m) continue;

                        double height = (double)(value / axisTop) * chartHeight;
                        bottom -= height;
                        dc.DrawRectangle(colors[type], null, new Rect(x, bottom, barWidth, height));
                    }

                    if (monthly)
                    {
                        var label = new FormattedText(
                            (i + 1).ToString(CultureInfo.InvariantCulture),
                            CultureInfo.InvariantCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Segoe UI"),
                            8,
                            labelBrush,
                            1.0);

                        double labelX = leftAxisWidth + i * slotWidth + (slotWidth - label.Width) / 2d;
                        dc.DrawText(label, new Point(labelX, baseline + 2d));
                    }
                    else if (i > 0 && start.AddDays(i).Day == 1)
                    {
                        double monthX = leftAxisWidth + i * slotWidth;
                        dc.DrawRectangle(gridBrush, null, new Rect(monthX, topPadding, 0.6, chartHeight));
                    }
                }
            }

            drawing.Freeze();
            var image = new DrawingImage(drawing);
            image.Freeze();
            return image;
        }

        private const string TurnoverSql = @"
            SELECT substr(r.Datum, 1, 10), COALESCE(s.VrstaArtikla, 2),
                   COALESCE(SUM(CASE WHEN r.Reklamiran = 'DA' COLLATE NOCASE THEN 0
                       ELSE COALESCE(s.Kolicina, 0) * COALESCE(s.Cijena, 0) END), 0)
            FROM tblRacuni r
            INNER JOIN tblRacunStavke s ON s.BrojRacuna = r.BrojRacuna
            WHERE r.Datum >= @od AND r.Datum < @do
              AND (r.TipRacuna IS NULL OR r.TipRacuna <> 'Storno' COLLATE NOCASE)
            GROUP BY substr(r.Datum, 1, 10), COALESCE(s.VrstaArtikla, 2);";

        public IReadOnlyList<PieSlice> PaymentPie => BuildPie(
            ("Gotovina", Gotovina, Color.FromRgb(52, 138, 215)),
            ("Kartica", Kartica, Color.FromRgb(41, 174, 145)),
            ("Ček", Cek, Color.FromRgb(241, 171, 63)),
            ("Virman", Virman, Color.FromRgb(156, 107, 218)));

        public IReadOnlyList<PieSlice> CategoryPie => BuildPie(
            ("Šank", Sank, Color.FromRgb(52, 138, 215)),
            ("Kuhinja", Kuhinja, Color.FromRgb(241, 171, 63)),
            ("Ostalo", Ostalo, Color.FromRgb(41, 174, 145)));

        public IReadOnlyList<PieSlice> WorkerPie => BuildPie(Radnici.Select(worker =>
        {
            int id = int.TryParse(worker.Id, out int value) ? value : 0;
            return (worker.Ime, worker.Neto, WorkerColor(id));
        }).ToArray());

        private static Color WorkerColor(int index)
        {
            double hue = (index * 137.508) % 360;
            double chroma = 0.72 * 0.88;
            double x = chroma * (1 - Math.Abs((hue / 60) % 2 - 1));
            double m = 0.88 - chroma;

            (double r, double g, double b) = hue switch
            {
                < 60 => (chroma, x, 0d),
                < 120 => (x, chroma, 0d),
                < 180 => (0d, chroma, x),
                < 240 => (0d, x, chroma),
                < 300 => (x, 0d, chroma),
                _ => (chroma, 0d, x)
            };

            return Color.FromRgb((byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
        }

        private static IReadOnlyList<PieSlice> BuildPie(params (string Name, decimal Value, Color Color)[] values)
        {
            const double center = 66, radius = 62, labelRadius = 39;
            var valid = values.Where(x => x.Value > 0).ToArray();
            decimal total = valid.Sum(x => x.Value);
            if (total <= 0) return Array.Empty<PieSlice>();

            var result = new List<PieSlice>(valid.Length);
            double angle = -90;

            foreach (var (name, value, color) in valid)
            {
                double percentage = (double)(value / total * 100m);
                double sweep = percentage * 3.6;
                double middle = (angle + sweep / 2) * Math.PI / 180;
                Geometry geometry;

                if (sweep >= 359.999)
                {
                    geometry = new EllipseGeometry(new Point(center, center), radius, radius);
                }
                else
                {
                    double startRadians = angle * Math.PI / 180;
                    double endRadians = (angle + sweep) * Math.PI / 180;
                    var start = new Point(center + radius * Math.Cos(startRadians),
                        center + radius * Math.Sin(startRadians));
                    var end = new Point(center + radius * Math.Cos(endRadians),
                        center + radius * Math.Sin(endRadians));

                    var figure = new PathFigure { StartPoint = new Point(center, center), IsClosed = true, IsFilled = true };
                    figure.Segments.Add(new LineSegment(start, true));
                    figure.Segments.Add(new ArcSegment(end, new Size(radius, radius), 0,
                        sweep > 180, SweepDirection.Clockwise, true));
                    geometry = new PathGeometry(new[] { figure });
                }

                result.Add(new PieSlice
                {
                    Name = name,
                    Percentage = percentage,
                    Label = $"{percentage:0.#}%",
                    Fill = new SolidColorBrush(color),
                    Geometry = geometry,
                    LabelX = center + labelRadius * Math.Cos(middle) - 23,
                    LabelY = center + labelRadius * Math.Sin(middle) - 9
                });
                angle += sweep;
            }
            return result;
        }

        public sealed class PieSlice
        {
            public string Name { get; init; } = string.Empty;
            public double Percentage { get; init; }
            public string Label { get; init; } = string.Empty;
            public Brush Fill { get; init; } = Brushes.Transparent;
            public Geometry Geometry { get; init; } = Geometry.Empty;
            public double LabelX { get; init; }
            public double LabelY { get; init; }
            public string Tooltip => $"{Name}: {Percentage:N1}%";
        }

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

                if (IsStockReport)
                {
                    DateTime dan = OdDatuma!.Value.Date;
                    var stock = await db.KnjigaKuhinje.AsNoTracking()
                        .Where(x => x.Datum >= dan && x.Datum < dan.AddDays(1))
                        .OrderBy(x => x.Artikl)
                        .Select(x => new { x.Artikl, x.JedinicaMjere, x.Zaliha })
                        .ToListAsync();

                    SelectedWorker = null;
                    _workerArticles.Clear();
                    Radnici.Clear();

                    if (stock.Count == 0)
                    {
                        SetArticles(Array.Empty<ArticleSummary>());
                        ErrorMessage = $"Knjiga kuhinje nije obračunata za {dan:dd.MM.yyyy}.";
                    }
                    else
                    {
                        SetArticles(stock.Select(x => new ArticleSummary
                        {
                            Naziv = x.Artikl ?? string.Empty,
                            JedinicaMjere = UnitName(x.JedinicaMjere),
                            Kolicina = x.Zaliha ?? 0m
                        }));
                    }
                    return;
                }

                var workers = await db.Radnici.AsNoTracking()
                    .Select(x => new { x.IdRadnika, x.Radnik }).ToListAsync();

                var workerNames = workers.ToDictionary(
                    x => x.IdRadnika.ToString(CultureInfo.InvariantCulture),
                    x => x.Radnik ?? string.Empty);

                var summaries = new Dictionary<string, WorkerSummary>();
                var articles = new List<ArticleSummary>();
                var workerArticles = new Dictionary<string, List<ArticleSummary>>();
                string? previousWorkerId = SelectedWorker?.Id;

                decimal bruto = 0m, reklamirano = 0m, gotovina = 0m;
                decimal kartica = 0m, cek = 0m, virman = 0m;
                decimal sank = 0m, kuhinja = 0m, ostalo = 0m;

                await db.Database.OpenConnectionAsync();
                try
                {
                    DbConnection connection = db.Database.GetDbConnection();

                    await using (var command = CreateCommand(connection, PaymentsSql, od, doExclusive))
                    {
                        await using var reader = await command.ExecuteReaderAsync();
                        while (await reader.ReadAsync())
                        {
                            string workerId = reader.IsDBNull(0) ? string.Empty :
                                Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture) ?? string.Empty;

                            int paymentType = reader.IsDBNull(1) ? -1 :
                                Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture);

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
                                    && !string.IsNullOrWhiteSpace(found) ? found
                                    : string.IsNullOrWhiteSpace(workerId) ? "Nepoznat radnik" : $"Radnik #{workerId}";

                                worker = new WorkerSummary { Id = workerId, Ime = name };
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
                            int type = reader.IsDBNull(0) ? 2 :
                                Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);

                            decimal amount = ReadDecimal(reader, 1);
                            switch (type)
                            {
                                case 0: sank += amount; break;
                                case 1: kuhinja += amount; break;
                                default: ostalo += amount; break;
                            }
                        }
                    }

                    if (IsConsumptionReport)
                    {
                        await using var command = CreateCommand(connection, ConsumptionSql, od, doExclusive);
                        await using var reader = await command.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            articles.Add(new ArticleSummary
                            {
                                Naziv = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                                JedinicaMjere = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                Kolicina = ReadDecimal(reader, 2)
                            });
                        }
                    }
                    else if (IsWorkerReport)
                    {
                        await using var command = CreateCommand(connection, WorkerArticlesSql, od, doExclusive);
                        await using var reader = await command.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            string workerId = reader.IsDBNull(0) ? string.Empty :
                                Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture) ?? string.Empty;

                            int? unit = reader.IsDBNull(2) ? null :
                                Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture);

                            if (!workerArticles.TryGetValue(workerId, out List<ArticleSummary>? items))
                            {
                                items = new List<ArticleSummary>();
                                workerArticles.Add(workerId, items);
                            }

                            items.Add(new ArticleSummary
                            {
                                Naziv = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                JedinicaMjere = UnitName(unit),
                                Cijena = ReadDecimal(reader, 3),
                                Kolicina = ReadDecimal(reader, 4)
                            });
                        }
                    }
                    else if (IsArticleReport)
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

                _workerArticles = workerArticles;
                Radnici.Clear();

                foreach (var worker in summaries.Values.OrderByDescending(x => x.Neto).ThenBy(x => x.Ime))
                {
                    worker.RoundAmounts();
                    Radnici.Add(worker);
                }

                if (IsWorkerReport)
                {
                    SelectedWorker = Radnici.FirstOrDefault(x => x.Id == previousWorkerId)
                        ?? Radnici.FirstOrDefault();
                    ShowWorkerArticles();
                }
                else
                {
                    SelectedWorker = null;
                    SetArticles(articles);
                }
                NotifyTotals();
            }
            catch (Exception ex)
            {
                if (IsStockReport) SetArticles(Array.Empty<ArticleSummary>());
                ErrorMessage = "Greška pri učitavanju Dashboarda: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ShowWorkerArticles()
        {
            SetArticles(SelectedWorker != null && _workerArticles.TryGetValue(SelectedWorker.Id, out var items)
                ? items : Array.Empty<ArticleSummary>());
        }

        private void SetArticles(IEnumerable<ArticleSummary> articles)
        {
            Artikli.Clear();
            foreach (var article in articles.OrderBy(x => x.Naziv).ThenBy(x => x.Cijena))
                Artikli.Add(article);
            _artikliUkupno = Artikli.Sum(x => x.Iznos);
            OnPropertyChanged(nameof(ArtikliUkupno));
            OnPropertyChanged(nameof(NemaArtikala));
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
                SELECT r.BrojRacuna, r.Radnik, r.NacinPlacanja, r.Reklamiran,
                       ROUND(SUM(COALESCE(s.Kolicina, 0) * COALESCE(s.Cijena, 0)), 2) AS Iznos
                FROM tblRacuni r
                INNER JOIN tblRacunStavke s ON s.BrojRacuna = r.BrojRacuna
                WHERE r.Datum >= @od AND r.Datum < @do
                  AND (r.TipRacuna IS NULL OR r.TipRacuna <> 'Storno' COLLATE NOCASE)
                GROUP BY r.BrojRacuna, r.Radnik, r.NacinPlacanja, r.Reklamiran
            )
            SELECT Radnik, NacinPlacanja,
                   COALESCE(SUM(Iznos), 0) AS Bruto,
                   COALESCE(SUM(CASE WHEN Reklamiran = 'DA' COLLATE NOCASE THEN Iznos ELSE 0 END), 0) AS Reklamirano
            FROM ReceiptTotals
            GROUP BY Radnik, NacinPlacanja;";

        private const string CategoriesSql = @"
            SELECT COALESCE(s.VrstaArtikla, 2) AS Vrsta,
                   COALESCE(SUM(
                       CASE WHEN r.Reklamiran = 'DA' COLLATE NOCASE THEN 0
                            ELSE COALESCE(s.Kolicina, 0) * COALESCE(s.Cijena, 0) END
                   ), 0) AS Iznos
            FROM tblRacunStavke s
            INNER JOIN tblRacuni r ON r.BrojRacuna = s.BrojRacuna
            WHERE r.Datum >= @od AND r.Datum < @do
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

        private const string WorkerArticlesSql = @"
            SELECT COALESCE(r.Radnik, ''), COALESCE(s.Artikl, ''), s.JedinicaMjere,
                   COALESCE(s.Cijena, 0), SUM(COALESCE(s.Kolicina, 0))
            FROM tblRacunStavke s
            INNER JOIN tblRacuni r ON r.BrojRacuna = s.BrojRacuna
            WHERE r.Datum >= @od AND r.Datum < @do
              AND (r.TipRacuna IS NULL OR r.TipRacuna <> 'Storno' COLLATE NOCASE)
              AND (r.Reklamiran IS NULL OR r.Reklamiran <> 'DA' COLLATE NOCASE)
            GROUP BY r.Radnik, s.IdArtikla, s.Sifra, s.Artikl, s.JedinicaMjere, s.Cijena;";

        private const string ConsumptionSql = @"
            SELECT n.Repromaterijal, COALESCE(n.JedinicaMjere, ''),
                   SUM(COALESCE(s.Kolicina, 0) * COALESCE(n.Kolicina, 0))
            FROM tblRacunStavke s
            INNER JOIN tblRacuni r ON r.BrojRacuna = s.BrojRacuna
            INNER JOIN tblNormativi n ON n.IdProizvoda = s.IdArtikla
            WHERE r.Datum >= @od AND r.Datum < @do
              AND s.VrstaArtikla = 1
              AND (r.TipRacuna IS NULL OR r.TipRacuna <> 'Storno' COLLATE NOCASE)
              AND (r.Reklamiran IS NULL OR r.Reklamiran <> 'DA' COLLATE NOCASE)
              AND n.Repromaterijal IS NOT NULL AND n.Repromaterijal <> ''
            GROUP BY n.Repromaterijal, n.JedinicaMjere
            ORDER BY n.Repromaterijal, n.JedinicaMjere;";

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
            OnPropertyChanged(nameof(PaymentPie));
            OnPropertyChanged(nameof(CategoryPie));
            OnPropertyChanged(nameof(WorkerPie));
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
            public string Id { get; set; } = string.Empty;
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