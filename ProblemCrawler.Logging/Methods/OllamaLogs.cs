using Microsoft.Extensions.Logging;
using ProblemCrawler.Logging.EventIds;

namespace ProblemCrawler.Logging.Methods;

public static class OllamaLogs
{
    public static void LogOllamaRequestMetrics(
        this ILogger logger,
        int tokens,
        int promptTokens,
        double generationSeconds,
        double promptSeconds,
        double totalSeconds,
        double tokensPerSecond) =>
        logger.LogInformation(
            OllamaLogEvents.RequestMetrics,
            "Ollama metrics. tokens={Tokens} promptTokens={PromptTokens} genSec={GenSec:F2} promptSec={PromptSec:F2} totalSec={TotalSec:F2} speed={Speed:F2} tok/s",
            tokens,
            promptTokens,
            generationSeconds,
            promptSeconds,
            totalSeconds,
            tokensPerSecond);

    public static void LogOllamaRequestFailed(this ILogger logger, int statusCode) =>
        logger.LogWarning(OllamaLogEvents.RequestFailed, "Ollama request failed with status code {StatusCode}.", statusCode);

    public static void LogOllamaRequestAttemptFailed(this ILogger logger, Exception ex, int attempt) =>
        logger.LogWarning(OllamaLogEvents.RequestAttemptFailed, ex, "Ollama request attempt {Attempt} failed.", attempt);

    public static void LogOllamaRequestFailedAfterRetries(this ILogger logger, int maxRetries) =>
        logger.LogError(OllamaLogEvents.RequestFailedAfterRetries, "Ollama request failed after {MaxRetries} attempts.", maxRetries);

    public static void LogOllamaStartupAttempt(this ILogger logger, int attempt, int maxAttempts) =>
        logger.LogInformation(OllamaLogEvents.StartupAttempt, "Ollama startup attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);

    public static void LogOllamaStarted(this ILogger logger) =>
        logger.LogInformation(OllamaLogEvents.Started, "Ollama started successfully.");

    public static void LogOllamaStartupAttemptFailed(this ILogger logger, Exception ex, int attempt, double delaySeconds) =>
        logger.LogWarning(OllamaLogEvents.StartupAttemptFailed, ex, "Ollama startup attempt {Attempt} failed, retrying in {DelaySeconds}s", attempt, delaySeconds);

    public static void LogOllamaStartupFailed(this ILogger logger, int maxAttempts) =>
        logger.LogCritical(OllamaLogEvents.StartupFailed, "Ollama failed to start after {MaxAttempts} attempts.", maxAttempts);

    public static void LogOllamaShuttingDown(this ILogger logger) =>
        logger.LogInformation(OllamaLogEvents.ShuttingDown, "Ollama shutdown started.");

    public static void LogOllamaStopped(this ILogger logger) =>
        logger.LogInformation(OllamaLogEvents.Stopped, "Ollama stopped.");

    public static void LogOllamaVerificationFailed(this ILogger logger, Exception ex) =>
        logger.LogError(OllamaLogEvents.VerificationFailed, ex, "Ollama verification failed after startup.");

    public static void LogOllamaNoModelsReported(this ILogger logger) =>
        logger.LogWarning(OllamaLogEvents.NoModelsReported, "No models reported by Ollama /api/ps.");

    public static void LogOllamaVramUsage(this ILogger logger, string? modelName, long vramMb, long totalMb) =>
        logger.LogInformation(OllamaLogEvents.VramUsage, "Ollama model: {Model} | VRAM: {VramMb} MB | Total: {TotalMb} MB", modelName, vramMb, totalMb);

    public static void LogOllamaGpuActive(this ILogger logger) =>
        logger.LogInformation(OllamaLogEvents.GpuActive, "Ollama is running on GPU.");

    public static void LogOllamaRunningOnCpu(this ILogger logger) =>
        logger.LogWarning(OllamaLogEvents.RunningOnCpu, "Ollama is running on CPU.");

    public static void LogOllamaProcessExited(this ILogger logger, int code) =>
        logger.LogError(OllamaLogEvents.ProcessExited, "Ollama process exited with code {Code}", code);
}