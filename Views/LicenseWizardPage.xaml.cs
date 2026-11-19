using Caupo.Helpers;
using Caupo.Licensing;
using Caupo.Licensing.Models;
using Caupo.ViewModels;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Caupo.Views;

public partial class LicenseWizardPage : UserControl
{
    private readonly Caupo.Licensing.LicenseManager _licenseManager = new();
    private readonly LicenseWizardViewModel _viewModel;
    private string? _activeTransactionId;
    private bool _paymentCompleted;
    private bool _activationStarted;
    private bool _disposed;

    public event EventHandler? ActivationSucceeded;

    public LicenseWizardPage()
    {
        InitializeComponent();
        _viewModel = new LicenseWizardViewModel(_licenseManager);
        DataContext = _viewModel;
        Unloaded += LicenseWizardPage_Unloaded;
    }

    private async void Purchase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.OpenPurchaseAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LICENSING] Katalog: {ex}");
            ShowError("Nije moguće učitati cjenovnik.\n\n" + ex.Message);
        }
    }

    private void Activation_Click(object sender, RoutedEventArgs e) => _viewModel.OpenActivation();

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        string? error = _viewModel.IsPlans ? _viewModel.ValidatePlan()
            : _viewModel.IsCustomer ? _viewModel.ValidateCustomer() : null;

        if (error is not null)
        {
            ShowError(error);
            return;
        }

        _viewModel.Next();
    }

    private void Back_Click(object sender, RoutedEventArgs e) => _viewModel.Back();

    private async void Checkout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CheckoutResponse checkout = await _viewModel.StartCheckoutAsync();
            _activeTransactionId = checkout.TransactionId;
            _paymentCompleted = false;
            _activationStarted = false;
            PaddleWebView.Height = 700;

            await PaddleWebView.EnsureCoreWebView2Async();

            PaddleWebView.CoreWebView2.WebMessageReceived -= Paddle_WebMessageReceived;
            PaddleWebView.CoreWebView2.WebMessageReceived += Paddle_WebMessageReceived;

            string theme = App.CurrentTheme == "Svijetla" ? "light" : "dark";
            string url = "https://caupo.app/checkout/wpf.php?_ptxn="
                + Uri.EscapeDataString(_activeTransactionId) + "#" + theme;

            PaddleWebView.CoreWebView2.Navigate(url);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LICENSING] Checkout: {ex}");
            _activeTransactionId = null;

            if (_viewModel.IsCheckout)
                _viewModel.ReturnToReview();

            ShowError("Nije moguće pokrenuti plaćanje.\n\n" + ex.Message);
        }
    }

    private void Paddle_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (_disposed || !_viewModel.IsCheckout || _activeTransactionId is null)
            return;

        if (!Uri.TryCreate(e.Source, UriKind.Absolute, out Uri? source)
            || source.Scheme != Uri.UriSchemeHttps
            || !string.Equals(source.Host, "caupo.app", StringComparison.OrdinalIgnoreCase)
            || source.AbsolutePath != "/checkout/wpf.php"
            || source.Query != "?_ptxn=" + _activeTransactionId)
            return;

        try
        {
            using JsonDocument document = JsonDocument.Parse(e.WebMessageAsJson);
            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("type", out JsonElement type)
                || !root.TryGetProperty("transaction_id", out JsonElement transaction)
                || transaction.GetString() != _activeTransactionId)
                return;

            switch (type.GetString())
            {
                case "caupo-checkout-ready":
                    SendCustomerToCheckout();
                    break;

                case "caupo-checkout-opened":
                    _viewModel.StatusMessage = string.Empty;
                    break;

                case "caupo-checkout-resize":
                    if (root.TryGetProperty("height", out JsonElement height)
                        && height.ValueKind == JsonValueKind.Number
                        && height.TryGetDouble(out double pixels)
                        && double.IsFinite(pixels)
                        && pixels >= 200 && pixels <= 5000)
                    {
                        PaddleWebView.Height = Math.Ceiling(pixels);
                    }
                    break;

                case "caupo-checkout-completed":
                    _paymentCompleted = true;
                    _viewModel.StatusMessage =
                        "Plaćanje je prihvaćeno. Čeka se potvrda servera i izdavanje licence.";

                    if (!_activationStarted)
                    {
                        _activationStarted = true;
                        _ = CompletePurchaseAsync(_activeTransactionId);
                    }
                    break;

                case "caupo-checkout-closed":
                    if (!_paymentCompleted)
                        _viewModel.StatusMessage =
                            "Plaćanje je zatvoreno. Možete se vratiti na pregled narudžbe.";
                    break;

                case "caupo-checkout-error":
                    _viewModel.StatusMessage =
                        root.TryGetProperty("message", out JsonElement message)
                            ? message.GetString() ?? "Greška pri plaćanju."
                            : "Greška pri plaćanju.";
                    break;
            }
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"[LICENSING] WebView2 poruka: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task CompletePurchaseAsync(string transactionId)
    {
        try
        {
            if (!_viewModel.IsSingleLicensePurchase)
            {
                _viewModel.ShowMultiLicenseDecision();
                return;
            }

            bool activated = await _viewModel.CompleteCheckoutAsync(transactionId);

            if (!activated || _disposed)
                return;

            Debug.WriteLine("[LICENSING] Automatska aktivacija uspješna.");
            ActivationSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LICENSING] Automatska aktivacija: {ex}");
            if (_disposed) return;

            _viewModel.StatusMessage = "Automatska aktivacija nije uspjela: " + ex.Message;
            ShowError("Plaćanje je prihvaćeno, ali aktivacija nije završena.\n\n" + ex.Message);
        }
    }

    private async void MultiLicenseYes_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTransactionId is null || !_viewModel.IsMultiLicenseDecision || _viewModel.Busy) return;

        try
        {
            bool activated = await _viewModel.CompleteCheckoutAsync(_activeTransactionId);
            if (!activated || _disposed) return;

            string remaining = _viewModel.PurchasedQuantity == 2
                ? "LICENCA 2 ostaje slobodna za drugi računar."
                : $"LICENCE 2–{_viewModel.PurchasedQuantity} ostaju slobodne za druge računare.";

            ShowMessage($"LICENCA 1 je uspješno aktivirana na ovom računaru.\n{remaining}");
            ActivationSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LICENSING] Aktivacija LICENCE 1: {ex}");
            if (_disposed) return;

            _viewModel.StatusMessage = "Automatska aktivacija nije uspjela: " + ex.Message;
            ShowError("Plaćanje je prihvaćeno, ali aktivacija nije završena.\n\n" + ex.Message);
        }
    }

    private void MultiLicenseNo_Click(object sender, RoutedEventArgs e) =>
        _viewModel.DeclineMultiLicenseActivation();

    private void SendCustomerToCheckout()
    {
        PaddleCustomer customer = _viewModel.Customer;

        string request = JsonSerializer.Serialize(new
        {
            type = "caupo-start-checkout",
            transaction_id = _activeTransactionId,
            customer = new
            {
                company_name = customer.CompanyName,
                contact_name = customer.ContactName,
                email = customer.Email,
                phone = customer.Phone,
                address = customer.Address,
                city = customer.City,
                postal_code = customer.PostalCode,
                country_code = customer.CountryCode,
                tax_number = customer.TaxNumber
            }
        });

        PaddleWebView.CoreWebView2.PostWebMessageAsJson(request);
    }

    private void CheckoutBack_Click(object sender, RoutedEventArgs e)
    {
        if (_paymentCompleted)
        {
            ShowMessage("Plaćanje je prihvaćeno i čeka se potvrda servera.");
            return;
        }

        _activeTransactionId = null;

        if (PaddleWebView.CoreWebView2 is not null)
            PaddleWebView.CoreWebView2.Navigate("about:blank");

        _viewModel.ReturnToReview();
    }

    private async void ActivateLicense_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_viewModel.ManualActivationNeedsRegion)
            {
                _viewModel.FinishManualActivation();
                ShowMessage("Licenca je uspješno aktivirana.");
                ActivationSucceeded?.Invoke(this, EventArgs.Empty);
                return;
            }

            LicenseState state = await _viewModel.ActivateAsync();

            if (!state.CanOperate)
            {
                ShowError(state.Message);
                return;
            }

            if (_viewModel.ManualActivationNeedsRegion)
                return;

            ShowMessage("Licenca je uspješno aktivirana.");
            ActivationSucceeded?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LICENSING] Aktivacija: {ex}");
            ShowError("Aktivacija nije završena.\n\n" + ex.Message);
        }
    }

    private void ShowMessage(string message) => ShowDialog("Caupo", message);
    private void ShowError(string message) => ShowDialog("Greška", message);

    private void ShowDialog(string title, string message)
    {
        var dialog = new MyMessageBox();
        Window? owner = Window.GetWindow(this);

        if (owner is not null)
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        dialog.MessageTitle.Text = title;
        dialog.MessageText.Text = message;
        dialog.ShowDialog();
    }

    private void LicenseWizardPage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_disposed) return;

        _disposed = true;

        if (PaddleWebView.CoreWebView2 is not null)
            PaddleWebView.CoreWebView2.WebMessageReceived -= Paddle_WebMessageReceived;

        PaddleWebView.Dispose();
        _licenseManager.Dispose();
    }
}