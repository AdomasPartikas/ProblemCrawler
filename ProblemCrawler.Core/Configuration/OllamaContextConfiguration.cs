using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Configuration
{
    public sealed record OllamaContextConfiguration
    (
        int AnalysisContextSize,
        int SynthesisContextSize,
        int FullContextSize
    );
}
