using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Caupo.Data;
using Caupo.Properties;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;



namespace Caupo.ViewModels
{
    public class LoginPageViewModel : ObservableObject
    {
        private string? _radnik;
        private int? _lozinka;
        private DatabaseTables.TblRadnici? _selectedUser;

        public ObservableCollection<DatabaseTables.TblRadnici> Workers { get; set; } = new ObservableCollection<DatabaseTables.TblRadnici>();

        public string? radnik
        {
            get => _radnik;
            set => SetProperty(ref _radnik, value);
        }

        public int? lozinka
        {
            get => _lozinka;
            set => SetProperty(ref _lozinka, value);
        }

        public DatabaseTables.TblRadnici? SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
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

        private string? _imagePathLogo;
        public string? ImagePathLogo
        {
            get { return _imagePathLogo; }
            set
            {
                _imagePathLogo = value;
                OnPropertyChanged(nameof(ImagePathLogo));
            }
        }

        private string? _imagePathLogoSmall;
        public string? ImagePathLogoSmall
        {
            get { return _imagePathLogoSmall; }
            set
            {
                _imagePathLogoSmall = value;
                OnPropertyChanged(nameof(ImagePathLogoSmall));
            }
        }

        public LoginPageViewModel()
        {

            Start();

        
        }

        public async void Start()
        {
            await LoadRadniciAsync();
            await SetImage();
        }


        public async Task SetImage()
        {
            await Task.Delay(1);
            string tema = Settings.Default.Tema;

            if (tema == "Tamna")
            {
                Debug.WriteLine("Aktivna tema koju vidi viewmodel je : " + tema);

                ImagePathLogo= "pack://application:,,,/Images/caupolight.svg";
                ImagePathLogoSmall = "pack://application:,,,/Images/logowhite.svg";
                FontColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(212, 212, 212));   Application.Current.Resources["GlobalFontColor"] =    FontColor;
                BackColor = new SolidColorBrush(Color.FromArgb(0xFF, 0x28, 0x28, 0x28));
            }
            else
            {

                ImagePathLogo = "pack://application:,,,/Images/caupodark.svg";
                ImagePathLogoSmall = "pack://application:,,,/Images/logodark.svg";
                FontColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 50));  Application.Current.Resources["GlobalFontColor"] =    FontColor;
               BackColor = new SolidColorBrush(Color.FromArgb(0xFF, 0xf8, 0xf8, 0xf8));

            }
        }
        private async Task LoadRadniciAsync()
        {

            using (var db = new AppDbContext())
            {
                var radnikExists = await db.Radnici.AnyAsync();
                Debug.WriteLine($"-------------------------------Data Exists in EF Query: {radnikExists}");
            }
            using (var db = new AppDbContext())
            {
                var radnici = await db.Radnici.ToListAsync();
                Debug.WriteLine("-------------------------------------------------" + radnici.Count.ToString());
                Workers.Clear();
                foreach (var radnik in radnici)
                    Workers.Add(radnik);
            }
        }

     

     
    }
}
