using Caupo.Fiscal.Common;
using Caupo.Fiscal.Serbia.Helpers;
using System.Diagnostics;

namespace Caupo.Fiscal.Serbia
{
    public sealed class SerbiaFiscalService :
        IFiscalService
    {
        public async Task<FiscalResult> IzdajRacunAsync(
            FiscalRequest request,
            CancellationToken cancellationToken = default)
        {
            if(request == null)
                throw new ArgumentNullException(nameof(request));

            if(request.Items.Count == 0)
            {
                return FiscalResult.Failed(
                    "Račun nema stavki.");
            }

            SerbiaFiscalSettings settings =
                SerbiaFiscalSettings.Load();

            try
            {
                settings.Validate();

                var client =
                    new SerbiaFiscalClient(
                        settings);

                var taxMapper =
                    new SerbiaTaxMapper();

                var builder =
                    new SerbiaInvoiceBuilder(
                        taxMapper);

                var repository =
                    new SerbiaReceiptRepository();

                var printer =
                    new SerbiaReceiptPrinter();

                // Uzimamo aktivne oznake direktno od PFR-a.
                // Tako Srbija modul ne zavisi od hardkodiranih
                // A/B/C/... oznaka.
                var activeRates =
                    await client.GetCurrentTaxRatesAsync(
                        cancellationToken);

                int localReceiptNumber =
                    await repository
                        .GetNextLocalReceiptNumberAsync(
                            cancellationToken);

                var invoice =
                    await builder.BuildAsync(
                        request,
                        localReceiptNumber,
                        activeRates,
                        cancellationToken);

                var response =
                    await client.IssueInvoiceAsync(
                        invoice,
                        cancellationToken);

                // Od ovog trenutka račun JE fiskalizovan.
                // Greška baze ili printera više nikada ne smije
                // uzrokovati automatsko ponovno slanje istog računa.
                bool savedToDatabase =
                    false;

                bool printed =
                    false;

                string? postFiscalError =
                    null;

                int? savedLocalReceiptNumber =
                    null;

                try
                {
                    savedLocalReceiptNumber =
                        await repository.SaveAsync(
                            request,
                            response,
                            cancellationToken);

                    savedToDatabase =
                        true;
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(
                        "[SRBIJA] DB greška nakon fiskalizacije: " +
                        ex);

                    postFiscalError =
                        "Račun je fiskalizovan, ali nije " +
                        "sačuvan u lokalnu bazu: " +
                        ex.Message;
                }

                try
                {
                    printed =
                        await printer.PrintAsync(
                            response,
                            cancellationToken);

                    if(!printed)
                    {
                        postFiscalError =
                            AppendError(
                                postFiscalError,
                                "Račun je fiskalizovan, ali " +
                                "nije odštampan.");
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(
                        "[SRBIJA] Print greška nakon fiskalizacije: " +
                        ex);

                    postFiscalError =
                        AppendError(
                            postFiscalError,
                            "Račun je fiskalizovan, ali " +
                            "štampanje nije uspjelo: " +
                            ex.Message);
                }

                return new FiscalResult
                {
                    // Success ovdje znači:
                    // PFR je prihvatio račun.
                    // DB i print imaju zasebna stanja.
                    Success =
                        true,

                    Fiscalized =
                        true,

                    SavedToDatabase =
                        savedToDatabase,

                    Printed =
                        printed,

                    LocalReceiptNumber =
                        savedLocalReceiptNumber
                        ?? localReceiptNumber,

                    ReceiptNumber =
                        response.InvoiceNumber,

                    FiscalNumber =
                        response.InvoiceNumber,

                    FiscalDateTime =
                        response.SdcDateTime?
                            .LocalDateTime,

                    ErrorMessage =
                        postFiscalError,

                    RawResponse =
                        response.RawJson
                };
            }
            catch(OperationCanceledException)
            {
                throw;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(
                    "[SRBIJA] Fiskalizacija greška: " +
                    ex);

                return FiscalResult.Failed(
                    ex.Message);
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
