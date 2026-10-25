using Microsoft.SemanticKernel;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;
using System.ComponentModel;
using System.Text.Json;

namespace SapImpactAnalyzer.Services.AI
{
    public class SAPAnalysisPlugin
    {
        private readonly ISAPService _sAPService;
        
        public SAPAnalysisPlugin(ISAPService sAPService, IDependencyAnalysisService dependencyService)
        {
            _sAPService = sAPService;
        }

        [KernelFunction, Description("Get SAP transport request details")]
        public async Task<string> GetTransportDetailsAsync(
            [Description("Transport Request ID")]string transportId)
        {
            var data = await _sAPService.GetTransportObjectsAsync(transportId);
            return JsonSerializer.Serialize(data);
        }
        [KernelFunction, Description("Analyze dependencies for a given SAP transport request")]
        public async Task<string> AnalyzeDependenciesAsync(
            [Description("Get SAP object dependencies")]string transportId
            )
        {
            var dependencies = await _sAPService.GetObjectDependenciesAsync(transportId);
            return JsonSerializer.Serialize(dependencies);
        }

        [KernelFunction, Description("Calculate risk assessment based on transport and dependency data")]
        public string CalculateRiskAssessment(
            [Description("Number of dependencies")] int dependencyCount,
            [Description("Number of critical components affected")] int criticalCount,
            [Description("Transport object types involved")] string objectTypes)
        {
            // Your existing risk calculation logic
            var riskScore = (dependencyCount * 0.3) + (criticalCount * 0.7);
            var riskLevel = riskScore switch
            {
                < 2 => "Low",
                < 5 => "Medium",
                < 8 => "High",
                _ => "Critical"
            };

            return JsonSerializer.Serialize(new
            {
                RiskLevel = riskLevel,
                RiskScore = riskScore,
                Reasoning = $"Based on {dependencyCount} dependencies and {criticalCount} critical components"
            });
        }
    }
}
