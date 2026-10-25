namespace SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces
{
    public interface ISearchIndexManager
    {
        Task CreateIndexAsync();
        Task DeleteIndexAsync();
        Task<bool> IndexExistsAsync();
    }
}
