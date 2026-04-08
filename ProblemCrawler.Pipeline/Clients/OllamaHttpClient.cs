using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;
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
    private static readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public async Task<float[]?> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            for (var attempt = 0; attempt < _options.MaxRetries; attempt++)
            {
                try
                {
                    var payload = new
                    {
                        model = _options.EmbedModel,
                        prompt = text
                    };

                    var body = JsonSerializer.Serialize(payload, JsonOptions);
                    using var request = new HttpRequestMessage(HttpMethod.Post, _options.EmbedPath)
                    {
                        Content = new StringContent(body, Encoding.UTF8, "application/json")
                    };

                    using var response = await _httpClient.SendAsync(request, cancellationToken);
                    var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var parsed = JsonSerializer.Deserialize<OllamaEmbedResponse>(responseBody, JsonOptions);

                        if (parsed?.Embedding is not null)
                        {
                            _logger.LogInformation(
                                "[ollama] embed dims={Dims}",
                                parsed.Embedding.Length);
                        }

                        return parsed?.Embedding;
                    }

                    if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
                    {
                        await Task.Delay(_options.RequestDelayMs, cancellationToken);
                        continue;
                    }

                    _logger.LogWarning("Ollama embed request failed with status {StatusCode}. Body: {Body}", response.StatusCode, responseBody);
                    return null;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ollama embed attempt {Attempt} failed.", attempt + 1);
                    if (attempt < _options.MaxRetries - 1)
                    {
                        await Task.Delay(_options.RequestDelayMs, cancellationToken);
                    }
                }

            }

            _logger.LogError("Ollama embed request failed after {MaxRetries} attempts.", _options.MaxRetries);
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string?> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
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

                            _logger.LogInformation(
                                "[ollama] tokens={Tokens} promptTokens={PromptTokens} genSec={GenSec:F2} promptSec={PromptSec:F2} totalSec={TotalSec:F2} speed={Speed:F2} tok/s",
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

                    _logger.LogWarning("Ollama request failed with status {StatusCode}. Body: {Body}", response.StatusCode, responseBody);
                    return null;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ollama request attempt {Attempt} failed.", attempt + 1);
                    if (attempt < _options.MaxRetries - 1)
                    {
                        await Task.Delay(_options.RequestDelayMs, cancellationToken);
                    }
                }
            }
            _logger.LogError("Ollama request failed after {MaxRetries} attempts.", _options.MaxRetries);
            return null;
        }
        finally
        {
            _gate.Release();
        }



    }
    private sealed class OllamaEmbedResponse
    {
        public float[]? Embedding { get; set; }
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
