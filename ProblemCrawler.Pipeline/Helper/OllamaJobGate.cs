using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Pipeline.Helper
{
    /// <summary>
    /// Ensures that only one Ollama job runs at a time across all pipeline services.
    /// This prevents model switching overhead when multiple scheduled jobs (analysis,
    /// synthesis, embedding) attempt to use Ollama concurrently, which would cause
    /// Ollama to repeatedly unload and reload different models between requests.
    /// </summary>
    public sealed class OllamaJobGate
    {
        private static readonly SemaphoreSlim _gate = new(1, 1);

        public async Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);
            return new Releaser(_gate);
        }

        private sealed class Releaser(SemaphoreSlim semaphore) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                semaphore.Release();
                return ValueTask.CompletedTask;
            }
        }
    }
}
