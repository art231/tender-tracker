using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using TenderTracker.API.DTOs;
using TenderTracker.API.Services;

namespace TenderTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAngularApp")]
    public class FoundTendersController : ControllerBase
    {
        private readonly IFoundTenderService _foundTenderService;
        private readonly ILogger<FoundTendersController> _logger;

        public FoundTendersController(
            IFoundTenderService foundTenderService,
            ILogger<FoundTendersController> logger)
        {
            _foundTenderService = foundTenderService;
            _logger = logger;
        }

        // GET: api/foundtenders
        [HttpGet]
        public async Task<ActionResult<FoundTenderResponse>> GetFoundTenders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? queryId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] DateTime? applicationDeadlineFrom = null,
            [FromQuery] DateTime? applicationDeadlineTo = null,
            [FromQuery] bool showExpired = false,
            [FromQuery] string? sortBy = "SavedAt",
            [FromQuery] bool sortDescending = true)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 1;
                if (pageSize > 100) pageSize = 100;

                var searchParams = new TenderSearchParams
                {
                    Page = page,
                    PageSize = pageSize,
                    Search = search,
                    QueryId = queryId,
                    FromDate = fromDate,
                    ToDate = toDate,
                    ApplicationDeadlineFrom = applicationDeadlineFrom,
                    ApplicationDeadlineTo = applicationDeadlineTo,
                    ShowExpired = showExpired,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var result = await _foundTenderService.GetTendersAsync(searchParams);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting found tenders");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/foundtenders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FoundTenderDto>> GetFoundTender(int id)
        {
            try
            {
                var tender = await _foundTenderService.GetByIdAsync(id);
                
                if (tender == null)
                {
                    return NotFound($"Tender with ID {id} not found");
                }

                return Ok(tender);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tender with ID: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/foundtenders/count
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetTenderCount()
        {
            try
            {
                var count = await _foundTenderService.GetTotalCountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tender count");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/foundtenders/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            try
            {
                var totalCount = await _foundTenderService.GetTotalCountAsync();
                
                // Здесь можно добавить дополнительную статистику
                // Например, количество тендеров по дням, по запросам и т.д.
                
                return Ok(new
                {
                    TotalTenders = totalCount,
                    LastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tender stats");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
