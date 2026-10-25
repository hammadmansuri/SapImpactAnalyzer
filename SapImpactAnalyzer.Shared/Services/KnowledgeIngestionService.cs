using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;
using System.Text.Json;

namespace SapImpactAnalyzer.Services
{
    public class KnowledgeIngestionService
    {
        private readonly IVectorSearchService _vectorSearchService;
        private readonly ISearchIndexManager _indexManager;
        private readonly ILogger<KnowledgeIngestionService> _logger;

        public KnowledgeIngestionService(
            IVectorSearchService vectorSearchService,
            ISearchIndexManager indexManager,
            ILogger<KnowledgeIngestionService> logger)
        {
            _vectorSearchService = vectorSearchService;
            _indexManager = indexManager;
            _logger = logger;
        }

        public async Task IngestSampleKnowledgeAsync()
        {
            // Ensure the search index exists before ingesting documents
            _logger.LogInformation("Checking if search index exists...");
            if (!await _indexManager.IndexExistsAsync())
            {
                _logger.LogInformation("Search index does not exist. Creating index...");
                await _indexManager.CreateIndexAsync();
                _logger.LogInformation("Search index created successfully");
            }
            else
            {
                _logger.LogInformation("Search index already exists");
            }

            var sampleDocuments = GetSampleSAPKnowledge();

            _logger.LogInformation("Starting ingestion of {Count} sample SAP knowledge documents", sampleDocuments.Count);

            foreach (var doc in sampleDocuments)
            {
                try
                {
                    await _vectorSearchService.IndexDocumentAsync(JsonSerializer.Serialize(doc));
                    _logger.LogInformation("Ingested knowledge document: {Id}", doc.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest document {Id}", doc.Id);
                    throw;
                }
            }

            _logger.LogInformation("Completed ingestion of sample SAP knowledge");
        }

        private List<SAPKnowledgeDocument> GetSampleSAPKnowledge()
        {
            return new List<SAPKnowledgeDocument>
            {
                new SAPKnowledgeDocument
                {
                    Id = "incident-hr-001",
                    Content = "HR transport TR001234 caused system-wide user lockout during peak business hours. Issue occurred due to incorrect authorization role assignment in production. Resolution required emergency rollback and affected 500+ users for 2 hours. Root cause: Missing quality assurance testing for role changes.",
                    Category = "historical_incident",
                    RiskLevel = "Critical",
                    BusinessImpact = "user_impact",
                    UsersAffected = 500,
                    BusinessProcess = "Employee Self-Service",
                    SAPModules = new[] { "HR" },
                    ObjectTypes = new[] { "ROLE", "AUTH" },
                    Source = "incident_report"
                },
                new SAPKnowledgeDocument
                {
                    Id = "incident-fi-002",
                    Content = "Financial posting transport TR005678 resulted in incorrect GL account balances. Issue affected month-end reporting and required manual corrections. Prevention: Always validate posting logic in QA environment with real data before production deployment.",
                    Category = "historical_incident",
                    RiskLevel = "High",
                    BusinessImpact = "financial_loss",
                    UsersAffected = 50,
                    BusinessProcess = "General Ledger",
                    SAPModules = new[] { "FI" },
                    ObjectTypes = new[] { "PROG", "TABU" },
                    Source = "incident_report"
                },
                new SAPKnowledgeDocument
                {
                    Id = "best-practice-001",
                    Content = "Best practice: Always perform end-to-end testing including integration points before transport deployment. Use transport simulation tools to identify potential conflicts. Maintain detailed rollback procedures for all high-risk transports.",
                    Category = "best_practice",
                    RiskLevel = "Low",
                    BusinessImpact = "process_disruption",
                    Source = "team_knowledge"
                },
                new SAPKnowledgeDocument
                {
                    Id = "lesson-003",
                    Content = "Lesson learned: Custom code transports should be reviewed by senior developers. Recent incident showed untested custom enhancement caused performance degradation affecting 1000+ transactions per hour.",
                    Category = "lesson_learned",
                    RiskLevel = "Medium",
                    BusinessImpact = "process_disruption",
                    UsersAffected = 1000,
                    SAPModules = new[] { "SD", "MM" },
                    ObjectTypes = new[] { "ENHO", "FUNC" },
                    Source = "team_knowledge"
                },
                new SAPKnowledgeDocument
                {
                    Id = "incident-mm-004",
                    Content = "Materials Management transport TR009876 caused procurement process interruption. Purchase orders could not be created for 4 hours. Issue was due to missing table entries in custom configuration. Always verify custom tables are included in transport requests.",
                    Category = "historical_incident",
                    RiskLevel = "High",
                    BusinessImpact = "process_disruption",
                    UsersAffected = 200,
                    BusinessProcess = "Procure-to-Pay",
                    SAPModules = new[] { "MM" },
                    ObjectTypes = new[] { "TABU", "VIEW" },
                    Source = "incident_report"
                },
                new SAPKnowledgeDocument
                {
                    Id = "best-practice-002",
                    Content = "Best practice for high-risk transports: Schedule during maintenance windows, prepare detailed communication plan, have rollback procedures ready, and ensure key stakeholders are available during deployment.",
                    Category = "best_practice",
                    RiskLevel = "Low",
                    Source = "documentation"
                }
            };
        }
    }
}
