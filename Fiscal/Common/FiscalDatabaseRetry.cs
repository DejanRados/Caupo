using System.Diagnostics;

namespace Caupo.Fiscal.Common
{
    public static class FiscalDatabaseRetry
    {
        public static async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
        {
            const int maxAttempts = 3;
            const int delayMilliseconds = 400;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    T result = await action();

                    if (attempt > 1)
                        Debug.WriteLine($"[FISCAL DB] DB upis uspješan iz pokušaja {attempt}/{maxAttempts}.");

                    return result;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FISCAL DB] DB upis nije uspio. Pokušaj {attempt}/{maxAttempts}: {ex}");

                    if (attempt == maxAttempts)
                        throw;

                    await Task.Delay(delayMilliseconds, cancellationToken);
                }
            }

            throw new InvalidOperationException("DB upis nije završen.");
        }

        public static async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            await ExecuteAsync(async () =>
            {
                await operation();
                return true;
            }, cancellationToken);
        }
    }
}