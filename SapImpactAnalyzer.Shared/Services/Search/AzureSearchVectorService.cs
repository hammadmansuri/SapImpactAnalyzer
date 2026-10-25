using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;
using System.Text.Json;

namespace SapImpactAnalyzer.Services.Search
{
    public class AzureSearchVectorService : IVectorSearchService
    {
        private readonly SearchClient _searchClient;
        private readonly AzureOpenAIClient _openAIClient;
        private readonly AzureOpenAIConfig _config;
        private readonly ILogger<AzureSearchVectorService> _logger;

        public AzureSearchVectorService(
            SearchClient searchClient,
            AzureOpenAIClient openAIClient,
            IOptions<AzureOpenAIConfig> config,
            ILogger<AzureSearchVectorService> logger)
        {
            _searchClient = searchClient;
            _openAIClient = openAIClient;
            _config = config.Value;
            _logger = logger;
        }

        public async Task AddDocumentAsync(string documentId, string content)
        {
            try
            {
                var embedding = await GenerateEmbeddingAsync(content);

                var document = new SAPKnowledgeDocument
                {
                    Id = documentId,
                    Content = content,
                    ContentVector = embedding,
                    CreatedDate = DateTimeOffset.UtcNow,
                    Category = "general",
                    Source = "user_input"
                };

                var batch = IndexDocumentsBatch.Upload(new[] { document });
                var result = await _searchClient.IndexDocumentsAsync(batch);

                if (result.Value.Results.Any(r => !r.Succeeded))
                {
                    var failed = result.Value.Results.Where(r => !r.Succeeded);
                    _logger.LogError("Failed to index documents: {Errors}", string.Join(", ", failed.Select(f => f.ErrorMessage)));
                }
                else
                {
                    _logger.LogInformation("Successfully indexed document {DocumentId}", documentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding document {DocumentId}", documentId);
                throw;
            }
        }

        public async Task IndexDocumentAsync(string documentJson)
        {
            try
            {
                var document = JsonSerializer.Deserialize<SAPKnowledgeDocument>(documentJson);
                if (document == null)
                {
                    throw new ArgumentException("Invalid document JSON");
                }

                // Generate embedding if not provided
                if (document.ContentVector.IsEmpty)
                {
                    document.ContentVector = await GenerateEmbeddingAsync(document.Content);
                }

                // Set defaults if not provided
                document.CreatedDate = document.CreatedDate == default ? DateTimeOffset.UtcNow : document.CreatedDate;
                document.Category = string.IsNullOrEmpty(document.Category) ? "general" : document.Category;
                document.Source = string.IsNullOrEmpty(document.Source) ? "api_input" : document.Source;

                var batch = IndexDocumentsBatch.Upload(new[] { document });
                var result = await _searchClient.IndexDocumentsAsync(batch);

                if (result.Value.Results.Any(r => !r.Succeeded))
                {
                    var failed = result.Value.Results.Where(r => !r.Succeeded);
                    _logger.LogError("Failed to index document: {Errors}", string.Join(", ", failed.Select(f => f.ErrorMessage)));
                }
                else
                {
                    _logger.LogInformation("Successfully indexed document {DocumentId}", document.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing document JSON");
                throw;
            }
        }

        public async Task<List<SearchResult>> SearchesAsync(string query, int topK = 5)
        {
            try
            {
                var queryEmbedding = await GenerateEmbeddingAsync(query);

                var searchOptions = new SearchOptions
                {
                    Size = topK,
                    Select = { "Id", "Content", "Category", "TransportId", "ObjectTypes", "SAPModules", "RiskLevel", "BusinessImpact", "CreatedDate", "UsersAffected", "BusinessProcess", "ConfidenceScore", "Source" },
                    VectorSearch = new()
                    {
                        Queries =
                        {
                            new VectorizedQuery(queryEmbedding.ToArray())
                            {
                                KNearestNeighborsCount = topK,
                                Fields = { "ContentVector" }
                            }
                        }
                    }
                };

                var response = await _searchClient.SearchAsync<SAPKnowledgeDocument>(query, searchOptions);

                var results = new List<SearchResult>();
                await foreach (var result in response.Value.GetResultsAsync())
                {
                    results.Add(new SearchResult
                    {
                        DocumentId = result.Document.Id,
                        Content = result.Document.Content,
                        Score = result.Score ?? 0,
                        Metadata = new Dictionary<string, object>
                        {
                            ["Category"] = result.Document.Category,
                            ["RiskLevel"] = result.Document.RiskLevel,
                            ["BusinessImpact"] = result.Document.BusinessImpact,
                            ["CreatedDate"] = result.Document.CreatedDate,
                            ["Source"] = result.Document.Source
                        }
                    });
                }

                _logger.LogInformation("Vector search completed for query '{Query}', returned {Count} results", query, results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing vector search for query '{Query}'", query);
                throw;
            }
        }

        private async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var embeddingDeployment = _config.EmbeddingDeploymentName ?? "text-embedding-ada-002";
                var client = _openAIClient.GetEmbeddingClient(embeddingDeployment);

                var result = await client.GenerateEmbeddingsAsync(new[] { text });
                return new ReadOnlyMemory<float>(result.Value[0].ToFloats().ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for text");
                throw;
            }
        }
    }
}
