using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;
using System.Text.Json;

namespace SapImpactAnalyzer.Services.Transport
{
    public class SAPService : ISAPService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SAPService> _logger;
        private readonly string _sapGetTransportObjectsEndpoint;
        private readonly string _basicAuthHeader;
        public SAPService(HttpClient httpClient, ILogger<SAPService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _sapGetTransportObjectsEndpoint = configuration["SapApi:BaseUrl"] + configuration["SapApi:TransportAPIUrl"] ?? throw new ArgumentNullException("SAP:GetTransportObjectsEndpoint configuration is missing");
            
            var authValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{configuration["SapApi:Username"]}:{configuration["SapApi:Password"]}"));
            _basicAuthHeader = $"Basic {authValue}";
        }

        public async Task<List<SAPTransportObject>> GetTransportObjectsAsync(string transportNumber)
        {
            try
            {
                var requestQuery = $"$filter=Trkorr eq '{transportNumber}'&$format=json";
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_sapGetTransportObjectsEndpoint}?{requestQuery}");
                request.Headers.Add("Authorization", _basicAuthHeader);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                var sapResponse = JsonSerializer.Deserialize<SAPTransportResponse>(jsonContent);

                return sapResponse?.Data?.Results ?? new List<SAPTransportObject>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error while fetching transport {TransportNumber}", transportNumber);
                throw new ApplicationException($"Failed to fetch transport data for {transportNumber}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error for transport {TransportNumber}", transportNumber);
                throw new ApplicationException($"Invalid response format for transport {transportNumber}", ex);
            }
        }

        public async Task<List<SAPTransportObject>> GetMultipleTransportObjectsAsync(List<string> transportNumbers)
        {
            var allObjects = new List<SAPTransportObject>();

            // Process transports in parallel with throttling
            var semaphore = new SemaphoreSlim(3, 3); // Max 3 concurrent requests
            var tasks = transportNumbers.Select(async trNumber =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await GetTransportObjectsAsync(trNumber);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                allObjects.AddRange(result);
            }

            return allObjects;
        }


        public async Task<List<DependencyInfo>> GetDependenciesAsync(List<string> objectNames)
        {
            var dependencies = new List<DependencyInfo>();

            var semaphore = new SemaphoreSlim(2, 2); // Limit dependency calls
            var tasks = objectNames.Select(async objName =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await GetObjectDependenciesAsync(objName);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            dependencies.AddRange(results.Where(r => r != null));

            return dependencies;
        }

        public async Task<DependencyInfo> GetObjectDependenciesAsync(string objectName)
        {
            try
            {
                _logger.LogInformation("Fetching dependencies for object: {ObjectName}", objectName);

                // Call SAP Dependency API - adjust endpoint as per your SAP API
                var response = await _httpClient.GetAsync($"Dependencies?$filter=ObjectName eq '{objectName}'");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new DependencyInfo { ObjectName = objectName };
                }

                response.EnsureSuccessStatusCode();
                var jsonContent = await response.Content.ReadAsStringAsync();

                // Parse dependency response - adjust based on actual SAP API structure
                var dependencyResponse = JsonSerializer.Deserialize<SAPDependencyResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return MapToDependencyInfo(objectName, dependencyResponse);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Could not fetch dependencies for {ObjectName}", objectName);
                return new DependencyInfo { ObjectName = objectName };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dependencies for {ObjectName}", objectName);
                return new DependencyInfo { ObjectName = objectName };
            }
        }

        private DependencyInfo MapToDependencyInfo(string objectName, SAPDependencyResponse response)
        {
            var dependencyInfo = new DependencyInfo
            {
                ObjectName = objectName
            };

            if (response?.d?.results != null)
            {
                foreach (var dep in response.d.results)
                {
                    var relation = new DependencyRelation
                    {
                        RelatedObject = dep.RelatedObject,
                        RelationType = dep.RelationType,
                        Description = $"{dep.RelationType}: {dep.RelatedObject}",
                        Impact = DetermineImpactLevel(dep.RelationType)
                    };

                    if (dep.Direction == "UPSTREAM")
                        dependencyInfo.UpstreamDependencies.Add(relation);
                    else
                        dependencyInfo.DownstreamDependencies.Add(relation);
                }
            }

            return dependencyInfo;
        }

        private RiskLevel DetermineImpactLevel(string relationType)
        {
            return relationType switch
            {
                "CALLS" => RiskLevel.High,
                "USES" => RiskLevel.Medium,
                "INCLUDES" => RiskLevel.Medium,
                "REFERENCES" => RiskLevel.Low,
                _ => RiskLevel.Low
            };
        }
    }
}
