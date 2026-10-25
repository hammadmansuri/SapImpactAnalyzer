using SapImpactAnalyzer.Models;

namespace SapImpactAnalyzer.Shared.Abstractions
{
    public interface IComputeService
    {
        Task<AIAnalysisResponse> ExecuteAnalysisAsync(AIAnalysisRequest request);
        Task<ImpactAnalysisSummary> AnalyzeTransportRequestsAsync(List<string> transportRequestIds);
        Task<TransportAnalysisResult> AnalyzeSingleTransportAsync(string transportId);
    }
}
