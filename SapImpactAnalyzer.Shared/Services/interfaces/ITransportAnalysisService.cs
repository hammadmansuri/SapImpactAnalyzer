using SapImpactAnalyzer.Models;

namespace SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces
{
    public interface ITransportAnalysisService
    {
        Task<ImpactAnalysisSummary> AnalyzeTransportRequestsAsync(List<string> transportRequestIds);
        Task<TransportAnalysisResult> AnalyzeSingleTransportAsync(string transportRequestId);
    }
}
