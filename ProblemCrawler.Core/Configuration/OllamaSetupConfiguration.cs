using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Configuration
{
    public sealed record OllamaSetupConfiguration(IReadOnlyList<string> Models)
    {
        public static OllamaSetupConfiguration FromConfiguration(IConfiguration cfg)
        {
            var modelsRaw = cfg["LLMAnalysis:OllamaSetup:Models"]
                ?? throw new InvalidOperationException("LLMAnalysis:OllamaSetup:Models is required");

            return new OllamaSetupConfiguration(
                Models: modelsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            );
        }
    }
}
