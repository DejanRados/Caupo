using Caupo.Fiscal.Common;
using Caupo.Fiscal.Federation.Models;
using Caupo.Services;
using System.Diagnostics;

namespace Caupo.Fiscal.Federation
{
    public sealed class FederationFiscalService : IFiscalService
    {
        public async Task<FiscalResult> IzdajRacunAsync(FiscalRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Items == null || request.Items.Count == 0)
                return FiscalResult.Failed("Račun nema stavki.");

            bool isRefund = string.Equals(request.TransactionType, "Refund", StringComparison.OrdinalIgnoreCase);
            bool isSale = string.IsNullOrWhiteSpace(request.TransactionType) || string.Equals(request.TransactionType, "Sale", StringComparison.OrdinalIgnoreCase);

            if (!isSale && !isRefund)
                return FiscalResult.Failed($"Nepodržana Tring transakcija: {request.TransactionType}.");

            if (isRefund && string.IsNullOrWhiteSpace(request.ReferentDocumentNumber))
                return FiscalResult.Failed("Za reklamaciju nije zadan broj originalnog fiskalnog računa.");

            FederationFiscalSettings settings = FederationFiscalSettings.FromProperties();

            try
            {
                settings.Validate();
            }
            catch (Exception ex)
            {
                return FiscalResult.Failed(ex.Message);
            }

            var repository = new FederationReceiptRepository();
            int localReceiptNumber;

            try
            {
                if (isRefund)
                    localReceiptNumber = await repository.GetLocalReceiptNumberByFiscalNumberAsync(request.ReferentDocumentNumber!, cancellationToken);
                else
                    localReceiptNumber = await repository.GetNextLocalReceiptNumberAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return FiscalResult.Failed(isRefund
                    ? "Nije moguće pronaći originalni račun: " + ex.Message
                    : "Nije moguće odrediti interni broj računa: " + ex.Message);
            }

            FederationBuiltInvoice builtInvoice;

            try
            {
                var builder = new FederationInvoiceBuilder();

                builtInvoice = isRefund
                    ? builder.BuildRefund(request, localReceiptNumber)
                    : builder.Build(request, localReceiptNumber);
            }
            catch (Exception ex)
            {
                return FiscalResult.Failed(isRefund
                    ? "Greška pri pripremi Tring reklamacije: " + ex.Message
                    : "Greška pri pripremi Tring računa: " + ex.Message);
            }

            var client = new FederationFiscalClient(settings);

            FederationFiscalResponse fiscalResponse = isRefund
                ? client.IssueRefund(builtInvoice)
                : client.Issue(builtInvoice);

            if (!fiscalResponse.Fiscalized)
            {
                return new FiscalResult
                {
                    Success = false,
                    Fiscalized = false,
                    SavedToDatabase = false,
                    Printed = fiscalResponse.Printed,
                    LocalReceiptNumber = localReceiptNumber,
                    FiscalDateTime = builtInvoice.IssueDateTime,
                    ErrorMessage = fiscalResponse.ErrorMessage
                };
            }

            bool saved = false;
            string? error = fiscalResponse.ErrorMessage;

            try
            {
                if (isRefund)
                    await FiscalDatabaseRetry.ExecuteAsync(() => repository.MarkRefundedAsync(request, fiscalResponse, cancellationToken), cancellationToken);
                else
                    await FiscalDatabaseRetry.ExecuteAsync(() => repository.SaveAsync(request, builtInvoice, fiscalResponse, cancellationToken), cancellationToken);

                saved = true;
                DatabaseBackupService.StartBackup(Globals.CurrentDbPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(isRefund
                    ? "[FEDERACIJA/TRING REFUND] DB upis nije uspio ni nakon 3 pokušaja: " + ex
                    : "[FEDERACIJA/TRING] DB upis nije uspio ni nakon 3 pokušaja: " + ex);

                bool restored = await DatabaseBackupService.RestoreLatestBackupAsync(Globals.CurrentDbPath, cancellationToken);

                if (restored)
                {
                    try
                    {
                        if (isRefund)
                            await repository.MarkRefundedAsync(request, fiscalResponse, cancellationToken);
                        else
                            await repository.SaveAsync(request, builtInvoice, fiscalResponse, cancellationToken);

                        saved = true;
                        DatabaseBackupService.StartBackup(Globals.CurrentDbPath);

                        Debug.WriteLine(isRefund
                            ? "[FEDERACIJA/TRING REFUND] Baza vraćena iz backupa i reklamacija uspješno lokalno spremljena."
                            : "[FEDERACIJA/TRING] Baza vraćena iz backupa i račun uspješno lokalno spremljen.");
                    }
                    catch (Exception retryEx)
                    {
                        Debug.WriteLine(isRefund
                            ? "[FEDERACIJA/TRING REFUND] DB upis nije uspio ni nakon restorea: " + retryEx
                            : "[FEDERACIJA/TRING] DB upis nije uspio ni nakon restorea: " + retryEx);

                        error = AppendError(error, isRefund
                            ? "Reklamacija je fiskalizovana, baza je vraćena iz backupa, ali originalni račun nije moguće označiti kao reklamiran: " + retryEx.Message
                            : "Račun je fiskalizovan, baza je vraćena iz backupa, ali račun nije moguće spremiti u lokalnu bazu: " + retryEx.Message);
                    }
                }
                else
                {
                    error = AppendError(error, isRefund
                        ? "Reklamacija je fiskalizovana, ali originalni račun nije označen kao reklamiran. Automatski restore backupa nije uspio."
                        : "Račun je fiskalizovan, ali nije spremljen u lokalnu bazu. Automatski restore backupa nije uspio.");
                }
            }

            // VAŽNO:
            // Tring fiskalizacija/reklamacija se izvršava samo jednom.
            // Nakon uspješnog odgovora svi retry pokušaji odnose se isključivo na lokalnu bazu.
            return new FiscalResult
            {
                Success = true,
                Fiscalized = true,
                SavedToDatabase = saved,
                Printed = fiscalResponse.Printed,
                LocalReceiptNumber = localReceiptNumber,
                ReceiptNumber = fiscalResponse.FiscalReceiptNumber,
                FiscalNumber = fiscalResponse.FiscalReceiptNumber,
                FiscalDateTime = builtInvoice.IssueDateTime,
                ErrorMessage = error
            };
        }

        private static string AppendError(string? current, string next)
        {
            if (string.IsNullOrWhiteSpace(current))
                return next;

            return current + Environment.NewLine + next;
        }
    }
}