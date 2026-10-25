namespace SapImpactAnalyzer.Models
{
    public class AzureOpenAIConfig
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = string.Empty;
        public string EmbeddingDeploymentName { get; set; } = string.Empty;
    }

    public class AzureSearchConfig
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string IndexName { get; set; } = string.Empty;
    }
}
