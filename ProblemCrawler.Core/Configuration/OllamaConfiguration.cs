namespace ProblemCrawler.Core.Configuration;

/// <summary>
/// Connection settings for an Ollama instance.
/// </summary>
public sealed class OllamaConfiguration
{
    /// <summary>
    /// Base URL for Ollama HTTP API.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Model name used for generation.
    /// </summary>
    public string Model { get; set; } = "llama3.1:8b";

    /// <summary>
    /// Relative endpoint used for generation requests.
    /// </summary>
    public string GeneratePath { get; set; } = "/api/generate";

    /// <summary>
    /// Delay between retries in milliseconds.
    /// </summary>
    public int RequestDelayMs { get; set; } = 500;

    /// <summary>
    /// Maximum number of retries for transient HTTP failures.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// HTTP timeout in milliseconds.
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Model name used for embedding.
    /// </summary>
    public string EmbedModel { get; set; } = "nomic-embed-text";

    /// <summary>
    /// Relative endpoint used for embedding requests.
    /// </summary>
    public string EmbedPath { get; set; } = "/api/embeddings";
}
