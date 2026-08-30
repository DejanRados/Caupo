using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Models;
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
                await repository.SaveAsync(
                    request,
                    builtInvoice,
                    fiscalization,
                    cancellationToken);

                saved = true;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(
                    "[HR] DB greška poslije fiskalizacije: " +
                    ex);

                postError =
                    AppendError(
                        postError,
                        "Račun nije spremljen u bazu: " +
                        ex.Message);
            }

            if(saved)
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
                Success = saved,
                Fiscalized =
                    fiscalization.Fiscalized,

                SavedToDatabase =
                    saved,

                Printed =
                    printed,

                LocalReceiptNumber =
                    localReceiptNumber,

                ReceiptNumber =
                    builtInvoice.ReceiptNumberHr,

                FiscalNumber =
                    fiscalization.Jir,

                FiscalDateTime =
                    builtInvoice.IssueDateTime,

                ErrorMessage =
                    postError
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
