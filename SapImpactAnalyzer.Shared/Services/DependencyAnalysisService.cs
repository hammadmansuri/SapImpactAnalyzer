using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;

namespace SapImpactAnalyzer.Services
{
    public class DependencyAnalysisService : IDependencyAnalysisService
    {
        private readonly ISAPService _sapService;
        private readonly ILogger<DependencyAnalysisService> _logger;

        public DependencyAnalysisService(ISAPService sapService, ILogger<DependencyAnalysisService> logger)
        {
            _sapService = sapService;
            _logger = logger;
        }

        public async Task<List<DependencyInfo>> AnalyzeDependenciesAsync(List<TransportAnalysisResult> transportAnalysis)
        {
            _logger.LogInformation("Starting dependency analysis for {Count} transports", transportAnalysis.Count);

            // Collect all unique objects from all transports
            var allObjects = transportAnalysis
                .SelectMany(t => t.ObjectsAffected)
                .Select(obj => obj.ObjectName)
                .Distinct()
                .ToList();

            if (!allObjects.Any())
            {
                _logger.LogWarning("No objects found for dependency analysis");
                return new List<DependencyInfo>();
            }

            try
            {
                var dependencies = await _sapService.GetDependenciesAsync(allObjects);

                // Assess risk for each dependency
                foreach (var dependency in dependencies)
                {
                    dependency.RiskLevel = await AssessDependencyRiskAsync(dependency);
                }

                _logger.LogInformation("Dependency analysis completed. Found {Count} objects with dependencies",
                    dependencies.Count(d => d.TotalDependencies > 0));

                return dependencies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze dependencies");
                return new List<DependencyInfo>();
            }
        }

        public async Task<RiskLevel> AssessDependencyRiskAsync(DependencyInfo dependency)
        {
            await Task.Delay(1); // Placeholder for async operation

            var riskScore = 0;

            // High number of dependencies increases risk
            if (dependency.TotalDependencies > 20) riskScore += 3;
            else if (dependency.TotalDependencies > 10) riskScore += 2;
            else if (dependency.TotalDependencies > 5) riskScore += 1;

            // Upstream dependencies are riskier (things that depend on this object)
            var upstreamHighRisk = dependency.UpstreamDependencies.Count(d => d.Impact >= RiskLevel.High);
            riskScore += upstreamHighRisk * 2;

            // Critical relationship types
            var criticalRelations = dependency.DownstreamDependencies.Count(d =>
                d.RelationType == "CALLS" || d.RelationType == "CRITICAL_PATH");
            riskScore += criticalRelations;

            return riskScore switch
            {
                <= 1 => RiskLevel.Low,
                <= 3 => RiskLevel.Medium,
                <= 6 => RiskLevel.High,
                _ => RiskLevel.Critical
            };
        }
    }
}
