using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;
using SapImpactAnalyzer.Utils;

namespace SapImpactAnalyzer.Services.Transport
{
    public class TransportAnalysisService : ITransportAnalysisService
    {
        private readonly ISAPService _sapService;
        private readonly IDependencyAnalysisService _dependencyService;
        private readonly IUnifiedAIAnalysisService _aiService;
        private readonly ILogger<TransportAnalysisService> _logger;

        public TransportAnalysisService(
            ISAPService sapService,
            IDependencyAnalysisService dependencyService,
            IUnifiedAIAnalysisService aiService,
            ILogger<TransportAnalysisService> logger)
        {
            _sapService = sapService;
            _dependencyService = dependencyService;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<TransportAnalysisResult> AnalyzeSingleTransportAsync(string transportRequestId)
        {
            var sapObjects = await _sapService.GetTransportObjectsAsync(transportRequestId);

            if (!sapObjects.Any())
            {
                return new TransportAnalysisResult
                {
                    TransportNumber = transportRequestId,
                    Status = "NOT_FOUND",
                    CalculatedRisk = RiskLevel.Medium,
                    Recommendations = new List<string> { "Transport request not found or has no objects" }
                };
            }

            var firstObject = sapObjects.First();
            var uniqueObjects = sapObjects
                .GroupBy(obj => new { obj.ObjectName, obj.ObjectType })
                .Select(g => new TransportObjectInfo
                {
                    ObjectName = g.Key.ObjectName,
                    ObjectType = g.Key.ObjectType,
                    ProgramId = g.First().ProgramId,
                    ApplicationArea = g.First().ApplicationArea
                })
                .ToList();

            var analysis = new TransportAnalysisResult
            {
                TransportNumber = transportRequestId,
                Status = firstObject.Status,
                ObjectsAffected = uniqueObjects,
                CreateDate = SAPDateTimeHelper.ParseSAPDate(firstObject.CreateDate),
                CreatedBy = firstObject.User,
                CalculatedRisk = CalculateRisk(uniqueObjects, firstObject.Status),
                BusinessImpacts = GenerateBusinessImpacts(uniqueObjects),
                Recommendations = GenerateRecommendations(uniqueObjects, firstObject.Status)
            };

            return analysis;
        }

        public async Task<ImpactAnalysisSummary> AnalyzeTransportRequestsAsync(List<string> transportRequestIds)
        {
            _logger.LogInformation("Starting comprehensive analysis for {Count} transport requests", transportRequestIds.Count);

            // Step 1: Basic transport analysis
            var analysisResults = new List<TransportAnalysisResult>();
            foreach (var trId in transportRequestIds)
            {
                try
                {
                    var analysis = await AnalyzeSingleTransportAsync(trId);
                    analysisResults.Add(analysis);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to analyze transport {TransportId}", trId);
                    analysisResults.Add(new TransportAnalysisResult
                    {
                        TransportNumber = trId,
                        Status = "ERROR",
                        CalculatedRisk = RiskLevel.Critical,
                        Recommendations = new List<string> { $"Failed to analyze: {ex.Message}" }
                    });
                }
            }

            // Step 2: Dependency analysis
            List<DependencyInfo> dependencies = new();
            try
            {
                dependencies = await _dependencyService.AnalyzeDependenciesAsync(analysisResults);
                _logger.LogInformation("Dependency analysis completed with {Count} dependencies found",
                    dependencies.Sum(d => d.TotalDependencies));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dependency analysis failed");
            }

            // Step 3: AI-powered business analysis
            AIAnalysisResponse aiInsights = null;
            try
            {
                var aiRequest = new AIAnalysisRequest
                {
                    TransportAnalysis = analysisResults,
                    Dependencies = dependencies,
                    BusinessContext = "SAP Production Release"
                };

                aiInsights = await _aiService.AnalyzeAsync(aiRequest);
                _logger.LogInformation("AI analysis completed with confidence score: {Score}", aiInsights.ConfidenceScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI analysis failed");
            }

            // Step 4: Compile comprehensive summary
            return new ImpactAnalysisSummary
            {
                TotalTransports = transportRequestIds.Count,
                TransportAnalysis = analysisResults,
                Dependencies = dependencies,
                AIInsights = aiInsights,
                OverallRisk = CalculateOverallRisk(analysisResults, dependencies),
                CriticalRecommendations = GenerateCriticalRecommendations(analysisResults, dependencies, aiInsights)
            };
        }


        private RiskLevel CalculateRisk(List<TransportObjectInfo> objects, string status)
        {
            // Basic risk calculation logic
            var riskScore = 0;

            // Status-based risk
            riskScore += status switch
            {
                "R" => 1, // Released - lower risk
                "D" => 3, // Modifiable - higher risk
                _ => 2
            };

            // Object type based risk
            foreach (var obj in objects)
            {
                riskScore += obj.ObjectType switch
                {
                    "T" => 3, // Table changes - high risk
                    "F" => 2, // Function modules - medium risk
                    "P" => 2, // Programs - medium risk
                    "W" => 1, // Workbench - lower risk
                    _ => 1
                };

                // Custom objects (Z*) might have different risk
                if (obj.ObjectName.StartsWith("Z"))
                {
                    riskScore += 1; // Custom objects - slightly higher risk
                }
            }

            // Convert score to risk level
            var avgRisk = (double)riskScore / Math.Max(objects.Count, 1);
            return avgRisk switch
            {
                <= 1.5 => RiskLevel.Low,
                <= 2.5 => RiskLevel.Medium,
                <= 3.5 => RiskLevel.High,
                _ => RiskLevel.Critical
            };
        }

        private List<string> GenerateBusinessImpacts(List<TransportObjectInfo> objects)
        {
            var impacts = new List<string>();

            foreach (var obj in objects)
            {
                switch (obj.ObjectType)
                {
                    case "T":
                        impacts.Add($"Database table '{obj.ObjectName}' changes may affect data integrity and reporting");
                        break;
                    case "F":
                        impacts.Add($"Function module '{obj.ObjectName}' changes may affect business processes");
                        break;
                    case "P":
                        impacts.Add($"Program '{obj.ObjectName}' changes may affect automated jobs and interfaces");
                        break;
                    case "W":
                        impacts.Add($"Workbench object '{obj.ObjectName}' changes may affect user interface and workflows");
                        break;
                    default:
                        impacts.Add($"Object '{obj.ObjectName}' changes require impact assessment");
                        break;
                }
            }

            return impacts.Distinct().ToList();
        }

        private List<string> GenerateRecommendations(List<TransportObjectInfo> objects, string status)
        {
            var recommendations = new List<string>();

            if (status == "D")
            {
                recommendations.Add("Transport is still modifiable - ensure all changes are complete before release");
            }

            if (objects.Any(o => o.ObjectType == "T"))
            {
                recommendations.Add("Database table changes detected - perform thorough data backup before deployment");
                recommendations.Add("Coordinate with DBA team for table structure changes");
            }

            if (objects.Any(o => o.ObjectName.StartsWith("Z")))
            {
                recommendations.Add("Custom objects detected - verify custom functionality with business users");
            }

            if (objects.Count > 10)
            {
                recommendations.Add("Large number of objects - consider phased deployment approach");
            }

            recommendations.Add("Perform unit testing in development environment");
            recommendations.Add("Schedule deployment during maintenance window");

            return recommendations;
        }

        private RiskLevel CalculateOverallRisk(List<TransportAnalysisResult> analyses, List<DependencyInfo> dependencies)
        {
            if (!analyses.Any()) return RiskLevel.Low;

            var maxTransportRisk = analyses.Max(a => (int)a.CalculatedRisk);
            var maxDependencyRisk = dependencies.Any() ? dependencies.Max(d => (int)d.RiskLevel) : 0;

            var overallRisk = Math.Max(maxTransportRisk, maxDependencyRisk);

            // Escalate if many high-risk items
            var highRiskCount = analyses.Count(a => a.CalculatedRisk >= RiskLevel.High) +
                               dependencies.Count(d => d.RiskLevel >= RiskLevel.High);

            if (highRiskCount > 3 && overallRisk < (int)RiskLevel.Critical)
                overallRisk = (int)RiskLevel.High;

            return (RiskLevel)overallRisk;
        }

        private List<string> GenerateCriticalRecommendations(List<TransportAnalysisResult> analyses, List<DependencyInfo> dependencies, AIAnalysisResponse aiInsights)
        {
            var criticalRecs = new List<string>();

            var highRiskCount = analyses.Count(a => a.CalculatedRisk >= RiskLevel.High);
            if (highRiskCount > 0)
            {
                criticalRecs.Add($"{highRiskCount} high-risk transports detected - review deployment strategy");
            }

            var complexDependencies = dependencies.Count(d => d.TotalDependencies > 10);
            if (complexDependencies > 0)
            {
                criticalRecs.Add($"{complexDependencies} objects with complex dependencies - coordinate deployment sequence");
            }

            var errorCount = analyses.Count(a => a.Status == "ERROR");
            if (errorCount > 0)
            {
                criticalRecs.Add($"{errorCount} transports could not be analyzed - manual review required");
            }

            if (analyses.Count > 20)
            {
                criticalRecs.Add("Large release detected - consider breaking into smaller deployments");
            }

            // Add AI-generated critical recommendations if available
            if (aiInsights?.KeyRisks?.Any() == true)
            {
                criticalRecs.AddRange(aiInsights.KeyRisks.Take(2)); // Add top 2 AI risks
            }

            return criticalRecs;
        }
    }
}
