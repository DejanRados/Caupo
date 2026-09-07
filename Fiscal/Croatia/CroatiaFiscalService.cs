using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Models;
using Caupo.Services;
using System.Diagnostics;

namespace Caupo.Fiscal.Croatia
{
    public sealed class CroatiaFiscalService :
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
                CroatiaFiscalSettings
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
                new CroatiaReceiptRepository();

            var builder =
                new CroatiaInvoiceBuilder(
                    settings);

            var client =
                new CroatiaFiscalClient(
                    settings);

            var printer =
                new CroatiaReceiptPrinter(
                    settings);

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
                    "Nije moguće odrediti broj računa: " +
                    ex.Message);
            }

            CroatiaBuiltInvoice builtInvoice;

            try
            {
                builtInvoice =
                    await builder.BuildAsync(
                        request,
                        localReceiptNumber,
                        cancellationToken);
            }
            catch(Exception ex)
            {
                return FiscalResult.Failed(
                    "Greška pri pripremi hrvatskog računa: " +
                    ex.Message);
            }

            CroatiaFiscalizationResponse fiscalization;

            try
            {
                fiscalization =
                    await client.FiscalizeAsync(
                        builtInvoice,
                        cancellationToken);
            }
            catch(OperationCanceledException)
            {
                throw;
            }
            catch(Exception ex)
            {
                fiscalization =
                    new CroatiaFiscalizationResponse
                    {
                        Fiscalized = false,
                        Zki =
                            builtInvoice.Invoice.ZastKod,
                        ErrorMessage =
                            ex.Message
                    };
            }

            bool saved = false;
            bool printed = false;

            string? postError =
                fiscalization.ErrorMessage;

            try
            {
                await FiscalDatabaseRetry.ExecuteAsync(() => repository.SaveAsync(request, builtInvoice, fiscalization, cancellationToken), cancellationToken);
                saved = true;
                DatabaseBackupService.StartBackup(Globals.CurrentDbPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HR] DB upis nije uspio ni nakon 3 pokušaja: " + ex);

                bool restored = await DatabaseBackupService.RestoreLatestBackupAsync(Globals.CurrentDbPath, cancellationToken);

                if (restored)
                {
                    try
                    {
                        await repository.SaveAsync(request, builtInvoice, fiscalization, cancellationToken);
                        saved = true;
                        DatabaseBackupService.StartBackup(Globals.CurrentDbPath);
                        Debug.WriteLine("[HR] Baza vraćena iz backupa i račun uspješno lokalno spremljen.");
                    }
                    catch (Exception retryEx)
                    {
                        Debug.WriteLine("[HR] DB upis nije uspio ni nakon restorea: " + retryEx);
                        postError = AppendError(postError, "Račun je fiskalizovan, baza je vraćena iz backupa, ali račun nije moguće spremiti u lokalnu bazu: " + retryEx.Message);
                    }
                }
                else
                {
                    postError = AppendError(postError, "Račun je fiskalizovan, ali nije spremljen u lokalnu bazu. Automatski restore backupa nije uspio.");
                }
            }

            if (saved)
            {
                try
                {
                    printed =
                        await printer.PrintAsync(
                            request,
                            builtInvoice,
                            fiscalization,
                            cancellationToken);

                    if(!printed)
                    {
                        postError =
                            AppendError(
                                postError,
                                "Račun nije isprintan.");
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(
                        "[HR] Print greška: " +
                        ex);

                    postError =
                        AppendError(
                            postError,
                            "Greška pri printanju: " +
                            ex.Message);
                }
            }

            // Za Hrvatsku račun se mora sačuvati i kada CIS
            // trenutno nije dostupan, kako bi se kasnije mogao
            // poslati naknadnom dostavom.
            //
            // Success ovdje znači da je lokalno izdavanje završeno.
            // Fiscalized posebno govori da li je CIS već dodijelio JIR.
            return new FiscalResult
            {
                Success = fiscalization.Fiscalized || saved,
                Fiscalized = fiscalization.Fiscalized,
                SavedToDatabase = saved,
                Printed = printed,
                LocalReceiptNumber = localReceiptNumber,
                ReceiptNumber = builtInvoice.ReceiptNumberHr,
                FiscalNumber = fiscalization.Jir,
                FiscalDateTime = builtInvoice.IssueDateTime,
                ErrorMessage = postError
            };
        }

        public async Task<CroatiaFiscalizationResponse> FiscalizeSubsequentlyAsync(int localReceiptNumber, CancellationToken cancellationToken = default)
        {
            var settings = CroatiaFiscalSettings.FromProperties();
            settings.Validate();

            var repository = new CroatiaReceiptRepository();
            var builder = new CroatiaInvoiceBuilder(settings);
            var client = new CroatiaFiscalClient(settings);

            var (receipt, items) = await repository.GetForSubsequentFiscalizationAsync(localReceiptNumber, cancellationToken);

            CroatiaBuiltInvoice builtInvoice = await builder.BuildSubsequentAsync(receipt, items, cancellationToken);

            await repository.MarkFiscalizationAttemptAsync(localReceiptNumber, cancellationToken);

            CroatiaFiscalizationResponse fiscalization;

            try
            {
                fiscalization = await client.FiscalizeSubsequentlyAsync(builtInvoice, receipt.Zki!, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HR] Naknadna fiskalizacija nije uspjela: " + ex);

                return new CroatiaFiscalizationResponse
                {
                    Fiscalized = false,
                    Zki = receipt.Zki,
                    ErrorMessage = ex.Message
                };
            }

            if (!fiscalization.Fiscalized || string.IsNullOrWhiteSpace(fiscalization.Jir))
                return fiscalization;

            try
            {
                await FiscalDatabaseRetry.ExecuteAsync(
                    () => repository.MarkFiscalizedAsync(localReceiptNumber, fiscalization.Jir, receipt.Zki!, cancellationToken),
                    cancellationToken);

                DatabaseBackupService.StartBackup(Globals.CurrentDbPath);

                Debug.WriteLine($"[HR] Račun {receipt.BrojRacunaHr} uspješno naknadno fiskalizovan. JIR: {fiscalization.Jir}");

                return fiscalization;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HR] JIR je primljen, ali DB update nije uspio ni nakon ponovljenih pokušaja: " + ex);

                bool restored;

                try
                {
                    restored = await DatabaseBackupService.RestoreLatestBackupAsync(Globals.CurrentDbPath, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception restoreEx)
                {
                    throw new FiscalDatabaseFatalException(
                        $"CIS je fiskalizovao račun {receipt.BrojRacunaHr} i vratio JIR {fiscalization.Jir}, ali lokalni DB upis nije uspio, a nije uspio ni restore backupa. Potrebna je intervencija podrške.",
                        restoreEx);
                }

                if (!restored)
                {
                    throw new FiscalDatabaseFatalException(
                        $"CIS je fiskalizovao račun {receipt.BrojRacunaHr} i vratio JIR {fiscalization.Jir}, ali lokalni DB upis nije uspio i nije moguće vratiti ispravan backup. Potrebna je intervencija podrške.",
                        ex);
                }

                try
                {
                    await repository.MarkFiscalizedAsync(localReceiptNumber, fiscalization.Jir, receipt.Zki!, cancellationToken);

                    DatabaseBackupService.StartBackup(Globals.CurrentDbPath);

                    Debug.WriteLine($"[HR] Baza vraćena iz backupa i JIR {fiscalization.Jir} uspješno spremljen za račun {receipt.BrojRacunaHr}.");

                    return fiscalization;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception retryEx)
                {
                    throw new FiscalDatabaseFatalException(
                        $"CIS je fiskalizovao račun {receipt.BrojRacunaHr} i vratio JIR {fiscalization.Jir}, ali JIR nije moguće spremiti u lokalnu bazu ni nakon vraćanja ispravnog backupa. Daljnji rad Caupa nije siguran i potrebna je intervencija podrške.",
                        retryEx);
                }
            }
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
