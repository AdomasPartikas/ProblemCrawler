using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Interfaces
{
    public interface IClusterJobRunner
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
