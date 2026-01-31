using Microsoft.AspNetCore.Mvc;
using TenderTracker.API.DTOs;
using TenderTracker.API.Services;
using System.Text.Json;

namespace TenderTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TechnologyAnalysisController : ControllerBase
    {
        private readonly ITechnologyAnalysisService _analysisService;
        private readonly ILogger<TechnologyAnalysisController> _logger;

        public TechnologyAnalysisController(
            ITechnologyAnalysisService analysisService,
            ILogger<TechnologyAnalysisController> logger)
        {
            _analysisService = analysisService;
            _logger = logger;
        }

        [HttpPost("tender/{tenderId}/analyze")]
        public async Task<ActionResult<TechnologyAnalysisDto>> AnalyzeTender(int tenderId, [FromQuery] int? documentId = null)
        {
            try
            {
                var analysis = await _analysisService.AnalyzeTenderAsync(tenderId, documentId);
                if (analysis == null)
                {
                    return NotFound($"Tender {tenderId} not found or no text available for analysis");
                }

                return Ok(MapToDto(analysis));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing tender {TenderId}", tenderId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("tender/{tenderId}")]
        public async Task<ActionResult<TechnologyAnalysisDto>> GetAnalysis(int tenderId)
        {
            try
            {
                var analysis = await _analysisService.GetAnalysisAsync(tenderId);
                if (analysis == null)
                {
                    return NotFound($"Analysis not found for tender {tenderId}");
                }

                return Ok(MapToDto(analysis));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analysis for tender {TenderId}", tenderId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TechnologyAnalysisDto>> GetAnalysisById(int id)
        {
            try
            {
                var analysis = await _analysisService.GetAnalysisByIdAsync(id);
                if (analysis == null)
                {
                    return NotFound();
                }

                return Ok(MapToDto(analysis));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analysis by ID: {AnalysisId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("compatible/{isCompatible}")]
        public async Task<ActionResult<List<TechnologyAnalysisDto>>> GetAnalysesByCompatibility(bool isCompatible)
        {
            try
            {
                var analyses = await _analysisService.GetAnalysesByCompatibilityAsync(isCompatible);
                var dtos = analyses.Select(a => MapToDto(a)).ToList();
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting analyses by compatibility: {IsCompatible}", isCompatible);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("compatible-tenders")]
        public async Task<ActionResult<List<FoundTenderDto>>> GetCompatibleTenders([FromQuery] int minMatchScore = 60)
        {
            try
            {
                var tenders = await _analysisService.GetCompatibleTendersAsync(minMatchScore);
                var dtos = tenders.Select(t => new FoundTenderDto
                {
                    Id = t.Id,
                    ExternalId = t.ExternalId,
                    PurchaseNumber = t.PurchaseNumber,
                    Title = t.Title,
                    CustomerName = t.CustomerName,
                    PublishDate = t.PublishDate,
                    DirectLinkToSource = t.DirectLinkToSource,
                    FoundByQueryId = t.FoundByQueryId,
                    SavedAt = t.SavedAt,
                    ApplicationDeadline = t.ApplicationDeadline,
                    MaxPrice = t.MaxPrice,
                    Region = t.Region,
                    CustomerInn = t.CustomerInn,
                    AdditionalInfo = t.AdditionalInfo
                }).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting compatible tenders with min score {MinScore}", minMatchScore);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TechnologyAnalysisDto>> UpdateAnalysis(int id, [FromBody] UpdateAnalysisRequest request)
        {
            try
            {
                var analysis = await _analysisService.UpdateAnalysisAsync(
                    id, request.MatchScore, request.IsCompatible, request.Notes);

                if (analysis == null)
                {
                    return NotFound();
                }

                return Ok(MapToDto(analysis));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating analysis {AnalysisId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/verify")]
        public async Task<ActionResult> MarkAsVerified(int id, [FromBody] VerifyAnalysisRequest request)
        {
            try
            {
                var result = await _analysisService.MarkAsManuallyVerifiedAsync(id, request.Verified, request.Notes);
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking analysis {AnalysisId} as verified", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<Dictionary<string, int>>> GetTechnologyStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var statistics = await _analysisService.GetTechnologyStatisticsAsync(fromDate, toDate);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting technology statistics");
                return StatusCode(500, "Internal server error");
            }
        }

        private TechnologyAnalysisDto MapToDto(Models.TechnologyAnalysis analysis)
        {
            List<MatchedTechnologyDto>? matchedTechnologies = null;
            try
            {
                if (!string.IsNullOrEmpty(analysis.MatchedTechnologiesJson))
                {
                    var matchedTechs = JsonSerializer.Deserialize<List<MatchedTechnology>>(
                        analysis.MatchedTechnologiesJson) ?? new List<MatchedTechnology>();

                    matchedTechnologies = matchedTechs.Select(mt => new MatchedTechnologyDto
                    {
                        Technology = mt.Technology,
                        Count = mt.Count,
                        Mentions = mt.Mentions
                    }).ToList();
                }
            }
            catch (JsonException)
            {
                // Если не удалось распарсить JSON, оставляем null
            }

            return new TechnologyAnalysisDto
            {
                Id = analysis.Id,
                TenderId = analysis.TenderId,
                DocumentId = analysis.DocumentId,
                MatchScore = analysis.MatchScore,
                IsCompatible = analysis.IsCompatible,
                AnalyzedAt = analysis.AnalyzedAt,
                AnalysisNotes = analysis.AnalysisNotes,
                ManuallyVerified = analysis.ManuallyVerified,
                MatchedTechnologies = matchedTechnologies
            };
        }

        private class MatchedTechnology
        {
            public string Technology { get; set; } = string.Empty;
            public int Count { get; set; }
            public List<string> Mentions { get; set; } = new List<string>();
        }
    }

    public class TechnologyAnalysisDto
    {
        public int Id { get; set; }
        public int TenderId { get; set; }
        public int? DocumentId { get; set; }
        public int MatchScore { get; set; }
        public bool IsCompatible { get; set; }
        public DateTime AnalyzedAt { get; set; }
        public string? AnalysisNotes { get; set; }
        public bool ManuallyVerified { get; set; }
        public List<MatchedTechnologyDto>? MatchedTechnologies { get; set; }
    }

    public class MatchedTechnologyDto
    {
        public string Technology { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<string> Mentions { get; set; } = new List<string>();
    }

    public class UpdateAnalysisRequest
    {
        public int MatchScore { get; set; }
        public bool IsCompatible { get; set; }
        public string? Notes { get; set; }
    }

    public class VerifyAnalysisRequest
    {
        public bool Verified { get; set; } = true;
        public string? Notes { get; set; }
    }
}
