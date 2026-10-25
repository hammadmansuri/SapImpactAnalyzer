using Microsoft.AspNetCore.Mvc;
using SapImpactAnalyzer.Models;
using SapImpactAnalyzer.SapImpactAnalyzer.Shared.Services.interfaces;
using SapImpactAnalyzer.Services;
using SapImpactAnalyzer.Utils;

namespace SapImpactAnalyzer.Controllers
{
    [ApiController]
    [Route("api/impact-analysis")]
    public class ImpactAnalysisController : ControllerBase
    {
        //public IActionResult Index()
        //{
        //    return View();
        //}

        private readonly ISAPService _sAPService;
        private readonly KnowledgeIngestionService _knowledgeIngestionService;

        public ImpactAnalysisController(ISAPService sAPService, KnowledgeIngestionService knowledgeIngestionService)
        {
            _sAPService = sAPService;
            _knowledgeIngestionService = knowledgeIngestionService;
        }


        [HttpGet("analyze-tr/{transportNumber}")]
        public async Task<ActionResult<TransportAnalysisResult>> GetTransportAnalysis(string transportNumber)
        {
            var sapObjects = await _sAPService.GetTransportObjectsAsync(transportNumber);
            
            var uniqueObjects = sapObjects
                .GroupBy(obj => new { obj.ObjectName, obj.ObjectType })
                .Select(g => new TransportObjectInfo
                {
                    ObjectName = g.Key.ObjectName,
                    ObjectType = g.Key.ObjectType,
                    ProgramId = g.First().ProgramId,
                    ApplicationArea = g.First().ApplicationArea
                })
                .ToList();

            var analysis = new TransportAnalysisResult
            {
                TransportNumber = transportNumber,
                Status = sapObjects.FirstOrDefault()?.Status ?? "Unknown",
                ObjectsAffected = uniqueObjects,
                CreateDate = SAPDateTimeHelper.ParseSAPDate(sapObjects.FirstOrDefault()?.CreateDate ?? ""),
                CreatedBy = sapObjects.FirstOrDefault()?.User ?? ""
            };

            return Ok(analysis);
        }

        [HttpPost("ingest-knowledge")]
        public async Task<IActionResult> IngestKnowledge()
        {
            try
            {
                await _knowledgeIngestionService.IngestSampleKnowledgeAsync();
                return Ok(new { message = "Knowledge ingestion completed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Knowledge ingestion failed: {ex.Message}" });
            }
        }
    }
}
