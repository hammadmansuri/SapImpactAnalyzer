using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;
using SapImpactAnalyzer.Utils;
using System.Text.Json;

namespace SapImpactAnalyzer.Services.AI
{
    public class AzureOpenAIService : IUnifiedAIAnalysisService
    {
        private readonly AzureOpenAIClient _openAIClient;
        private readonly AzureOpenAIConfig _config;
        private readonly ILogger<AzureOpenAIService> _logger;
        private readonly IVectorSearchService _vectorSearchService;

        public AzureOpenAIService(
            AzureOpenAIClient openAIClient,
            IOptions<AzureOpenAIConfig> config,
            ILogger<AzureOpenAIService> logger,
            IVectorSearchService vectorSearchService)
        {
            _openAIClient = openAIClient;
            _config = config.Value;
            _logger = logger;
            _vectorSearchService = vectorSearchService;
        }

        public async Task<AIAnalysisResponse> AnalyzeAsync(AIAnalysisRequest request)
        {
            try
            {
                // RAG: Retrieve relevant SAP knowledge
                var transportNumbers = string.Join(", ", request.TransportAnalysis.Select(t => t.TransportNumber));
                var searchQuery = $"SAP transport impact analysis for transports: {transportNumbers}";
                var retrievedDocs = await _vectorSearchService.SearchesAsync(searchQuery, topK: 3);

                var promptTemplate = await File.ReadAllTextAsync("Prompts/ImpactAnalysis.yaml");
                string prompt = AIAnalysisUtils.FormatPrompt(promptTemplate, request);

                // Add retrieved context to the prompt
                var context = string.Join("\n\n", retrievedDocs.Select((doc, index) =>
                    $"Knowledge Document {index + 1} (Relevance: {doc.Score:F2}):\n{doc.Content}\nCategory: {doc.Metadata.GetValueOrDefault("Category", "N/A")}\nRisk Level: {doc.Metadata.GetValueOrDefault("RiskLevel", "N/A")}"));

                var enhancedPrompt = $"{prompt}\n\nRelevant SAP Knowledge from Knowledge Base:\n{context}\n\nUse this historical knowledge to enhance your analysis where applicable.";

                var chatCompletionsOptions = new ChatCompletionOptions
                {
                    Temperature = 0.3f,
                    TopP = 0.95f,
                    MaxOutputTokenCount = 1200, // Increased for context
                    FrequencyPenalty = 0.0f,
                    PresencePenalty = 0.0f,
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
                };

                ChatClient chatClient = _openAIClient.GetChatClient(_config.DeploymentName);
                var response = await chatClient.CompleteChatAsync(
                    messages:
                    [
                        new SystemChatMessage("You are an expert SAP Basis consultant specializing in transport management and impact analysis. Use the provided knowledge base context to inform your analysis with historical insights and lessons learned."),
                        new UserChatMessage(enhancedPrompt)
                    ],
                    chatCompletionsOptions
                );

                var content = response.Value.Content;

                var result = AIAnalysisUtils.ParseAIResponse(content[0].Text.ToString());
                result.ConfidenceScore = Math.Min(result.ConfidenceScore + 10, 100); // Boost confidence with RAG

                _logger.LogInformation("RAG-enhanced analysis completed for {TransportCount} transports using {DocCount} knowledge documents",
                    request.TransportAnalysis.Count, retrievedDocs.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure OpenAI analysis failed for transport {TransportId}", request.TransportAnalysis.FirstOrDefault()?.TransportNumber);
                return new AIAnalysisResponse { Error = ex.Message };
            }
        }
    }
}
