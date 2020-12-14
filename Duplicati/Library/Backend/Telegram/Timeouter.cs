using System;
using System.Threading;
using System.Threading.Tasks;

namespace Duplicati.Library.Backend
{
    public static class Timeouter
    {
        public static async Task<T> TimeoutAsync<T>(Func<CancellationToken, Task<T>> func, int timeoutSeconds = 15, CancellationToken externalCancelToken = default(CancellationToken))
        {
            var timespan = TimeSpan.FromSeconds(timeoutSeconds);
            var tokenSource = new CancellationTokenSource(timespan);
            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancelToken, tokenSource.Token);
            return await func(linkedTokenSource.Token);
        }

        public static async Task TimeoutAsync(Func<CancellationToken, Task> func, int timeoutSeconds = 15, CancellationToken externalCancelToken = default(CancellationToken))
        {
            var timespan = TimeSpan.FromSeconds(timeoutSeconds);
            var tokenSource = new CancellationTokenSource(timespan);
            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalCancelToken, tokenSource.Token);

            await func(linkedTokenSource.Token);
        }
    }
}
