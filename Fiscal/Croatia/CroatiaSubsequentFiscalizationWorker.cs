using Caupo.Data;
using Caupo.Fiscal.Common;
using Caupo.Fiscal.Croatia.Models;
using Caupo.Services;
using Caupo.Views;
using System.Diagnostics;
using System.Windows;
using static Caupo.Data.DatabaseTables;

namespace Caupo.Fiscal.Croatia
{
    public sealed class CroatiaSubsequentFiscalizationWorker : IDisposable
    {
        /// <summary>
        /// Interval check for subsequent fiscalization.
        /// </summary>
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan WarningAge = TimeSpan.FromHours(24);
        private static readonly TimeSpan CriticalWarningAge = TimeSpan.FromHours(36);
        private static readonly TimeSpan CriticalWarningRepeatInterval = TimeSpan.FromHours(2);

  

        private readonly SemaphoreSlim _runLock = new(1, 1);

        private CancellationTokenSource? _cts;
        private Task? _workerTask;

        private bool _warning24Shown;
        private DateTime? _lastCriticalWarningAt;
        private bool _criticalPopupOpen;
        private CroatiaFiscalWarningPopup? _criticalPopup;

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

                CheckFiscalizationWarnings(receipts);

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

                        await CloseCriticalPopupIfResolvedAsync(repository, cancellationToken);

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

        private void CheckFiscalizationWarnings(IReadOnlyList<TblRacuni> receipts)
        {
            DateTime now = DateTime.Now;

            var overdue = receipts
                .Where(x => now - x.Datum >= WarningAge)
                .OrderBy(x => x.Datum)
                .ToList();

            if (overdue.Count == 0)
            {
                _warning24Shown = false;
                _lastCriticalWarningAt = null;
                return;
            }

            TblRacuni oldest = overdue[0];
            TimeSpan age = now - oldest.Datum;

            if (age < CriticalWarningAge)
            {
                if (_warning24Shown)
                    return;

                _warning24Shown = true;

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(
                        $"Postoji {overdue.Count} račun(a) koji još nisu fiskalizovani u CIS-u.\n\n" +
                        $"Najstariji račun: {oldest.BrojRacunaHr}\n" +
                        $"Vrijeme izdavanja: {oldest.Datum:dd.MM.yyyy. HH:mm}\n\n" +
                        "Caupo automatski nastavlja naknadnu fiskalizaciju.\n\n" +
                        "Provjerite internet vezu i fiskalnu konfiguraciju.",
                        "Caupo - naknadna fiskalizacija",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }));

                return;
            }

            if (_criticalPopupOpen)
                return;

            if (_lastCriticalWarningAt.HasValue &&
                now - _lastCriticalWarningAt.Value < CriticalWarningRepeatInterval)
                return;

            _lastCriticalWarningAt = now;

            Application.Current.Dispatcher.BeginInvoke(new Action(async () =>
            {
                await ShowCriticalWarningAsync(oldest, overdue.Count, age);
            }));
        }

        private async Task ShowCriticalWarningAsync(TblRacuni oldest, int count, TimeSpan age)
        {
            if (_criticalPopupOpen)
                return;

            _criticalPopupOpen = true;

            try
            {
                try
                {
                    await AuditLogService.WriteAsync(
                        "HR_NAKDOST_WARNING_SHOWN",
                        $"Nefiskalizovanih računa: {count}. Najstariji račun {oldest.BrojRacunaHr}, starost {(int)age.TotalHours} sati.",
                        brojRacuna: oldest.BrojRacuna);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[HR] Nije moguće zapisati audit za prikaz upozorenja: " + ex);
                }

                string warningText =
                    $"{count} račun(a) još nisu fiskalizovani u CIS-u.\n\n" +
                    $"Najstariji račun: {oldest.BrojRacunaHr}\n" +
                    $"Vrijeme izdavanja: {oldest.Datum:dd.MM.yyyy. HH:mm}\n" +
                    $"Proteklo vrijeme: {(int)age.TotalHours} sati\n\n" +
                    "Caupo i dalje automatski pokušava naknadnu fiskalizaciju.\n\n" +
                    "Za uklanjanje ovog upozorenja potrebna je šifra ovlaštenog radnika.";

                var popup = new CroatiaFiscalWarningPopup(warningText);
                _criticalPopup = popup;

                if (Application.Current.MainWindow?.IsVisible == true)
                    popup.Owner = Application.Current.MainWindow;

                bool? confirmed = popup.ShowDialog();

                if (confirmed == true && popup.ConfirmedWorker != null)
                {
                    TblRadnici worker = popup.ConfirmedWorker;

                    try
                    {
                        await AuditLogService.WriteAsync(
                            "HR_NAKDOST_WARNING_ACK",
                            $"Potvrđeno upozorenje za {count} nefiskalizovanih računa. Najstariji račun {oldest.BrojRacunaHr}.",
                            worker.IdRadnika,
                            worker.Radnik,
                            oldest.BrojRacuna);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("[HR] Nije moguće zapisati audit potvrde upozorenja: " + ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[HR] Greška fiskalnog upozorenja: " + ex);
            }
            finally
            {
                _criticalPopup = null;
                _criticalPopupOpen = false;
            }
        }

        private async Task CloseCriticalPopupIfResolvedAsync(CroatiaReceiptRepository repository, CancellationToken cancellationToken)
        {
            if (!_criticalPopupOpen)
                return;

            var remainingReceipts = await repository.GetNotFiscalizedAsync(cancellationToken);
            DateTime now = DateTime.Now;

            bool criticalProblemStillExists = remainingReceipts.Any(x => now - x.Datum >= CriticalWarningAge);

            if (criticalProblemStillExists)
                return;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_criticalPopup == null || !_criticalPopup.IsVisible)
                    return;

                Debug.WriteLine("[HR] Kritično upozorenje se automatski zatvara jer više nema kritičnih nefiskalizovanih računa.");

                _criticalPopup.CloseAutomatically();
            });
        }

        private static bool ShouldRetry(DateTime? lastAttempt)
        {
            if (!lastAttempt.HasValue)
                return true;

            return DateTime.Now - lastAttempt.Value >= RetryInterval;
        }

        public async Task<CroatiaFiscalizationResponse> FiscalizeManuallyAsync(int localReceiptNumber, CancellationToken cancellationToken = default)
        {
            await _runLock.WaitAsync(cancellationToken);

            try
            {
                Debug.WriteLine($"[HR] Worker pauziran zbog manuelne fiskalizacije računa {localReceiptNumber}.");

                var service = new CroatiaFiscalService();

                return await service.FiscalizeSubsequentlyAsync(localReceiptNumber, cancellationToken);
            }
            finally
            {
                _runLock.Release();

                Debug.WriteLine($"[HR] Worker nastavljen nakon manuelne fiskalizacije računa {localReceiptNumber}.");
            }
        }

        public async Task MarkImpossibleManuallyAsync(int localReceiptNumber, CancellationToken cancellationToken = default)
        {
            await _runLock.WaitAsync(cancellationToken);

            try
            {
                Debug.WriteLine($"[HR] Worker pauziran zbog označavanja računa {localReceiptNumber} kao Impossible.");

                var repository = new CroatiaReceiptRepository();

                await repository.MarkImpossibleAsync(localReceiptNumber, cancellationToken);
            }
            finally
            {
                _runLock.Release();

                Debug.WriteLine($"[HR] Worker nastavljen nakon označavanja računa {localReceiptNumber} kao Impossible.");
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _runLock.Dispose();
        }
    }
}