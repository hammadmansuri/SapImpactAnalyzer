using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;

namespace SapImpactAnalyzer.Shared.Abstractions
{
    public class AspNetComputeService : IComputeService
    {
        private readonly ITransportAnalysisService _transportAnalysisService;
        private readonly IUnifiedAIAnalysisService _aiAnalysisService;

        public AspNetComputeService(
            ITransportAnalysisService transportAnalysisService,
            IUnifiedAIAnalysisService aiAnalysisService)
        {
            _transportAnalysisService = transportAnalysisService;
            _aiAnalysisService = aiAnalysisService;
        }

        public async Task<AIAnalysisResponse> ExecuteAnalysisAsync(AIAnalysisRequest request)
        {
            return await _aiAnalysisService.AnalyzeAsync(request);
        }

        public async Task<ImpactAnalysisSummary> AnalyzeTransportRequestsAsync(List<string> transportRequestIds)
        {
            return await _transportAnalysisService.AnalyzeTransportRequestsAsync(transportRequestIds);
        }

        public async Task<TransportAnalysisResult> AnalyzeSingleTransportAsync(string transportId)
        {
            return await _transportAnalysisService.AnalyzeSingleTransportAsync(transportId);
        }
    }
}
