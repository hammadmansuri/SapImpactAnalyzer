using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;
using SapImpactAnalyzer.Services.AI;
using SapImpactAnalyzer.Utils;
using System.Text.Json;

namespace SapImpactAnalyzer.Services.Transport
{
    public class EnhancedTransportAnalysisService : IUnifiedAIAnalysisService
    {
        private readonly Kernel _kernel;
        private readonly IVectorSearchService _vectorSearchService;
        private readonly ILogger<EnhancedTransportAnalysisService> _logger;

        public EnhancedTransportAnalysisService(
            Kernel kernel,
            SAPAnalysisPlugin sAPAnalysisPlugin,
            IVectorSearchService vectorSearchService,
            ILogger<EnhancedTransportAnalysisService> logger)
        {
            _kernel = kernel;
            _vectorSearchService = vectorSearchService;
            _logger = logger;

            if (_kernel.Plugins.Count == 0 || _kernel.Plugins["SAPAnalysis"] == null)
            {
                _kernel.Plugins.Add(
                    KernelPluginFactory.CreateFromObject(sAPAnalysisPlugin, "SAPAnalysis")
                    );
            }
        }

        public async Task<AIAnalysisResponse> AnalyzeAsync(AIAnalysisRequest request)
        {
            var transportId = request.TransportAnalysis.FirstOrDefault()?.TransportNumber;
            if (string.IsNullOrEmpty(transportId))
                return new AIAnalysisResponse { Error = "No transportId provided" };
            return await AnalyzeTransportRequest(request, transportId);
        }

        private async Task<AIAnalysisResponse> AnalyzeTransportRequest(AIAnalysisRequest request, string transportId)
        {
            try
            {
                // RAG: Retrieve relevant SAP knowledge
                var transportNumbers = string.Join(", ", request.TransportAnalysis.Select(t => t.TransportNumber));
                var searchQuery = $"SAP transport impact analysis for transports: {transportNumbers}";
                var retrievedDocs = await _vectorSearchService.SearchesAsync(searchQuery, topK: 3);

                // Read YAML file as text
                var yamlText = await File.ReadAllTextAsync("Prompts/ImpactAnalysis.SemanticKernel.yaml");
                // Extract the template block (simple parsing)
                var templateStart = yamlText.IndexOf("template:");
                if (templateStart == -1)
                    return new AIAnalysisResponse { Error = "Prompt template not found in YAML." };
                var templateBlock = yamlText.Substring(templateStart + 9).TrimStart();
                // Remove the first '|' if present
                if (templateBlock.StartsWith("|"))
                {
                    templateBlock = templateBlock.Substring(1).TrimStart('\r', '\n', ' ');
                }
                // Now templateBlock contains the actual prompt
                var analysisPrompt = AIAnalysisUtils.FormatPrompt(templateBlock, request)
                    .Replace("{{TransportId}}", transportId ?? "");

                // Add retrieved context to the prompt
                var context = string.Join("\n\n", retrievedDocs.Select((doc, index) =>
                    $"Knowledge Document {index + 1} (Relevance: {doc.Score:F2}):\n{doc.Content}\nCategory: {doc.Metadata.GetValueOrDefault("Category", "N/A")}\nRisk Level: {doc.Metadata.GetValueOrDefault("RiskLevel", "N/A")}"));

                var enhancedPrompt = $"{analysisPrompt}\n\nRelevant SAP Knowledge from Knowledge Base:\n{context}\n\nUse this historical knowledge to enhance your analysis where applicable.";

                var service = _kernel.Services.GetRequiredService<IChatCompletionService>();
                OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };
                var result = await service.GetChatMessageContentAsync(
                    enhancedPrompt,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: _kernel
                );

                var response = AIAnalysisUtils.ParseAIResponse(result.ToString());
                response.ConfidenceScore = Math.Min(response.ConfidenceScore + 10, 100); // Boost confidence with RAG

                _logger.LogInformation("RAG-enhanced Semantic Kernel analysis completed for transport {TransportId} using {DocCount} knowledge documents",
                    transportId, retrievedDocs.Count);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Enhanced transport analysis failed for transport {TransportId}", transportId);
                return new AIAnalysisResponse { Error = ex.Message };
            }
        }
    }
}
