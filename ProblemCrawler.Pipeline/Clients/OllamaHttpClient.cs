using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProblemCrawler.Core.Configuration;

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

    private sealed class OllamaGenerateResponse
    {
        public string? Response { get; set; }
    }
}
