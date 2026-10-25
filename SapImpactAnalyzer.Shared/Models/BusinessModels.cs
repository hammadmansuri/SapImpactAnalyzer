namespace SapImpactAnalyzer.Models
{
    public class TransportAnalysisRequest
    {
        public List<string> TransportRequestIds { get; set; } // Multiple TRs directly
        //public string AnalysisType { get; set; } = "IMPACT"; // IMPACT, DEPENDENCY, RISK
    }

    public class TransportAnalysisResult
    {
        public string TransportNumber { get; set; }
        public string Status { get; set; }
        public string StatusDescription => Status switch
        {
            "R" => "Released",
            "D" => "Modifiable",
            "L" => "Released (with subsequent changes)",
            _ => "Unknown"
        };
        public List<TransportObjectInfo> ObjectsAffected { get; set; } = new();
        public DateTime CreateDate { get; set; }
        public string CreatedBy { get; set; }
        public RiskLevel CalculatedRisk { get; set; }
        public List<string> BusinessImpacts { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class TransportObjectInfo
    {
        public string ObjectName { get; set; }
        public string ObjectType { get; set; }
        public string ObjectTypeDescription => ObjectType switch
        {
            "W" => "Workbench Object",
            "T" => "Table",
            "P" => "Program",
            "F" => "Function Module",
            _ => "Unknown"
        };
        public string ProgramId { get; set; }
        public string ApplicationArea { get; set; }
    }
    public class ImpactAnalysisSummary
    {
        public int TotalTransports { get; set; }
        public List<TransportAnalysisResult> TransportAnalysis { get; set; } = new();
        public List<DependencyInfo> Dependencies { get; set; } = new();
        public AIAnalysisResponse AIInsights { get; set; } // Reference remains, type is now in AIModels.cs
        public RiskLevel OverallRisk { get; set; }
        public List<string> CriticalRecommendations { get; set; } = new();
        public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
    }

    public enum RiskLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    // Dependency Models
    public class DependencyInfo
    {
        public string ObjectName { get; set; }
        public List<DependencyRelation> UpstreamDependencies { get; set; } = new();
        public List<DependencyRelation> DownstreamDependencies { get; set; } = new();
        public int TotalDependencies => UpstreamDependencies.Count + DownstreamDependencies.Count;
        public RiskLevel RiskLevel { get; set; }
    }

    public class DependencyRelation
    {
        public string RelatedObject { get; set; }
        public string RelationType { get; set; } // CALLS, USES, INCLUDES, etc.
        public string Description { get; set; }
        public RiskLevel Impact { get; set; }
    }
    // AIAnalysisRequest and AIAnalysisResponse moved to AIModels.cs
}
