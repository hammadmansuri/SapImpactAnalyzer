using Azure.Search.Documents.Indexes;

namespace SapImpactAnalyzer.Models
{
    public class AIAnalysisRequest
    {
        public List<TransportAnalysisResult> TransportAnalysis { get; set; } = new();
        public List<DependencyInfo> Dependencies { get; set; } = new();
        public string BusinessContext { get; set; }
    }

    public class AIAnalysisResponse
    {
        public string ExecutiveSummary { get; set; }
        public List<string> KeyRisks { get; set; } = new();
        public List<string> BusinessImpacts { get; set; } = new();
        public List<string> TestingRecommendations { get; set; } = new();
        public List<string> RollbackStrategy { get; set; } = new();
        public string DeploymentApproach { get; set; }
        public int ConfidenceScore { get; set; } // 1-100
        public string? Error { get; set; }
    }

    public class SAPKnowledgeDocument
    {
        [SimpleField(IsKey = true)]
        public string Id { get; set; } = string.Empty;

        [SearchableField(IsFilterable = true)]
        public string Content { get; set; } = string.Empty;

        [VectorSearchField(VectorSearchDimensions = 1536, VectorSearchProfileName = "sap-vector-profile")]
        public ReadOnlyMemory<float> ContentVector { get; set; }

        // Metadata for filtering and context
        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public string Category { get; set; } = string.Empty; // "historical_incident", "best_practice", "lesson_learned"

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public string TransportId { get; set; } = string.Empty;

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public string[] ObjectTypes { get; set; } = Array.Empty<string>(); // ["W", "PROG", "TABU"]

        [SimpleField(IsFilterable = true, IsFacetable = true)]
        public string[] SAPModules { get; set; } = Array.Empty<string>(); // ["HR", "FI", "MM"]

        [SimpleField(IsFilterable = true)]
        public string RiskLevel { get; set; } = string.Empty; // "Low", "Medium", "High", "Critical"

        [SimpleField(IsFilterable = true)]
        public string BusinessImpact { get; set; } = string.Empty; // "process_disruption", "user_impact", "financial_loss"

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTimeOffset CreatedDate { get; set; }

        [SimpleField(IsFilterable = true)]
        public int UsersAffected { get; set; }

        [SearchableField]
        public string BusinessProcess { get; set; } = string.Empty; // "Order-to-Cash", "Procure-to-Pay"

        [SimpleField]
        public double ConfidenceScore { get; set; } = 1.0; // How reliable this knowledge is

        [SearchableField]
        public string Source { get; set; } = string.Empty; // "incident_report", "team_knowledge", "documentation"
    }

    public class SAPKnowledgeSearchResults
    {
        public List<SAPKnowledgeDocument> Documents { get; set; } = new();
        public double MaxScore { get; set; }
        public int TotalCount { get; set; }
        public string SearchQuery { get; set; } = string.Empty;
        public TimeSpan SearchDuration { get; set; }
        public Dictionary<string, int> CategoryBreakdown { get; set; } = new();
        public Dictionary<string, int> RiskLevelBreakdown { get; set; } = new();
    }

    public class SearchResult
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double Score { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
