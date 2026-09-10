using Caupo.Fiscal.Common;
using Caupo.Fiscal.RS.Models;
using Caupo.Services;
using System.Diagnostics;

namespace Caupo.Fiscal.RS
{
    public sealed class RsFiscalService :
        IFiscalService
    {
        public async Task<FiscalResult> IzdajRacunAsync(FiscalRequest request, CancellationToken cancellationToken = default)
        {
            if(request.Items == null ||
               request.Items.Count == 0)
            {
                return FiscalResult.Failed(
                    "Račun nema stavki.");
            }

            var settings =
                RsFiscalSettings
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
                new RsReceiptRepository();

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

            RsBuiltInvoice builtInvoice;

            try
            {
                builtInvoice =
                    new RsInvoiceBuilder(
                        settings)
                        .Build(
                            request,
                            localReceiptNumber);
            }
            catch(Exception ex)
            {
                return FiscalResult.Failed(
                    "Greška pri pripremi RS fiskalnog računa: " +
                    ex.Message);
            }

            RsFiscalResponse fiscalResponse =
                await new RsFiscalClient(
                    settings)
                    .IssueAsync(
                        builtInvoice,
                        cancellationToken);

            if(!fiscalResponse.Fiscalized)
            {
                return new FiscalResult
                {
                    Success = false,
                    Fiscalized = false,
                    SavedToDatabase = false,
                    Printed = false,

                    LocalReceiptNumber =
                        localReceiptNumber,

                    ErrorMessage =
                        fiscalResponse.ErrorMessage,

                    RawResponse =
                        fiscalResponse.RawResponse
                };
            }

            // OD OVOG TRENUTKA račun je već prihvaćen od LPFR-a.
            // Nijedna kasnija greška ne smije izazvati ponovnu
            // fiskalizaciju istog računa.
            bool saved = false;
            bool printed = false;
            string? error = null;

            try
            {
                if (string.Equals(request.InvoiceType, "Copy", StringComparison.OrdinalIgnoreCase))
                {
                    saved = true;
                }
                else if (request.IsRefund)
                {
                    saved = await FiscalDatabaseRetry.ExecuteAsync(() => repository.MarkRefundedAsync(request.ReferentDocumentNumber ?? string.Empty, fiscalResponse, cancellationToken), cancellationToken);

                    if (saved)
                        DatabaseBackupService.StartBackup(Globals.CurrentDbPath);
                }
                else
                {
                    await FiscalDatabaseRetry.ExecuteAsync(() => repository.SaveAsync(request, builtInvoice, fiscalResponse, cancellationToken), cancellationToken);
                    saved = true;
                    DatabaseBackupService.StartBackup(Globals.CurrentDbPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[RS/DB] DB obrada nije uspjela ni nakon 3 pokušaja: " + ex);

                bool restored = await DatabaseBackupService.RestoreLatestBackupAsync(Globals.CurrentDbPath, cancellationToken);

                if (restored)
                {
                    try
                    {
                        if (request.IsRefund)
                        {
                            saved = await repository.MarkRefundedAsync(request.ReferentDocumentNumber ?? string.Empty, fiscalResponse, cancellationToken);
                        }
                        else
                        {
                            await repository.SaveAsync(request, builtInvoice, fiscalResponse, cancellationToken);
                            saved = true;
                        }

                        if (saved)
                        {
                            DatabaseBackupService.StartBackup(Globals.CurrentDbPath);
                            Debug.WriteLine("[RS/DB] Baza vraćena iz backupa i DB obrada uspješno završena.");
                        }
                        else
                        {
                            error = "Račun je fiskalizovan i baza je vraćena iz backupa, ali lokalna DB obrada nije uspjela.";
                        }
                    }
                    catch (Exception retryEx)
                    {
                        Debug.WriteLine("[RS/DB] DB obrada nije uspjela ni nakon restorea: " + retryEx);
                        error = "Račun je fiskalizovan, baza je vraćena iz backupa, ali lokalna DB obrada nije uspjela: " + retryEx.Message;
                    }
                }
                else
                {
                    error = "Račun je fiskalizovan, ali lokalna DB obrada nije uspjela. Automatski restore backupa nije uspio.";
                }
            }

            try
            {
                printed =
                    new RsReceiptPrinter(
                        settings)
                        .Print(
                            fiscalResponse);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(
                    "[RS/PRINT] Greška nakon fiskalizacije: " +
                    ex);

                error =
                    AppendError(
                        error,
                        "Račun je fiskalizovan, ali ispis nije uspio: " +
                        ex.Message);
            }

            return new FiscalResult
            {
                Success = true,
                Fiscalized = true,
                SavedToDatabase = saved,
                Printed = printed,

                LocalReceiptNumber =
                    localReceiptNumber,

                ReceiptNumber =
                    fiscalResponse.FiscalReceiptNumber,

                FiscalNumber =
                    fiscalResponse.FiscalReceiptNumber,

                FiscalDateTime =
                    fiscalResponse.SdcDateTime
                    ?? builtInvoice.IssueDateTime,

                ErrorMessage =
                    error,

                RawResponse =
                    fiscalResponse.RawResponse
            };
        }

        private static string AppendError(
            string? current,
            string next)
        {
            if(string.IsNullOrWhiteSpace(
                current))
            {
                return next;
            }

            return current +
                   Environment.NewLine +
                   next;
        }
    }
}
