using Microsoft.AspNetCore.Mvc;
using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;
using SapImpactAnalyzer.Services.Transport;

namespace SapImpactAnalyzer.Controllers
{
    [ApiController]
    [Route("api/transport-analysis")]
    public class TransportAnalysisController : ControllerBase
    {
        private readonly ITransportAnalysisService _analysisService;
        private readonly IUnifiedAIAnalysisService _unifiedAIanalysisService;
        private readonly ILogger<TransportAnalysisController> _logger;
        private readonly EnhancedTransportAnalysisService _enhancedAnalysisService;
        public TransportAnalysisController(
            ITransportAnalysisService analysisService, 
            ILogger<TransportAnalysisController> logger,
            EnhancedTransportAnalysisService enhancedTransportAnalysisService,
            IUnifiedAIAnalysisService unifiedAIanalysisService)
        {
            _analysisService = analysisService;
            _logger = logger;
            _enhancedAnalysisService = enhancedTransportAnalysisService;
            _unifiedAIanalysisService = unifiedAIanalysisService;
        }

        [HttpPost("analyze")]
        [ProducesResponseType(typeof(ImpactAnalysisSummary), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ImpactAnalysisSummary>> AnalyzeTransportRequests(
        [FromBody] TransportAnalysisRequest request)
        {
            if (request?.TransportRequestIds == null || !request.TransportRequestIds.Any())
            {
                return BadRequest("At least one transport request ID is required");
            }
            
            // Validate transport request format (basic validation)
            var invalidTRs = request.TransportRequestIds.Where(tr => string.IsNullOrWhiteSpace(tr) || tr.Length < 6).ToList();
            if (invalidTRs.Any())
            {
                return BadRequest($"Invalid transport request IDs: {string.Join(", ", invalidTRs)}");
            }

            try
            {
                _logger.LogInformation("Starting analysis for transport requests: {TransportIds}", string.Join(", ", request.TransportRequestIds));

                var analysis = await _analysisService.AnalyzeTransportRequestsAsync(request.TransportRequestIds);

                _logger.LogInformation("Analysis completed for {Count} transports with overall risk: {Risk}",
                    analysis.TotalTransports, analysis.OverallRisk);

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze transport requests");
                return StatusCode(500, "Internal server error occurred during analysis");
            }

        }

        [HttpPost("analyze-using-sk")]
        public async Task<ActionResult<ImpactAnalysisSummary>> AnalyzeTransport(
            [FromBody] TransportAnalysisRequest request)
        {
            // Use the SK-powered service instead of separate SAP/AI calls
            var aiRequest = new AIAnalysisRequest
            {
                TransportAnalysis = new List<TransportAnalysisResult>(),
                Dependencies = new List<DependencyInfo>(),
                BusinessContext = "SAP Production Release"
            };
            if (request.TransportRequestIds != null && request.TransportRequestIds.Any())
            {
                aiRequest.TransportAnalysis.Add(new TransportAnalysisResult { TransportNumber = request.TransportRequestIds.FirstOrDefault() });
            }
            var analysis = await _enhancedAnalysisService.AnalyzeAsync(aiRequest);

            if (!string.IsNullOrEmpty(analysis.Error))
            {
                return BadRequest(new { Error = analysis.Error });
            }

            return Ok(analysis);
        }

        [HttpGet("{transportId}")]
        [ProducesResponseType(typeof(TransportAnalysisResult), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<TransportAnalysisResult>> GetTransportAnalysis(string transportId)
        {
            if (string.IsNullOrWhiteSpace(transportId))
            {
                return BadRequest("Transport ID is required");
            }

            try
            {
                var analysis = await _analysisService.AnalyzeSingleTransportAsync(transportId);

                if (analysis.Status == "NOT_FOUND")
                {
                    return NotFound($"Transport request {transportId} not found");
                }

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze transport {TransportId}", transportId);
                return StatusCode(500, "Internal server error occurred during analysis");
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
        }
    }
}
