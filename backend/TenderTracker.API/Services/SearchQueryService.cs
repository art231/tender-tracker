using Microsoft.EntityFrameworkCore;
using TenderTracker.API.Data;
using TenderTracker.API.Models;
using TenderTracker.API.DTOs;

namespace TenderTracker.API.Services
{
    public interface ISearchQueryService
    {
        Task<IEnumerable<SearchQueryDto>> GetAllAsync();
        Task<SearchQueryDto?> GetByIdAsync(int id);
        Task<SearchQueryDto> CreateAsync(CreateSearchQueryDto dto);
        Task<SearchQueryDto?> UpdateAsync(int id, UpdateSearchQueryDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<SearchQueryDto>> GetActiveQueriesAsync();
    }

    public class SearchQueryService : ISearchQueryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SearchQueryService> _logger;

        public SearchQueryService(ApplicationDbContext context, ILogger<SearchQueryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<SearchQueryDto>> GetAllAsync()
        {
            var queries = await _context.SearchQueries
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            return queries.Select(MapToDto);
        }

        public async Task<SearchQueryDto?> GetByIdAsync(int id)
        {
            var query = await _context.SearchQueries.FindAsync(id);
            return query != null ? MapToDto(query) : null;
        }

        public async Task<SearchQueryDto> CreateAsync(CreateSearchQueryDto dto)
        {
            var query = new SearchQuery
            {
                Keyword = dto.Keyword,
                Category = dto.Category,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.SearchQueries.Add(query);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created search query: {Keyword} (ID: {Id})", query.Keyword, query.Id);
            return MapToDto(query);
        }

        public async Task<SearchQueryDto?> UpdateAsync(int id, UpdateSearchQueryDto dto)
        {
            var query = await _context.SearchQueries.FindAsync(id);
            if (query == null)
                return null;

            if (dto.Keyword != null)
                query.Keyword = dto.Keyword;
            
            if (dto.Category != null)
                query.Category = dto.Category;
            
            if (dto.IsActive.HasValue)
                query.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated search query ID: {Id}", id);
            return MapToDto(query);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var query = await _context.SearchQueries.FindAsync(id);
            if (query == null)
                return false;

            _context.SearchQueries.Remove(query);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted search query ID: {Id}", id);
            return true;
        }

        public async Task<IEnumerable<SearchQueryDto>> GetActiveQueriesAsync()
        {
            var queries = await _context.SearchQueries
                .Where(q => q.IsActive)
                .OrderBy(q => q.Keyword)
                .ToListAsync();

            return queries.Select(MapToDto);
        }

        private static SearchQueryDto MapToDto(SearchQuery query)
        {
            return new SearchQueryDto
            {
                Id = query.Id,
                Keyword = query.Keyword,
                Category = query.Category,
                IsActive = query.IsActive,
                CreatedAt = query.CreatedAt
            };
        }
    }
}
