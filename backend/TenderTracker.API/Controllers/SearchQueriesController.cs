using Microsoft.AspNetCore.Mvc;
using TenderTracker.API.DTOs;
using TenderTracker.API.Services;

namespace TenderTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchQueriesController : ControllerBase
    {
        private readonly ISearchQueryService _searchQueryService;
        private readonly ILogger<SearchQueriesController> _logger;

        public SearchQueriesController(
            ISearchQueryService searchQueryService,
            ILogger<SearchQueriesController> logger)
        {
            _searchQueryService = searchQueryService;
            _logger = logger;
        }

        // GET: api/searchqueries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SearchQueryDto>>> GetSearchQueries()
        {
            try
            {
                var queries = await _searchQueryService.GetAllAsync();
                return Ok(queries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search queries");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/searchqueries/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SearchQueryDto>> GetSearchQuery(int id)
        {
            try
            {
                var query = await _searchQueryService.GetByIdAsync(id);
                
                if (query == null)
                {
                    return NotFound($"Search query with ID {id} not found");
                }

                return Ok(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search query with ID: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/searchqueries
        [HttpPost]
        public async Task<ActionResult<SearchQueryDto>> CreateSearchQuery(CreateSearchQueryDto createDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(createDto.Keyword))
                {
                    return BadRequest("Keyword is required");
                }

                var createdQuery = await _searchQueryService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetSearchQuery), new { id = createdQuery.Id }, createdQuery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating search query");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/searchqueries/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSearchQuery(int id, UpdateSearchQueryDto updateDto)
        {
            try
            {
                var updatedQuery = await _searchQueryService.UpdateAsync(id, updateDto);
                
                if (updatedQuery == null)
                {
                    return NotFound($"Search query with ID {id} not found");
                }

                return Ok(updatedQuery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating search query with ID: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/searchqueries/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSearchQuery(int id)
        {
            try
            {
                var deleted = await _searchQueryService.DeleteAsync(id);
                
                if (!deleted)
                {
                    return NotFound($"Search query with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting search query with ID: {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/searchqueries/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<SearchQueryDto>>> GetActiveQueries()
        {
            try
            {
                var activeQueries = await _searchQueryService.GetActiveQueriesAsync();
                return Ok(activeQueries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active search queries");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
