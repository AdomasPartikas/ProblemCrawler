using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProblemCrawler.Core.Configuration;
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
        private readonly int _availableSlotTotal;
        private readonly SemaphoreSlim _jobLock = new(1, 1);
        private readonly SemaphoreSlim _availableSlotLock;
        private readonly SemaphoreSlim _largeJobLock = new(1, 1);
        private readonly object _smallJobSync = new();
        private int _activeSmallJobs;
        private TaskCompletionSource _allSmallJobsDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);


        public OllamaJobGate(IConfiguration configuration)
        {
            var runtime = OllamaRuntimeConfiguration.FromConfiguration(configuration);
            _availableSlotTotal = Math.Max(1, runtime.NumParallel);
            _availableSlotLock = new SemaphoreSlim(_availableSlotTotal, _availableSlotTotal);
            _allSmallJobsDrained.SetResult();
        }

        /// <summary>
        /// Acquire an exclusive job-level lock for interacting with Ollama.
        /// This method waits asynchronously until no other job holds the lock and then
        /// returns an <see cref="IAsyncDisposable"/> that will release the lock when disposed.
        /// Use `await using` or call `DisposeAsync()` on the returned value to ensure the lock
        /// is always released (even when exceptions occur).
        /// </summary>
        /// <param name="cancellationToken">Token to cancel waiting for the lock. If cancelled,
        /// the underlying wait will throw <see cref="OperationCanceledException"/> and the lock
        /// will not be acquired.</param>
        /// <returns>An <see cref="IAsyncDisposable"/> which releases the exclusive job lock when disposed.</returns>
        /// <remarks>
        /// This method serializes access to a single "job" slot and is thread-safe. It is intended
        /// to prevent concurrent pipeline jobs from causing Ollama to repeatedly unload and reload
        /// different models. Prefer calling this with `await using var releaser = await gate.AcquireJobAsync(ct);`.
        /// </remarks>
        public async Task<IAsyncDisposable> AcquireJobAsync(CancellationToken cancellationToken)
        {
            await _jobLock.WaitAsync(cancellationToken);
            return new Releaser(_jobLock);
        }
        /// <summary>
        /// Acquire a slot from the available parallel execution capacity for Ollama jobs.
        /// This method waits asynchronously until a slot becomes available (based on the configured
        /// parallel capacity) and then returns an <see cref="IAsyncDisposable"/> that will release the
        /// slot back to the pool when disposed.
        /// Use `await using` or call `DisposeAsync()` on the returned value to ensure the slot
        /// is always released (even when exceptions occur).
        /// </summary>
        /// <param name="cancellationToken">Token to cancel waiting for an available slot. If cancelled,
        /// the underlying wait will throw <see cref="OperationCanceledException"/> and no slot
        /// will be acquired.</param>
        /// <returns>An <see cref="IAsyncDisposable"/> which releases the acquired slot back to the
        /// available pool when disposed.</returns>
        /// <remarks>
        /// This method allows up to <see cref="_availableSlotTotal"/> concurrent accesses (configured
        /// from the Ollama runtime's NumParallel setting). It is thread-safe and intended to enable
        /// controlled parallelism for Ollama operations within the configured capacity limits.
        /// Prefer calling this with `await using var releaser = await gate.AcquireAvailableSlotsAsync(ct);`.
        /// </remarks>
        public async Task<IAsyncDisposable> AcquireAvailableSlotsAsync(CancellationToken cancellationToken)
        {
            await _availableSlotLock.WaitAsync(cancellationToken);
            return new Releaser(_availableSlotLock);
        }
        /// <summary>
        /// Acquire a slot for a synthesis job, allowing multiple synthesis jobs to run concurrently
        /// while coordinating with full-context jobs.
        /// This method ensures that synthesis jobs register themselves with the gate and provides
        /// a mechanism to signal when all synthesis jobs have completed, enabling full-context jobs
        /// to proceed safely.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the acquisition. If the cancellation occurs
        /// before the method returns, this slot will not be acquired.</param>
        /// <returns>An <see cref="IAsyncDisposable"/> which, when disposed, decrements the active synthesis
        /// job count and signals completion when all synthesis jobs have finished.</returns>
        /// <remarks>
        /// This method implements a lightweight acquisition pattern by:
        /// 1. Acquiring and immediately releasing the <see cref="_largeJobLock"/> to ensure no full-context
        ///    job is currently holding it (fail-fast pattern).
        /// 2. Incrementing <see cref="_activeSmallJobs"/> under lock to register this synthesis job.
        /// 3. Returning a disposable that decrements the counter and updates <see cref="_allSmallJobsDrained"/>
        ///    when this synthesis job completes.
        /// 
        /// Multiple synthesis jobs can be active simultaneously. When all active synthesis jobs have been
        /// disposed, the gate signals via <see cref="_allSmallJobsDrained"/>, allowing pending full-context
        /// jobs to proceed. This design enables efficient parallelism for synthesis operations while
        /// maintaining exclusivity for full-context operations.
        /// 
        /// Use with `await using var releaser = await gate.AcquireSynthesisSlotAsync(ct);` to ensure
        /// proper cleanup even if an exception occurs.
        /// </remarks>
        public async Task<IAsyncDisposable> AcquireSynthesisSlotAsync(CancellationToken cancellationToken)
        {
            await _largeJobLock.WaitAsync(cancellationToken);
            _largeJobLock.Release();

            await _availableSlotLock.WaitAsync(cancellationToken);

            lock (_smallJobSync)
            {
                if (_activeSmallJobs == 0)
                    _allSmallJobsDrained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _activeSmallJobs++;
            }

            return new ActionDisposable(() =>
            {
                lock (_smallJobSync)
                {
                    if (--_activeSmallJobs == 0)
                        _allSmallJobsDrained.TrySetResult();
                }
                _availableSlotLock.Release();
            });
        }

        /// <summary>
        /// Acquire an exclusive slot for a full-context job, ensuring all active synthesis jobs
        /// have completed before proceeding.
        /// This method waits asynchronously until the large job lock is available and all currently
        /// active synthesis jobs have finished, then returns an <see cref="IAsyncDisposable"/> that will
        /// release the exclusive lock when disposed.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel waiting for the full-context slot. If cancelled,
        /// the underlying waits will throw <see cref="OperationCanceledException"/> and the lock
        /// will not be acquired.</param>
        /// <returns>An <see cref="IAsyncDisposable"/> which releases the exclusive full-context job lock
        /// when disposed.</returns>
        /// <remarks>
        /// This method enforces mutual exclusion between full-context jobs and coordination with synthesis jobs:
        /// 1. Acquires <see cref="_largeJobLock"/> to ensure only one full-context job runs at a time.
        /// 2. Captures the current <see cref="_allSmallJobsDrained"/> task, which will complete when all
        ///    active synthesis jobs have disposed their slots.
        /// 3. Waits for the drain task to complete, ensuring all synthesis jobs have finished before
        ///    this full-context job proceeds.
        /// 4. If cancellation or an error occurs during the wait, releases <see cref="_largeJobLock"/>
        ///    and propagates the exception to allow the lock to be acquired by other waiting jobs.
        /// 
        /// Full-context jobs have priority in that they will not begin execution until all synthesis jobs
        /// that were active at the time of acquisition have completed, preventing concurrent synthesis and
        /// full-context operations.
        /// 
        /// Use with `await using var releaser = await gate.AcquireFullContextSlotAsync(ct);` to ensure
        /// the lock is always released even if an exception occurs.
        /// </remarks>
        public async Task<IAsyncDisposable> AcquireFullContextSlotAsync(CancellationToken cancellationToken)
        {
            await _largeJobLock.WaitAsync(cancellationToken);

            Task drainTask;
            lock (_smallJobSync)
            {
                drainTask = _allSmallJobsDrained.Task;
            }

            try
            {
                await drainTask.WaitAsync(cancellationToken);
            }
            catch
            {
                _largeJobLock.Release();
                throw;
            }

            return new ActionDisposable(() => _largeJobLock.Release());
        }

        public void StartDiagnosticLogging(ILogger logger, CancellationToken cancellationToken)
        {
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                    var queued = Volatile.Read(ref _activeSmallJobs);
                    var fullCtx = _largeJobLock.CurrentCount == 0;

                    if (queued > 0 || fullCtx)
                        logger.LogInformation("[gate] synthesis  queued={Queued}  full-ctx={FullCtx}",
                            queued,
                            _largeJobLock.CurrentCount == 0 ? "yes" : "no");
                }
            }, cancellationToken);
        }
        private sealed class ActionDisposable(Action action) : IAsyncDisposable
        {
            public ValueTask DisposeAsync() { action(); return ValueTask.CompletedTask; }
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
