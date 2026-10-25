using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Options;
using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;

namespace SapImpactAnalyzer.Services.Search
{
    public class AzureSearchIndexManager : ISearchIndexManager
    {
        private readonly AzureSearchConfig _configuration;
        private readonly SearchIndexClient _indexClient;
        private readonly ILogger<AzureSearchIndexManager> _logger;

        public AzureSearchIndexManager(
            IOptions<AzureSearchConfig> configuration,
            SearchIndexClient indexClient,
            ILogger<AzureSearchIndexManager> logger)
        {
            _configuration = configuration.Value;
            _indexClient = indexClient;
            _logger = logger;
        }
        public async Task CreateIndexAsync()
        {
            var searchIndex = new SearchIndex(_configuration.IndexName)
            {
                Fields =
                {
                    new SimpleField("Id", SearchFieldDataType.String) { IsKey = true },
                    new SearchableField("Content") { IsFilterable = true },
                new VectorSearchField("ContentVector", 1536, "sap-vector-profile"),
                
                // Metadata fields
                new SimpleField("Category", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("TransportId", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("ObjectTypes", SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFilterable = true, IsFacetable = true },
                new SimpleField("SAPModules", SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFilterable = true, IsFacetable = true },
                new SimpleField("RiskLevel", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("BusinessImpact", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("CreatedDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SimpleField("UsersAffected", SearchFieldDataType.Int32) { IsFilterable = true },
                new SearchableField("BusinessProcess"),
                new SimpleField("ConfidenceScore", SearchFieldDataType.Double),
                new SearchableField("Source")
                },

                VectorSearch = new()
                {
                    Profiles =
                    {
                        new VectorSearchProfile("sap-vector-profile", "sap-algorithm-config")
                    },
                    Algorithms =
                    {
                        new HnswAlgorithmConfiguration("sap-algorithm-config")
                    }
                },

                SemanticSearch = new()
                {
                    Configurations =
                    {
                        new SemanticConfiguration("sap-semantic-configuration", new()
                        {
                            TitleField = new SemanticField("TransportId"),
                            ContentFields = 
                            {
                                new SemanticField("Content"),
                                new SemanticField("BusinessProcess")
                            },
                            KeywordsFields = 
                            { 
                                new SemanticField("Category"), 
                                new SemanticField("RiskLevel") 
                            }
                        })
                    }
                }
            };

            try
            {
                await _indexClient.CreateIndexAsync(searchIndex);
                _logger.LogInformation("Created Azure Search index: {IndexName}", _configuration.IndexName);
            }
            catch(RequestFailedException ex) when (ex.Status == 409)
            {
                _logger.LogWarning("Index {IndexName} already exists.", _configuration.IndexName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Azure Search index: {IndexName}", _configuration.IndexName);
                throw;
            }
        }

        public async Task DeleteIndexAsync()
        {
            try
            {
                await _indexClient.DeleteIndexAsync(_configuration.IndexName);
                _logger.LogInformation("Deleted Azure Search index: {IndexName}", _configuration.IndexName);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("Index {IndexName} does not exist.", _configuration.IndexName);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<bool> IndexExistsAsync()
        {
            try
            {
                await _indexClient.GetIndexAsync(_configuration.IndexName);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
        }
    }
}
