using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Enums;
using ProblemCrawler.Logging.LoggerMessages;

namespace ProblemCrawler.API.Helpers
{
    internal sealed class OllamaStartupService(
        ILogger<OllamaStartupService> logger,
        IConfiguration configuration) : IHostedService
    {
        private GpuVendor _activeGpu;
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _activeGpu = GpuDetection.DetectGpu();

            for (int attempt = 0; attempt < OllamaStartupServiceConfiguration.MaxRetries; attempt++)
            {
                try
                {
                    logger.LogOllamaStartupAttempt(attempt + 1, OllamaStartupServiceConfiguration.MaxRetries);
                    await OllamaRunner.RunOllama(_activeGpu, logger, configuration, cancellationToken);
                    logger.LogOllamaStarted();
                    return;
                }
                catch (Exception ex) when (attempt < OllamaStartupServiceConfiguration.MaxRetries)
                {
                    logger.LogOllamaStartupAttemptFailed(ex, attempt + 1, OllamaStartupServiceConfiguration.RetryDelay.TotalSeconds);
                    await Task.Delay(OllamaStartupServiceConfiguration.RetryDelay, cancellationToken);

                }
            }
            logger.LogOllamaStartupFailed(OllamaStartupServiceConfiguration.MaxRetries);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {

            await OllamaRunner.StopOllama(_activeGpu, logger, configuration, default);

        }
    }
}