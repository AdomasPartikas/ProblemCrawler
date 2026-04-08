using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Pipeline.Interfaces
{
    public interface IIdeaEmbeddingSchedulerTask
    {
        public Task ExecuteAsync();
    }
}
