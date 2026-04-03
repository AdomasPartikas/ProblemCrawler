using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
using ProblemCrawler.Logging.LoggerMessages;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProblemCrawler.Pipeline.Clients;

public sealed class OllamaHttpClient(
    HttpClient httpClient,
    IOptions<OllamaConfiguration> options,
    ILogger<OllamaHttpClient> logger)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly OllamaConfiguration _options = options.Value;
    private readonly ILogger<OllamaHttpClient> _logger = logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string?> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < _options.MaxRetries; attempt++)
        {
            try
            {
                var payload = new
                {
                    model = _options.Model,
                    prompt,
                    stream = false,
                    format = "json"
                };

                var body = JsonSerializer.Serialize(payload, JsonOptions);
                using var request = new HttpRequestMessage(HttpMethod.Post, _options.GeneratePath)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var parsed = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseBody, JsonOptions);
                    // Ollama metrics
                    if (parsed is not null)
                    {
                        var genSeconds = parsed.EvalDuration / 1_000_000_000.0;
                        var promptSeconds = parsed.PromptEvalDuration / 1_000_000_000.0;
                        var totalSeconds = parsed.TotalDuration / 1_000_000_000.0;

                        var tokensPerSecond = genSeconds > 0
                            ? parsed.EvalCount / genSeconds
                            : 0;

                        _logger.LogOllamaRequestMetrics(
                            parsed.EvalCount,
                            parsed.PromptEvalCount,
                            genSeconds,
                            promptSeconds,
                            totalSeconds,
                            tokensPerSecond
                        );
                    }

                    return parsed?.Response;
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                {
                    await Task.Delay(_options.RequestDelayMs, cancellationToken);
                    continue;
                }

                _logger.LogOllamaRequestFailed((int)response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogOllamaRequestAttemptFailed(ex, attempt + 1);
                if (attempt < _options.MaxRetries - 1)
                {
                    await Task.Delay(_options.RequestDelayMs, cancellationToken);
                }
            }
        }

        _logger.LogOllamaRequestFailedAfterRetries(_options.MaxRetries);
        return null;
    }

    private sealed class OllamaGenerateResponse
    {
        public string? Response { get; set; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }

        [JsonPropertyName("eval_duration")]
        public long EvalDuration { get; set; }

        [JsonPropertyName("prompt_eval_count")]
        public int PromptEvalCount { get; set; }

        [JsonPropertyName("prompt_eval_duration")]
        public long PromptEvalDuration { get; set; }

        [JsonPropertyName("total_duration")]
        public long TotalDuration { get; set; }
    }
}