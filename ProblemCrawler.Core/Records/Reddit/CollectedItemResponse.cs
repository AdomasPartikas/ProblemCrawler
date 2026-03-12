using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Records.Reddit
{
    public sealed record CollectedItemResponse(
        string Id,
        string ItemType,
        string? Author,
        DateTime CreatedAt,
        string? SourceUrl);
}
