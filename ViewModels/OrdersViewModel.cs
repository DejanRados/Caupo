using Caupo.Data;
using Caupo.Models;
using Caupo.Properties;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels
{
    public class OrdersViewModel : INotifyPropertyChanged
    {
        private readonly KasaViewModel _kasaViewModel;
        private ObservableCollection<RacunStavka> _stavkeRacuna = [];
        private ObservableCollection<TblNarudzbe> _narudzbe = [];
        private ObservableCollection<TblNarudzbeStavke> _narudzbeStavke = [];
        private string? _imagePathSaveButton;
        private string? _imagePathDeleteButton;
        private int? _idStola;
        private string? _imeStola;
        private string? _sala;

        public ObservableCollection<RacunStavka> StavkeRacuna
        {
            get => _stavkeRacuna;
            set
            {
                if (ReferenceEquals(_stavkeRacuna, value))
                    return;

                _stavkeRacuna = value ?? [];
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TblNarudzbe> Narudzbe
        {
            get => _narudzbe;
            set
            {
                if (ReferenceEquals(_narudzbe, value))
                    return;

                _narudzbe = value ?? [];
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TblNarudzbeStavke> NarudzbeStavke
        {
            get => _narudzbeStavke;
            set
            {
                if (ReferenceEquals(_narudzbeStavke, value))
                    return;

                _narudzbeStavke = value ?? [];
                OnPropertyChanged();
            }
        }

        public string? ImagePathSaveButton
        {
            get => _imagePathSaveButton;
            private set
            {
                if (_imagePathSaveButton == value)
                    return;

                _imagePathSaveButton = value;
                OnPropertyChanged();
            }
        }

        public string? ImagePathDeleteButton
        {
            get => _imagePathDeleteButton;
            private set
            {
                if (_imagePathDeleteButton == value)
                    return;

                _imagePathDeleteButton = value;
                OnPropertyChanged();
            }
        }

        public int? IdStola
        {
            get => _idStola;
            set
            {
                if (_idStola == value)
                    return;

                _idStola = value;
                OnPropertyChanged();
            }
        }

        public string? ImeStola
        {
            get => _imeStola;
            set
            {
                if (_imeStola == value)
                    return;

                _imeStola = value;
                OnPropertyChanged();
            }
        }

        public string? Sala
        {
            get => _sala;
            set
            {
                if (_sala == value)
                    return;

                _sala = value;
                OnPropertyChanged();
            }
        }

        public sealed class OccupiedTableInfo
        {
            public int IdStola { get; init; }
            public string Sala { get; init; } = string.Empty;
            public string? KonobarId { get; init; }
            public string? Konobar { get; init; }
            public decimal Total { get; init; }
        }

        

        public OrdersViewModel(KasaViewModel? kasaViewModel = null)
        {
            _kasaViewModel = kasaViewModel;
            StavkeRacuna = kasaViewModel != null
                ? new ObservableCollection<RacunStavka>(kasaViewModel.StavkeRacuna)
                : new ObservableCollection<RacunStavka>();

            SetImages();
        }

        public async Task<List<OccupiedTableInfo>> LoadOccupiedTablesAsync()
        {
            await using var db = new AppDbContext();

            var stavke = await db.NarudzbeStavke
                .AsNoTracking()
                .Where(x => x.IdNarudzbe != null && !string.IsNullOrEmpty(x.Sala))
                .ToListAsync();

            if (stavke.Count == 0)
                return [];

            var radnici = await db.Radnici
                .AsNoTracking()
                .ToDictionaryAsync(r => r.IdRadnika, r => r.Radnik);

            return stavke
                .GroupBy(x => new
                {
                    IdStola = x.IdNarudzbe!.Value,
                    Sala = x.Sala!
                })
                .Select(g =>
                {
                    string? konobarId = g.Select(x => x.Konobar).FirstOrDefault(x => !string.IsNullOrEmpty(x));
                    string? konobar = null;

                    if (int.TryParse(konobarId, out int idRadnika))
                        radnici.TryGetValue(idRadnika, out konobar);

                    return new OccupiedTableInfo
                    {
                        IdStola = g.Key.IdStola,
                        Sala = g.Key.Sala,
                        KonobarId = konobarId,
                        Konobar = konobar,
                        Total = g.Sum(x => (x.UnitPrice ?? 0m) * (x.Quantity ?? 0m))
                    };
                })
                .ToList();
        }

        private void SetImages()
        {
            bool tamna = string.Equals(Settings.Default.Tema, "Tamna", StringComparison.OrdinalIgnoreCase);
            string folder = tamna ? "Dark" : "Light";

            ImagePathSaveButton = $"pack://application:,,,/Images/{folder}/save.png";
            ImagePathDeleteButton = $"pack://application:,,,/Images/{folder}/delete.png";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
