
using Caupo.Data;
using Caupo.Helpers;
using Caupo.Licensing;
using Caupo.Properties;
using Caupo.Views;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static Caupo.Data.DatabaseTables;

namespace Caupo.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private object? _currentPage;

    public object? CurrentPage
    {
        get => _currentPage;
        set
        {
            if (ReferenceEquals(_currentPage, value)) return;
            _currentPage = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel()
    {
        Debug.WriteLine("[MAIN] MainViewModel kreiran.");
        Debug.WriteLine("[SETTINGS] user.config = " +
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath);

        Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
        {
            await StartApplicationAsync();
        }));
    }

    private async Task StartApplicationAsync()
    {
        try
        {
            Debug.WriteLine("[MAIN] Pokrećem inicijalizaciju aplikacije.");

            if (!await CheckDatabaseAsync())
            {
                Debug.WriteLine("[MAIN] Startup prekinut jer baza nije dostupna.");
                return;
            }

            Debug.WriteLine("[LICENSE] Provjera Caupo Licensing 2.0 licence.");

            LicenseState state;
            using (var manager = new Caupo.Licensing.LicenseManager())
                state = await manager.InitializeAsync();

            Debug.WriteLine($"[LICENSE] Status={state.Status}, Message={state.Message}");

            if (state.CanOperate)
            {
                if (state.RequiresWarning)
                    ShowLicenseMessage("Licenca je u dodatnom periodu.\n\n" + state.Message);

                HandleValidLicense();
                return;
            }

            OpenLicenseWizard(state);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MAIN] Neočekivana greška u startupu: {ex}");
            ShowLicenseMessage("Dogodila se greška prilikom pokretanja aplikacije.\n\n" + ex.Message);
        }
    }

    private static async Task<bool> CheckDatabaseAsync()
    {
        try
        {
            Debug.WriteLine("[DB] Provjera veze sa bazom.");

            await using var context = new AppDbContext();

            if (await context.Database.CanConnectAsync())
            {
                Debug.WriteLine("[DB] Veza sa bazom je uspješna.");
                return true;
            }

            ShowLicenseMessage("Ne mogu se spojiti na bazu podataka!\nProvjerite da li je baza dostupna.");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DB] Greška pri povezivanju: {ex}");
            ShowLicenseMessage("Greška pri povezivanju sa bazom:\n\n" + ex.Message);
            return false;
        }
    }

    public void SetColors()
    {
        string tema = Settings.Default.Tema ?? string.Empty;
        bool darkTheme = string.Equals(tema, "Tamna", StringComparison.OrdinalIgnoreCase);

        Color fontColor = darkTheme
            ? Color.FromRgb(244, 244, 244)
            : Color.FromRgb(45, 45, 48);

        Color backgroundColor = darkTheme
            ? Color.FromRgb(45, 45, 48)
            : Color.FromRgb(244, 244, 244);

        Application.Current.Resources["GlobalFontColor"] = new SolidColorBrush(fontColor);
        Application.Current.Resources["GlobalBackgroundColor"] = new SolidColorBrush(backgroundColor);

        Debug.WriteLine($"[THEME] Tema: {tema}");
        Debug.WriteLine($"[THEME] GlobalFontColor = #{fontColor.R:X2}{fontColor.G:X2}{fontColor.B:X2}");
        Debug.WriteLine($"[THEME] GlobalBackgroundColor = #{backgroundColor.R:X2}{backgroundColor.G:X2}{backgroundColor.B:X2}");
    }

    private void HandleValidLicense()
    {
        if (!CompanySettingsComplete())
        {
            Debug.WriteLine("[MAIN] Podaci firme nisu kompletni. Otvaram SettingsPage.");

            Globals.ulogovaniKorisnik = new TblRadnici
            {
                Radnik = "Admin"
            };

            PageNavigator.NavigateWithFade(new SettingsPage());
            return;
        }

        Debug.WriteLine("[MAIN] Otvaram LoginPage.");
        CurrentPage = new LoginPage();
    }

    private static bool CompanySettingsComplete()
    {
        return !string.IsNullOrWhiteSpace(Settings.Default.Firma)
            && !string.IsNullOrWhiteSpace(Settings.Default.Adresa)
            && !string.IsNullOrWhiteSpace(Settings.Default.Mjesto)
            && !string.IsNullOrWhiteSpace(Settings.Default.JIB)
            && !string.IsNullOrWhiteSpace(Settings.Default.PDV)
            && !string.IsNullOrWhiteSpace(Settings.Default.ZR)
            && !string.IsNullOrWhiteSpace(Settings.Default.Email);
    }

    private void OpenLicenseWizard(LicenseState state)
    {
        Debug.WriteLine($"[LICENSE] Otvaram LicenseWizardPage. Status={state.Status}");

        if (state.Status is LicenseOperationalStatus.Invalid or LicenseOperationalStatus.ClockError)
            ShowLicenseMessage(state.Message);

        var page = new LicenseWizardPage();
        page.ActivationSucceeded += LicenseWizard_ActivationSucceeded;
        CurrentPage = page;
    }

    private void LicenseWizard_ActivationSucceeded(object? sender, EventArgs e)
    {
        if (sender is LicenseWizardPage page)
            page.ActivationSucceeded -= LicenseWizard_ActivationSucceeded;

        Debug.WriteLine("[LICENSE] Aktivacija potvrđena. Nastavljam startup.");
        HandleValidLicense();
    }

    private static void ShowLicenseMessage(string message)
    {
        var dialog = new MyMessageBox();
        Window? owner = Application.Current.MainWindow;

        if (owner is not null && owner.IsLoaded)
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        dialog.MessageTitle.Text = "Caupo";
        dialog.MessageText.Text = message;
        dialog.ShowDialog();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
