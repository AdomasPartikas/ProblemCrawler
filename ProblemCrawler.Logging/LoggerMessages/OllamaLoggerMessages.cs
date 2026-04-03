using Microsoft.Extensions.Logging;

namespace ProblemCrawler.Logging.LoggerMessages;

public static partial class OllamaLoggerMessages
{
    // Informational and telemetry events.
    [LoggerMessage(EventId = 3000, Level = LogLevel.Information, Message = "Ollama metrics. Tokens: {Tokens}, PromptTokens: {PromptTokens}, GenSec: {GenSec:F2}, PromptSec: {PromptSec:F2}, TotalSec: {TotalSec:F2}, SpeedTokPerSec: {Speed:F2}")]
    public static partial void LogOllamaRequestMetrics(this ILogger logger, int tokens, int promptTokens, double genSec, double promptSec, double totalSec, double speed);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Ollama startup attempt {Attempt}/{MaxAttempts}")]
    public static partial void LogOllamaStartupAttempt(this ILogger logger, int attempt, int maxAttempts);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "Ollama started successfully.")]
    public static partial void LogOllamaStarted(this ILogger logger);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "Ollama shutdown started.")]
    public static partial void LogOllamaShuttingDown(this ILogger logger);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information, Message = "Ollama stopped.")]
    public static partial void LogOllamaStopped(this ILogger logger);

    [LoggerMessage(EventId = 3005, Level = LogLevel.Information, Message = "Ollama model VRAM usage. Model: {Model}, VramMb: {VramMb}, TotalMb: {TotalMb}")]
    public static partial void LogOllamaVramUsage(this ILogger logger, string? model, long vramMb, long totalMb);

    [LoggerMessage(EventId = 3006, Level = LogLevel.Information, Message = "Ollama is running on GPU.")]
    public static partial void LogOllamaGpuActive(this ILogger logger);

    // Warning events.
    [LoggerMessage(EventId = 3100, Level = LogLevel.Warning, Message = "Ollama request failed with status code {StatusCode}.")]
    public static partial void LogOllamaRequestFailed(this ILogger logger, int statusCode);

    [LoggerMessage(EventId = 3101, Level = LogLevel.Warning, Message = "Ollama request attempt {Attempt} failed.")]
    public static partial void LogOllamaRequestAttemptFailed(this ILogger logger, Exception ex, int attempt);

    [LoggerMessage(EventId = 3102, Level = LogLevel.Warning, Message = "Ollama startup attempt {Attempt} failed, retrying in {DelaySeconds}s")]
    public static partial void LogOllamaStartupAttemptFailed(this ILogger logger, Exception ex, int attempt, double delaySeconds);

    [LoggerMessage(EventId = 3103, Level = LogLevel.Warning, Message = "No models reported by Ollama /api/ps.")]
    public static partial void LogOllamaNoModelsReported(this ILogger logger);

    [LoggerMessage(EventId = 3104, Level = LogLevel.Warning, Message = "Ollama is running on CPU.")]
    public static partial void LogOllamaRunningOnCpu(this ILogger logger);

    // Error and critical failures.
    [LoggerMessage(EventId = 3200, Level = LogLevel.Error, Message = "Ollama request failed after {MaxRetries} attempts.")]
    public static partial void LogOllamaRequestFailedAfterRetries(this ILogger logger, int maxRetries);

    [LoggerMessage(EventId = 3201, Level = LogLevel.Error, Message = "Ollama verification failed after startup.")]
    public static partial void LogOllamaVerificationFailed(this ILogger logger, Exception ex);

    [LoggerMessage(EventId = 3202, Level = LogLevel.Error, Message = "Ollama process exited with code {Code}")]
    public static partial void LogOllamaProcessExited(this ILogger logger, int code);

    [LoggerMessage(EventId = 3203, Level = LogLevel.Critical, Message = "Ollama failed to start after {MaxAttempts} attempts.")]
    public static partial void LogOllamaStartupFailed(this ILogger logger, int maxAttempts);
}
