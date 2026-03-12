using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ProblemCrawler.Core.Interfaces;
using ProblemCrawler.Infrastructure.Data;
using ProblemCrawler.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProblemCrawler.Infrastructure
{
    /// <summary>
    /// Provides extension methods for configuring infrastructure services in dependency injection.
    /// </summary>
    public static class ServiceCollection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionString"];
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            services.AddDbContext<ProblemCrawlerDbContext>(options =>
                options.UseNpgsql(dataSource)
            );

            services.AddScoped<ICollectorItemRepository,CollectorItemRepository>();
            return services;
        }
    }
}
