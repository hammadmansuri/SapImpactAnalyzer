using SapImpactAnalyzer.Models;
using System.Threading.Tasks;

namespace SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces
{
    /// <summary>
    /// Unified interface for all AI-based analysis services.
    /// </summary>
    public interface IUnifiedAIAnalysisService
    {
        Task<AIAnalysisResponse> AnalyzeAsync(AIAnalysisRequest request);
    }
}
