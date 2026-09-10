using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Models;
using Caupo.Services;
using Caupo.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Caupo.Fiscal.Croatia
{
    public sealed class CroatiaFiscalService :
        IFiscalService
    {
        public async Task<FiscalResult> IzdajRacunAsync(FiscalRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Items == null || request.Items.Count == 0)
                return FiscalResult.Failed("Račun nema stavki.");

            var settings = CroatiaFiscalSettings.FromProperties();

            try
            {
                settings.Validate();
            }
            catch (Exception ex)
            {
                return FiscalResult.Failed(ex.Message);
            }

            var repository = new CroatiaReceiptRepository();
            var builder = new CroatiaInvoiceBuilder(settings);
            var client = new CroatiaFiscalClient(settings);
            var printer = new CroatiaReceiptPrinter(settings);

            int localReceiptNumber;

            try
            {
                localReceiptNumber = await repository.GetNextLocalReceiptNumberAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                return FiscalResult.Failed("Nije moguće odrediti broj računa: " + ex.Message);
            }

            CroatiaBuiltInvoice builtInvoice;

            try
            {
                builtInvoice = await builder.BuildAsync(request, localReceiptNumber, cancellationToken);
            }
            catch (Exception ex)
            {
                return FiscalResult.Failed("Greška pri pripremi hrvatskog računa: " + ex.Message);
            }

            CroatiaFiscalizationResponse fiscalization;

            try
            {
                fiscalization = await client.FiscalizeAsync(builtInvoice, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                fiscalization = new CroatiaFiscalizationResponse
                {
                    Fiscalized = false,
                    Zki = builtInvoice.Invoice.ZastKod,
                    ErrorMessage = ex.Message
                };
            }

            // CIS je račun primio i eksplicitno ga odbio.
            // Takav račun NE spremamo, NE printamo i NE šaljemo na NakDost.
            // Kasa mora ostati otvorena kako bi korisnik otklonio razlog greške
            // i ponovo pokušao fiskalizaciju.
            if (fiscalization.CisRejected)
            {
                Debug.WriteLine($"[HR] CIS je odbio račun {builtInvoice.ReceiptNumberHr}: {fiscalization.ErrorMessage}");

                return new FiscalResult
                {
                    Success = false,
                    Fiscalized = false,
                    SavedToDatabase = false,
                    Printed = false,
                    LocalReceiptNumber = localReceiptNumber,
                    ReceiptNumber = builtInvoice.ReceiptNumberHr,
                    FiscalNumber = null,
                    FiscalDateTime = builtInvoice.IssueDateTime,
                    ErrorMessage = fiscalization.ErrorMessage
                };
            }

            bool saved = false;
            bool printed = false;
            string? postError = fiscalization.ErrorMessage;

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
                    printed = await printer.PrintAsync(request, builtInvoice, fiscalization, cancellationToken);

                    if (!printed)
                        postError = AppendError(postError, "Račun nije isprintan.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[HR] Print greška: " + ex);
                    postError = AppendError(postError, "Greška pri printanju: " + ex.Message);
                }
            }

            // Ako CIS trenutno nije dostupan, račun se lokalno sprema kao
            // NotFiscalized i kasnije automatski šalje kroz NakDost.
            //
            // Success znači da je lokalno izdavanje završeno.
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

        public async Task<FiscalResult> StornirajRacunAsync(int originalLocalReceiptNumber, int workerId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (originalLocalReceiptNumber <= 0)
                    return FiscalResult.Failed("Neispravan broj originalnog računa.");

                if (workerId <= 0)
                    return FiscalResult.Failed("Nije moguće odrediti radnika koji radi storno.");

                var settings = CroatiaFiscalSettings.FromProperties();
                settings.Validate();

                var repository = new CroatiaReceiptRepository();
                var builder = new CroatiaInvoiceBuilder(settings);

                var original = await repository.GetForStornoAsync(originalLocalReceiptNumber, cancellationToken);

                await using var db = new AppDbContext();

                var worker = await db.Radnici.AsNoTracking().FirstOrDefaultAsync(x => x.IdRadnika == workerId, cancellationToken);

                if (worker == null)
                    return FiscalResult.Failed($"Radnik ID {workerId} nije pronađen.");

                if (string.IsNullOrWhiteSpace(worker.IB))
                    return FiscalResult.Failed($"Radnik {worker.Radnik} nema upisan OIB.");

                int newLocalReceiptNumber = await repository.GetNextLocalReceiptNumberAsync(cancellationToken);

                CroatiaBuiltInvoice builtInvoice = await builder.BuildStornoAsync(original.Receipt, original.Items, newLocalReceiptNumber, worker.IB, cancellationToken);

                var client = new CroatiaFiscalClient(settings);

                CroatiaFiscalizationResponse fiscalization;

                try
                {
                    fiscalization = await client.FiscalizeAsync(builtInvoice, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[HR STORNO] Fiskalizacija nije uspjela: " + ex);

                    fiscalization = new CroatiaFiscalizationResponse
                    {
                        Fiscalized = false,
                        Zki = builtInvoice.Invoice.ZastKod,
                        ErrorMessage = ex.Message
                    };
                }

                if (fiscalization.CisRejected)
                {
                    Debug.WriteLine($"[HR STORNO] CIS je odbio storno {builtInvoice.ReceiptNumberHr}: {fiscalization.ErrorMessage}");

                    return new FiscalResult
                    {
                        Success = false,
                        Fiscalized = false,
                        SavedToDatabase = false,
                        Printed = false,
                        LocalReceiptNumber = builtInvoice.LocalReceiptNumber,
                        ReceiptNumber = builtInvoice.ReceiptNumberHr,
                        FiscalNumber = null,
                        FiscalDateTime = builtInvoice.IssueDateTime,
                        ErrorMessage = fiscalization.ErrorMessage
                    };
                }

                bool saved = false;

                try
                {
                    await FiscalDatabaseRetry.ExecuteAsync(() => repository.SaveStornoAsync(original.Receipt, original.Items, builtInvoice, fiscalization, workerId, cancellationToken), cancellationToken);

                    saved = true;
                    DatabaseBackupService.StartBackup(Globals.CurrentDbPath);

                    Debug.WriteLine($"[HR STORNO] Storno {builtInvoice.ReceiptNumberHr} uspješno spremljen u bazu.");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[HR STORNO] DB upis nije uspio ni nakon ponovljenih pokušaja: " + ex);

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
                        if (fiscalization.Fiscalized && !string.IsNullOrWhiteSpace(fiscalization.Jir))
                            throw new FiscalDatabaseFatalException($"CIS je fiskalizovao storno {builtInvoice.ReceiptNumberHr} i vratio JIR {fiscalization.Jir}, ali lokalni DB upis nije uspio, a nije uspio ni restore backupa. Potrebna je intervencija podrške.", restoreEx);

                        return FiscalResult.Failed("Storno nije spremljen u lokalnu bazu, a restore backupa nije uspio. " + restoreEx.Message);
                    }

                    if (!restored)
                    {
                        if (fiscalization.Fiscalized && !string.IsNullOrWhiteSpace(fiscalization.Jir))
                            throw new FiscalDatabaseFatalException($"CIS je fiskalizovao storno {builtInvoice.ReceiptNumberHr} i vratio JIR {fiscalization.Jir}, ali lokalni DB upis nije uspio i nije moguće vratiti ispravan backup. Potrebna je intervencija podrške.", ex);

                        return FiscalResult.Failed("Storno nije spremljen u lokalnu bazu i nije moguće vratiti ispravan backup.");
                    }

                    try
                    {
                        await repository.SaveStornoAsync(original.Receipt, original.Items, builtInvoice, fiscalization, workerId, cancellationToken);

                        saved = true;
                        DatabaseBackupService.StartBackup(Globals.CurrentDbPath);

                        Debug.WriteLine($"[HR STORNO] Baza vraćena iz backupa i storno {builtInvoice.ReceiptNumberHr} uspješno lokalno spremljen.");
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception retryEx)
                    {
                        if (fiscalization.Fiscalized && !string.IsNullOrWhiteSpace(fiscalization.Jir))
                            throw new FiscalDatabaseFatalException($"CIS je fiskalizovao storno {builtInvoice.ReceiptNumberHr} i vratio JIR {fiscalization.Jir}, ali storno nije moguće spremiti u lokalnu bazu ni nakon vraćanja ispravnog backupa. Daljnji rad Caupa nije siguran i potrebna je intervencija podrške.", retryEx);

                        return FiscalResult.Failed("Storno nije moguće spremiti u lokalnu bazu ni nakon vraćanja backupa. " + retryEx.Message);
                    }
                }

                bool printed = false;

                try
                {
                    var printRequest = new FiscalRequest
                    {
                        Cashier = new FiscalCashier
                        {
                            Id = worker.IdRadnika,
                            Name = worker.Radnik
                        },
                        PaymentType = (FiscalPaymentType)original.Receipt.NacinPlacanja,
                        TotalAmount = builtInvoice.TotalAmount,
                        Items = original.Items.Select(x => new RacunStavka
                        {
                            Name = x.Artikl,
                            Quantity = -Math.Abs(x.Kolicina ?? 0m),
                            UnitPrice = x.Cijena ?? 0m,
                            PoreskaStopa = x.PoreskaStopa
                        }).ToList()
                    };

                    var printer = new CroatiaReceiptPrinter(settings);

                    printed = await printer.PrintStornoAsync(printRequest, builtInvoice, fiscalization, original.Receipt.BrojRacunaHr!, cancellationToken);

                    Debug.WriteLine($"[HR STORNO PRINT] Storno={builtInvoice.ReceiptNumberHr}, Printed={printed}");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[HR STORNO PRINT] Greška pri štampi storna: " + ex);
                }

                Debug.WriteLine($"[HR STORNO] Original={original.Receipt.BrojRacunaHr}, Storno={builtInvoice.ReceiptNumberHr}, Iznos={builtInvoice.TotalAmount:F2}, Radnik={worker.Radnik}, OIB={worker.IB}, Fiscalized={fiscalization.Fiscalized}, Saved={saved}, Printed={printed}, JIR={fiscalization.Jir}, ZKI={fiscalization.Zki}");

                return new FiscalResult
                {
                    Success = true,
                    Fiscalized = fiscalization.Fiscalized,
                    SavedToDatabase = saved,
                    Printed = printed,
                    LocalReceiptNumber = builtInvoice.LocalReceiptNumber,
                    ReceiptNumber = builtInvoice.ReceiptNumberHr,
                    FiscalNumber = fiscalization.Jir,
                    FiscalDateTime = builtInvoice.IssueDateTime,
                    ErrorMessage = fiscalization.ErrorMessage
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (FiscalDatabaseFatalException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HR STORNO] Greška: " + ex);
                return FiscalResult.Failed(ex.Message);
            }
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
