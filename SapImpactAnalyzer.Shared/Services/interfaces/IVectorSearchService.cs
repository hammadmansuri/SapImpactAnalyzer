using SapImpactAnalyzer.Models;

namespace SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces
{
    public interface IVectorSearchService
    {
        Task<List<SearchResult>> SearchesAsync(string query, int topK = 5);
        Task AddDocumentAsync(string documentId, string content);
        Task IndexDocumentAsync(string document);
    }
}
