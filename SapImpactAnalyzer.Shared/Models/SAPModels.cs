using System.Text.Json.Serialization;

namespace SapImpactAnalyzer.Models
{
    // Root response wrapper
    public class SAPTransportResponse
    {
        [JsonPropertyName("d")]
        public SAPDataWrapper Data { get; set; }
    }

    public class SAPDataWrapper
    {
        [JsonPropertyName("results")]
        public List<SAPTransportObject> Results { get; set; }
    }

    // Individual transport object
    public class SAPTransportObject
    {
        [JsonPropertyName("__metadata")]
        public SAPMetadata Metadata { get; set; }

        [JsonPropertyName("Recent")]
        public string Recent { get; set; }

        [JsonPropertyName("Trkorr")]
        public string TransportNumber { get; set; }

        [JsonPropertyName("As4user")]
        public string User { get; set; }

        [JsonPropertyName("Trfunction")]
        public string Function { get; set; } // S = Workbench, K = Customizing

        [JsonPropertyName("Trstatus")]
        public string Status { get; set; } // R = Released, D = Modifiable

        [JsonPropertyName("ObjName")]
        public string ObjectName { get; set; }

        [JsonPropertyName("ObjType")]
        public string ObjectType { get; set; } // W = Workbench object

        [JsonPropertyName("Pgmid")]
        public string ProgramId { get; set; }

        [JsonPropertyName("AppArea")]
        public string ApplicationArea { get; set; }

        [JsonPropertyName("Crtdate")]
        public string CreateDate { get; set; } // SAP Date format

        [JsonPropertyName("Crttime")]
        public string CreateTime { get; set; } // SAP Time format
    }

    public class SAPMetadata
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    // SAP Dependency Response Models
    public class SAPDependencyResponse
    {
        public SAPDependencyDataWrapper d { get; set; }
    }

    public class SAPDependencyDataWrapper
    {
        public List<SAPDependencyObject> results { get; set; } = new();
    }

    public class SAPDependencyObject
    {
        public string ObjectName { get; set; }
        public string RelatedObject { get; set; }
        public string RelationType { get; set; }
        public string Direction { get; set; } // UPSTREAM, DOWNSTREAM
        public string Description { get; set; }
    }
}
