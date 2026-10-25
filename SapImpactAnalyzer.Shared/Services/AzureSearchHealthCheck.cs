using Azure.Search.Documents;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SapImpactAnalyzer.Services
{
    public class AzureSearchHealthCheck : IHealthCheck
    {
        private readonly SearchClient _searchClient;

        public AzureSearchHealthCheck(SearchClient searchClient)
        {
            _searchClient = searchClient;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _searchClient.GetDocumentCountAsync(cancellationToken);
                return HealthCheckResult.Healthy($"Azure Search is healthy. Document count: {response.Value}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Azure Search is not responding", ex);
            }
        }
    }
}
