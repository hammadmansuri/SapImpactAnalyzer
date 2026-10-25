using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;
using SapImpactAnalyzer.Shared.Abstractions;

namespace SapImpactAnalyzer.AzureFunctions
{
    public class TransportAnalysisFunction
    {
        private readonly ITransportAnalysisService _transportAnalysisService;
        private readonly IUnifiedAIAnalysisService _aiAnalysisService;
        private readonly IComputeService _computeService;
        private readonly ILogger<TransportAnalysisFunction> _logger;

        public TransportAnalysisFunction(
            ITransportAnalysisService transportAnalysisService,
            IUnifiedAIAnalysisService aiAnalysisService,
            IComputeService computeService,
            ILogger<TransportAnalysisFunction> logger)
        {
            _transportAnalysisService = transportAnalysisService;
            _aiAnalysisService = aiAnalysisService;
            _computeService = computeService;
            _logger = logger;
        }

        [Function("AnalyzeTransport")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "analyze")] 
            HttpRequestData req,
            FunctionContext executionContext)
        {
            _logger.LogInformation("Transport analysis function triggered.");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var request = System.Text.Json.JsonSerializer.Deserialize<TransportAnalysisRequest>(requestBody);

                if (request?.TransportRequestIds == null || !request.TransportRequestIds.Any())
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("At least one transport request ID is required");
                    return badRequestResponse;
                }

                // Validate transport request format (basic validation)
                var invalidTRs = request.TransportRequestIds.Where(tr => string.IsNullOrWhiteSpace(tr) || tr.Length < 6).ToList();
                if (invalidTRs.Any())
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync($"Invalid transport request IDs: {string.Join(", ", invalidTRs)}");
                    return badRequestResponse;
                }

                _logger.LogInformation("Starting analysis for transport requests: {TransportIds}", string.Join(", ", request.TransportRequestIds));

                var analysis = await _computeService.AnalyzeTransportRequestsAsync(request.TransportRequestIds);

                _logger.LogInformation("Analysis completed for {Count} transports with overall risk: {Risk}",
                    analysis.TotalTransports, analysis.OverallRisk);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(analysis);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze transport requests");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Internal server error occurred during analysis");
                return errorResponse;
            }
        }

        [Function("GetTransportAnalysis")]
        public async Task<HttpResponseData> GetTransportAnalysis(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "analyze/{transportId}")] 
            HttpRequestData req,
            string transportId,
            FunctionContext executionContext)
        {
            _logger.LogInformation("Get transport analysis function triggered for transport: {TransportId}", transportId);

            try
            {
                if (string.IsNullOrWhiteSpace(transportId))
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Transport ID is required");
                    return badRequestResponse;
                }

                var analysis = await _computeService.AnalyzeSingleTransportAsync(transportId);

                if (analysis.Status == "NOT_FOUND")
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"Transport request {transportId} not found");
                    return notFoundResponse;
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(analysis);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze transport {TransportId}", transportId);
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("Internal server error occurred during analysis");
                return errorResponse;
            }
        }

        [Function("Health")]
        public HttpResponseData Health(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] 
            HttpRequestData req,
            FunctionContext executionContext)
        {
            _logger.LogInformation("Health check function triggered.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteStringAsync("{\"Status\": \"Healthy\", \"Timestamp\": \"" + DateTime.UtcNow.ToString("O") + "\"}");
            return response;
        }
    }
}
