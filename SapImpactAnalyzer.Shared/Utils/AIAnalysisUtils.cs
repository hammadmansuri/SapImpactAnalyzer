using OpenAI;
using SapImpactAnalyzer.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SapImpactAnalyzer.Utils
{
    public static class AIAnalysisUtils
    {
        public static string FormatPrompt(string template, AIAnalysisRequest request)
        {
            var firstTransport = request.TransportAnalysis.FirstOrDefault();
            var objectsModified = firstTransport != null && firstTransport.ObjectsAffected != null
                ? string.Join(", ", firstTransport.ObjectsAffected.Select(o => $"{o.ObjectType}:{o.ObjectName}"))
                : string.Empty;
            return template
                .Replace("{{TransportId}}", firstTransport?.TransportNumber ?? "")
                .Replace("{{Status}}", firstTransport?.Status ?? "")
                .Replace("{{ObjectsModified}}", objectsModified)
                .Replace("{{DependenciesCount}}", request.Dependencies?.Count.ToString() ?? "0")
                .Replace("{{User}}", firstTransport?.CreatedBy ?? "");
        }

        public static AIAnalysisResponse ParseAIResponse(string aiText)
        {
            try
            {
                // Try to extract JSON from AI response
                var jsonMatch = Regex.Match(aiText, @"\{.*\}", RegexOptions.Singleline);
                if (jsonMatch.Success)
                {
                    var response = JsonSerializer.Deserialize<AIAnalysisResponse>(jsonMatch.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (response != null) return response;
                }
            }
            catch (Exception ex)
            {
                return new AIAnalysisResponse { Error = ex.Message };
            }
            // Fallback parsing
            return new AIAnalysisResponse { Error = "Failed to parse AI response" };
        }
    }
}
