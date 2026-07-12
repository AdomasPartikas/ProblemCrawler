using Microsoft.Extensions.DependencyInjection;
using ProblemCrawler.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Core.Factory
{
    public sealed class RepositoryScope(IServiceScopeFactory scopeFactory)
    {
        public async Task<(ICollectorItemRepository Repository, IAsyncDisposable Scope)> CreateAsync()
        {
            var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<ICollectorItemRepository>();
            return (repo, scope);
        }

        public async Task RunAsync(Func<ICollectorItemRepository, Task> action)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<ICollectorItemRepository>();
            await action(repo);
        }

        public async Task<T> RunAsync<T>(Func<ICollectorItemRepository, Task<T>> action)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<ICollectorItemRepository>();
            return await action(repo);
        }
    }
}
