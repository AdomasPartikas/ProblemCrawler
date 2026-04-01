using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Core.Enums;

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
                    logger.LogInformation("[ollama] startup attempt {attempt}/{Max}", attempt, OllamaStartupServiceConfiguration.MaxRetries);
                    await OllamaRunner.RunOllama(_activeGpu, logger, configuration, cancellationToken);
                    logger.LogInformation("[ollama] Started successfully");
                    return;
                }
                catch (Exception ex) when (attempt < OllamaStartupServiceConfiguration.MaxRetries)
                {
                    logger.LogWarning(ex, "[ollama] Attempt {Attempt} failed retrying in {Delay}s", attempt, OllamaStartupServiceConfiguration.RetryDelay.TotalSeconds);
                    await Task.Delay(OllamaStartupServiceConfiguration.RetryDelay, cancellationToken);

                }
            }
            logger.LogCritical("[ollama] Failed to start after {Max} attempts.", OllamaStartupServiceConfiguration.MaxRetries);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
           
            await OllamaRunner.StopOllama(_activeGpu, logger, configuration, default);
            
        }
    }
}
