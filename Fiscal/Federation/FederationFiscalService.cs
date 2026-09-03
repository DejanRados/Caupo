using Caupo.Fiscal.Common;
using Caupo.Fiscal.Federation.Models;
using Caupo.Services;
using System.Diagnostics;

namespace Caupo.Fiscal.Federation
{
    public sealed class FederationFiscalService :
        IFiscalService
    {
        public async Task<FiscalResult> IzdajRacunAsync(
            FiscalRequest request,
            CancellationToken cancellationToken = default)
        {
            if(request == null)
                throw new ArgumentNullException(
                    nameof(request));

            if(request.Items == null ||
               request.Items.Count == 0)
            {
                return FiscalResult.Failed(
                    "Račun nema stavki.");
            }

            var settings =
                FederationFiscalSettings
                    .FromProperties();

            try
            {
                settings.Validate();
            }
            catch(Exception ex)
            {
                return FiscalResult.Failed(
                    ex.Message);
            }

            var repository =
                new FederationReceiptRepository();

            int localReceiptNumber;

            try
            {
                localReceiptNumber =
                    await repository
                        .GetNextLocalReceiptNumberAsync(
                            cancellationToken);
            }
            catch(Exception ex)
            {
                return FiscalResult.Failed(
                    "Nije moguće odrediti interni broj računa: " +
                    ex.Message);
            }

            FederationBuiltInvoice builtInvoice;

            try
            {
                builtInvoice =
                    new FederationInvoiceBuilder()
                        .Build(
                            request,
                            localReceiptNumber);
            }
            catch(Exception ex)
            {
                return FiscalResult.Failed(
                    "Greška pri pripremi Tring računa: " +
                    ex.Message);
            }

            var client =
                new FederationFiscalClient(
                    settings);

            FederationFiscalResponse fiscalResponse =
                client.Issue(
                    builtInvoice);

            // Ako fiskalni uređaj nije prihvatio račun,
            // ne spremamo ga kao uspješno izdani fiskalni račun.
            if(!fiscalResponse.Fiscalized)
            {
                return new FiscalResult
                {
                    Success = false,
                    Fiscalized = false,
                    SavedToDatabase = false,
                    Printed =
                        fiscalResponse.Printed,

                    LocalReceiptNumber =
                        localReceiptNumber,

                    FiscalDateTime =
                        builtInvoice.IssueDateTime,

                    ErrorMessage =
                        fiscalResponse.ErrorMessage
                };
            }

            bool saved = false;
            string? error =
                fiscalResponse.ErrorMessage;

            try
            {
                await FiscalDatabaseRetry.ExecuteAsync(() => repository.SaveAsync(request, builtInvoice, fiscalResponse, cancellationToken), cancellationToken);
                saved = true;
                DatabaseBackupService.StartBackup(Globals.CurrentDbPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[FEDERACIJA/TRING] DB upis nije uspio ni nakon 3 pokušaja: " + ex);

                bool restored = await DatabaseBackupService.RestoreLatestBackupAsync(Globals.CurrentDbPath, cancellationToken);

                if (restored)
                {
                    try
                    {
                        await repository.SaveAsync(request, builtInvoice, fiscalResponse, cancellationToken);
                        saved = true;
                        DatabaseBackupService.StartBackup(Globals.CurrentDbPath);
                        Debug.WriteLine("[FEDERACIJA/TRING] Baza vraćena iz backupa i račun uspješno lokalno spremljen.");
                    }
                    catch (Exception retryEx)
                    {
                        Debug.WriteLine("[FEDERACIJA/TRING] DB upis nije uspio ni nakon restorea: " + retryEx);
                        error = AppendError(error, "Račun je fiskalizovan, baza je vraćena iz backupa, ali račun nije moguće spremiti u lokalnu bazu: " + retryEx.Message);
                    }
                }
                else
                {
                    error = AppendError(error, "Račun je fiskalizovan, ali nije spremljen u lokalnu bazu. Automatski restore backupa nije uspio.");
                }
            }

            // VAŽNO:
            // ako Tring već vrati broj fiskalnog računa,
            // fiskalizacija je završena. Greška pri DB save-u
            // ne smije uzrokovati ponovno fiskalizovanje.
            return new FiscalResult
            {
                Success = true,
                Fiscalized = true,
                SavedToDatabase = saved,
                Printed =
                    fiscalResponse.Printed,

                LocalReceiptNumber =
                    localReceiptNumber,

                ReceiptNumber =
                    fiscalResponse
                        .FiscalReceiptNumber,

                FiscalNumber =
                    fiscalResponse
                        .FiscalReceiptNumber,

                FiscalDateTime =
                    builtInvoice.IssueDateTime,

                ErrorMessage =
                    error
            };
        }

        private static string AppendError(
            string? current,
            string next)
        {
            if(string.IsNullOrWhiteSpace(current))
                return next;

            return current +
                   Environment.NewLine +
                   next;
        }
    }
}
