using SapImpactAnalyzer.Models;

namespace SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces
{
    public interface IDependencyAnalysisService
    {
        Task<List<DependencyInfo>> AnalyzeDependenciesAsync(List<TransportAnalysisResult> transportAnalysis);
        Task<RiskLevel> AssessDependencyRiskAsync(DependencyInfo dependency);
    }
}
