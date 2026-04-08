using Microsoft.Extensions.Configuration;
namespace ProblemCrawler.Core.Configuration
{
    public sealed record OllamaWslConfiguration(
    IReadOnlyList<string> Models,
    string OllamaPath,
    string Password,
    string BaseUrl)
    {
        public static OllamaWslConfiguration FromConfiguration(IConfiguration cfg)
        {
            var ollamaPath = cfg["Wsl:OllamaPath"] ?? throw new InvalidOperationException("Wsl:OllamaPath is required");
            var password = cfg["Wsl:Password"] ?? throw new InvalidOperationException("Wsl:Password is required");
            var baseUrl = cfg["LLMAnalysis:Ollama:BaseUrl"] ?? throw new InvalidOperationException("LLMAnalysis:Ollama:BaseUrl is required");

            var models = OllamaSetupConfiguration.FromConfiguration(cfg).Models;

            return new OllamaWslConfiguration(
                Models: models,
                OllamaPath: ollamaPath,
                Password: password,
                BaseUrl: baseUrl);
        }
    }
}
