using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Models;
using System.Diagnostics;
using System.Windows;

namespace Caupo.Fiscal.Croatia
{
    public sealed class CroatiaSubsequentFiscalizationWorker : IDisposable
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(10);

        private readonly SemaphoreSlim _runLock = new(1, 1);
        private CancellationTokenSource? _cts;
        private Task? _workerTask;

        public bool IsRunning => _workerTask != null && !_workerTask.IsCompleted;

        public void Start()
        {
            if (IsRunning)
                return;

            _cts = new CancellationTokenSource();
            _workerTask = Task.Run(() => RunAsync(_cts.Token));

            Debug.WriteLine("[HR] Naknadna fiskalizacija worker pokrenut.");
        }

        public async Task StopAsync()
        {
            if (_cts == null)
                return;

            _cts.Cancel();

            if (_workerTask != null)
            {
                try
                {
                    await _workerTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            _cts.Dispose();
            _cts = null;
            _workerTask = null;

            Debug.WriteLine("[HR] Naknadna fiskalizacija worker zaustavljen.");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            await ProcessAsync(cancellationToken);

            using var timer = new PeriodicTimer(CheckInterval);

            while (await timer.WaitForNextTickAsync(cancellationToken))
                await ProcessAsync(cancellationToken);
        }

        private async Task ProcessAsync(CancellationToken cancellationToken)
        {
            if (!await _runLock.WaitAsync(0, cancellationToken))
                return;

            try
            {
                if (!string.Equals(Properties.Settings.Default.Country, "Hrvatska", StringComparison.OrdinalIgnoreCase))
                    return;

                var repository = new CroatiaReceiptRepository();
                var service = new CroatiaFiscalService();

                var receipts = await repository.GetNotFiscalizedAsync(cancellationToken);

                foreach (var receipt in receipts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!ShouldRetry(receipt.DatumFiskalnogDokumenta))
                        continue;

                    Debug.WriteLine($"[HR] Pokušaj naknadne fiskalizacije računa {receipt.BrojRacunaHr}.");

                    CroatiaFiscalizationResponse result;

                    try
                    {
                        result = await service.FiscalizeSubsequentlyAsync(receipt.BrojRacuna, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (FiscalDatabaseFatalException ex)
                    {
                        Debug.WriteLine("[HR] KRITIČNA DB GREŠKA: " + ex);

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show(
                                ex.Message,
                                "Caupo - kritična greška baze",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                            Application.Current.Shutdown();
                        });

                        return;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[HR] Greška naknadne fiskalizacije računa {receipt.BrojRacunaHr}: {ex}");
                        continue;
                    }

                    if (result.Fiscalized && !string.IsNullOrWhiteSpace(result.Jir))
                    {
                        Debug.WriteLine($"[HR] Račun {receipt.BrojRacunaHr} naknadno fiskalizovan. JIR: {result.Jir}");
                        continue;
                    }

                    Debug.WriteLine($"[HR] Račun {receipt.BrojRacunaHr} još nije fiskalizovan. {result.ErrorMessage}");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HR] Worker greška: " + ex);
            }
            finally
            {
                _runLock.Release();
            }
        }

        private static bool ShouldRetry(DateTime? lastAttempt)
        {
            if (!lastAttempt.HasValue)
                return true;

            return DateTime.Now - lastAttempt.Value >= RetryInterval;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _runLock.Dispose();
        }
    }
}