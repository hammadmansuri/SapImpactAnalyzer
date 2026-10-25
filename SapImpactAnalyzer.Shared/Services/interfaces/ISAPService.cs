using SapImpactAnalyzer.Models;

namespace SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces
{
    public interface ISAPService
    {
        Task<List<SAPTransportObject>> GetTransportObjectsAsync(string transportNumber);
        Task<List<SAPTransportObject>> GetMultipleTransportObjectsAsync(List<string> transportNumbers);
        Task<List<DependencyInfo>> GetDependenciesAsync(List<string> objectNames);
        Task<DependencyInfo> GetObjectDependenciesAsync(string objectName);
    }
}
