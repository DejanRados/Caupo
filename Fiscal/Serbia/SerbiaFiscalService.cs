using Caupo.Fiscal.Common;
using Caupo.Fiscal.Serbia.Helpers;
using Caupo.Services;
using System.Diagnostics;

namespace Caupo.Fiscal.Serbia
{
    public sealed class SerbiaFiscalService : IFiscalService
    {
        public async Task<FiscalResult> IzdajRacunAsync(
            FiscalRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Items.Count == 0)
                return FiscalResult.Failed("Račun nema stavki.");

            SerbiaFiscalSettings settings = SerbiaFiscalSettings.Load();

            try
            {
                settings.Validate();

                var client = new SerbiaFiscalClient(settings);
                var taxMapper = new SerbiaTaxMapper();
                var builder = new SerbiaInvoiceBuilder(taxMapper, settings);
                var repository = new SerbiaReceiptRepository();
                var printer = new SerbiaReceiptPrinter();

                var activeRates = await client.GetCurrentTaxRatesAsync(cancellationToken);

                int localReceiptNumber = await repository.GetNextLocalReceiptNumberAsync(cancellationToken);

                var invoice = await builder.BuildAsync(
                    request,
                    localReceiptNumber,
                    activeRates,
                    cancellationToken);

                string requestId = Guid.NewGuid().ToString("N");

                var response = await client.IssueInvoiceAsync(
                    invoice,
                    requestId,
                    cancellationToken);

                bool isCopy = string.Equals(
                    request.InvoiceType,
                    "Copy",
                    StringComparison.OrdinalIgnoreCase);

                bool isRefund = string.Equals(
                    request.TransactionType,
                    "Refund",
                    StringComparison.OrdinalIgnoreCase);

                bool savedToDatabase = isCopy;
                bool printed = false;
                string? postFiscalError = null;
                int? savedLocalReceiptNumber = null;

                if (isCopy)
                {
                    Debug.WriteLine(
                        $"[SRBIJA COPY] Fiskalni duplikat izdat. Novi lokalni račun se ne kreira. FiscalNumber={response.InvoiceNumber}");
                }
                else
                {
                    try
                    {
                        if (isRefund)
                        {
                            await FiscalDatabaseRetry.ExecuteAsync(
                                async () =>
                                {
                                    await repository.MarkRefundedAsync(
                                        request,
                                        response,
                                        cancellationToken);

                                    return true;
                                },
                                cancellationToken);

                            savedToDatabase = true;

                            Debug.WriteLine(
                                $"[SRBIJA REFUND] Originalni račun označen kao reklamiran. RefundFiscalNumber={response.InvoiceNumber}");
                        }
                        else
                        {
                            savedLocalReceiptNumber =
                                await FiscalDatabaseRetry.ExecuteAsync(
                                    () => repository.SaveAsync(
                                        request,
                                        invoice,
                                        response,
                                        cancellationToken),
                                    cancellationToken);

                            savedToDatabase = true;
                        }

                        DatabaseBackupService.StartBackup(Globals.CurrentDbPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("[SRBIJA] DB upis nije uspio ni nakon 3 pokušaja: " + ex);

                        bool restored =
                            await DatabaseBackupService.RestoreLatestBackupAsync(
                                Globals.CurrentDbPath,
                                cancellationToken);

                        if (restored)
                        {
                            try
                            {
                                if (isRefund)
                                {
                                    await repository.MarkRefundedAsync(
                                        request,
                                        response,
                                        cancellationToken);

                                    savedToDatabase = true;

                                    Debug.WriteLine(
                                        "[SRBIJA REFUND] Baza vraćena iz backupa i Refund uspješno lokalno spremljen.");
                                }
                                else
                                {
                                    savedLocalReceiptNumber =
                                        await repository.SaveAsync(
                                            request,
                                            invoice,
                                            response,
                                            cancellationToken);

                                    savedToDatabase = true;

                                    Debug.WriteLine(
                                        "[SRBIJA] Baza vraćena iz backupa i račun uspješno lokalno spremljen.");
                                }

                                DatabaseBackupService.StartBackup(Globals.CurrentDbPath);
                            }
                            catch (Exception retryEx)
                            {
                                Debug.WriteLine("[SRBIJA] DB upis nije uspio ni nakon restorea: " + retryEx);

                                postFiscalError = isRefund
                                    ? "Refund je fiskalizovan, baza je vraćena iz backupa, ali Refund nije moguće spremiti u lokalnu bazu: " + retryEx.Message
                                    : "Račun je fiskalizovan, baza je vraćena iz backupa, ali račun nije moguće spremiti u lokalnu bazu: " + retryEx.Message;
                            }
                        }
                        else
                        {
                            postFiscalError = isRefund
                                ? "Refund je fiskalizovan, ali promjene nisu sačuvane u lokalnu bazu. Automatski restore backupa nije uspio."
                                : "Račun je fiskalizovan, ali nije sačuvan u lokalnu bazu. Automatski restore backupa nije uspio.";
                        }
                    }
                }

                try
                {
                    printed = await printer.PrintAsync(
                        response,
                        cancellationToken);

                    if (!printed)
                    {
                        postFiscalError = AppendError(
                            postFiscalError,
                            "Račun je fiskalizovan, ali nije odštampan.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[SRBIJA] Print greška nakon fiskalizacije: " + ex);

                    postFiscalError = AppendError(
                        postFiscalError,
                        "Račun je fiskalizovan, ali štampanje nije uspjelo: " + ex.Message);
                }

                return new FiscalResult
                {
                    Success = true,
                    Fiscalized = true,
                    FiscalizationStatus = FiscalizationStatus.Fiscalized,
                    SavedToDatabase = savedToDatabase,
                    Printed = printed,
                    LocalReceiptNumber = savedLocalReceiptNumber ?? localReceiptNumber,
                    ReceiptNumber = response.InvoiceNumber,
                    FiscalNumber = response.InvoiceNumber,
                    FiscalDateTime = response.SdcDateTime?.LocalDateTime,
                    ErrorMessage = postFiscalError,
                    RawResponse = response.RawJson
                };
            }
            catch (FiscalOutcomeUnknownException ex)
            {
                Debug.WriteLine(
                    $"[SRBIJA] NEPOZNAT ISHOD FISKALIZACIJE. RequestId={ex.RequestId}");

                Debug.WriteLine(ex);

                return new FiscalResult
                {
                    Success = false,
                    Fiscalized = false,
                    FiscalizationStatus = FiscalizationStatus.Unknown,
                    SavedToDatabase = false,
                    Printed = false,
                    ErrorMessage = ex.Message,
                    RawResponse = ex.RequestId
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SRBIJA] Fiskalizacija greška: " + ex);

                return FiscalResult.Failed(ex.Message);
            }
        }

        private static string AppendError(string? current, string next)
        {
            if (string.IsNullOrWhiteSpace(current))
                return next;

            return current + Environment.NewLine + next;
        }
    }
}