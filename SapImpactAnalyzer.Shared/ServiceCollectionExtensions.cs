using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.Services.AI;
using SapImpactAnalyzer.Services.Search;
using SapImpactAnalyzer.Services.Transport;
using SapImpactAnalyzer.Services;
using SapImpactAnalyzer.Shared.Abstractions;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;

namespace SapImpactAnalyzer.Shared
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSharedServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register all service interfaces with their implementations
            services.AddScoped<ISAPService, SAPService>();
            services.AddScoped<ITransportAnalysisService, TransportAnalysisService>();
            services.AddScoped<IDependencyAnalysisService, DependencyAnalysisService>();
            services.AddScoped<IUnifiedAIAnalysisService, AzureOpenAIService>();
            services.AddScoped<ISearchIndexManager, AzureSearchIndexManager>();
            services.AddScoped<IVectorSearchService, AzureSearchVectorService>();
            services.AddScoped<KnowledgeIngestionService>();
            services.AddScoped<SAPAnalysisPlugin>();

            // Register health checks
            services.AddScoped<AzureSearchHealthCheck>();

            // Register abstractions
            services.AddScoped<IComputeService, AspNetComputeService>();

            // Configuration
            services.Configure<AzureOpenAIConfig>(configuration.GetSection("AzureOpenAI"));
            services.Configure<AzureSearchConfig>(configuration.GetSection("AzureSearch"));

            return services;
        }
    }
}
