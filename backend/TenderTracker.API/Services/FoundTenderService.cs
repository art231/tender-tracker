using Microsoft.EntityFrameworkCore;
using TenderTracker.API.Data;
using TenderTracker.API.Models;
using TenderTracker.API.DTOs;

namespace TenderTracker.API.Services
{
    public interface IFoundTenderService
    {
        Task<FoundTenderResponse> GetTendersAsync(TenderSearchParams searchParams);
        Task<FoundTenderDto?> GetByIdAsync(int id);
        Task<bool> ExistsByExternalIdAsync(string externalId);
        Task<FoundTenderDto> AddAsync(FoundTender tender);
        Task<int> AddRangeAsync(IEnumerable<FoundTender> tenders);
        Task<int> GetTotalCountAsync();
    }

    public class FoundTenderService : IFoundTenderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FoundTenderService> _logger;

        public FoundTenderService(ApplicationDbContext context, ILogger<FoundTenderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<FoundTenderResponse> GetTendersAsync(TenderSearchParams searchParams)
        {
            var query = _context.FoundTenders
                .Include(t => t.FoundByQuery)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchParams.Search))
            {
                var search = searchParams.Search.ToLower();
                query = query.Where(t => 
                    t.Title.ToLower().Contains(search) ||
                    t.PurchaseNumber.ToLower().Contains(search) ||
                    (t.CustomerName != null && t.CustomerName.ToLower().Contains(search)) ||
                    (t.AdditionalInfo != null && t.AdditionalInfo.ToLower().Contains(search))
                );
            }

            if (searchParams.QueryId.HasValue)
            {
                query = query.Where(t => t.FoundByQueryId == searchParams.QueryId.Value);
            }

            if (searchParams.FromDate.HasValue)
            {
                query = query.Where(t => t.SavedAt >= searchParams.FromDate.Value);
            }

            if (searchParams.ToDate.HasValue)
            {
                query = query.Where(t => t.SavedAt <= searchParams.ToDate.Value);
            }

            // Фильтрация по сроку подачи заявок
            if (searchParams.ApplicationDeadlineFrom.HasValue)
            {
                query = query.Where(t => t.ApplicationDeadline >= searchParams.ApplicationDeadlineFrom.Value);
            }

            if (searchParams.ApplicationDeadlineTo.HasValue)
            {
                query = query.Where(t => t.ApplicationDeadline <= searchParams.ApplicationDeadlineTo.Value);
            }

            // Фильтрация истекших тендеров (по умолчанию скрываем)
            if (!searchParams.ShowExpired)
            {
                query = query.Where(t => t.ApplicationDeadline == null || t.ApplicationDeadline > DateTime.UtcNow);
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply sorting
            query = searchParams.SortBy?.ToLower() switch
            {
                "publishdate" => searchParams.SortDescending 
                    ? query.OrderByDescending(t => t.PublishDate)
                    : query.OrderBy(t => t.PublishDate),
                "title" => searchParams.SortDescending
                    ? query.OrderByDescending(t => t.Title)
                    : query.OrderBy(t => t.Title),
                "applicationdeadline" => searchParams.SortDescending
                    ? query.OrderByDescending(t => t.ApplicationDeadline)
                    : query.OrderBy(t => t.ApplicationDeadline),
                "maxprice" => searchParams.SortDescending
                    ? query.OrderByDescending(t => t.MaxPrice)
                    : query.OrderBy(t => t.MaxPrice),
                _ => searchParams.SortDescending
                    ? query.OrderByDescending(t => t.SavedAt)
                    : query.OrderBy(t => t.SavedAt)
            };

            // Apply pagination
            var tenders = await query
                .Skip((searchParams.Page - 1) * searchParams.PageSize)
                .Take(searchParams.PageSize)
                .ToListAsync();

            // Map to DTOs
            var tenderDtos = tenders.Select(MapToDto).ToList();

            return new FoundTenderResponse
            {
                Tenders = tenderDtos,
                TotalCount = totalCount,
                Page = searchParams.Page,
                PageSize = searchParams.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)searchParams.PageSize)
            };
        }

        public async Task<FoundTenderDto?> GetByIdAsync(int id)
        {
            var tender = await _context.FoundTenders
                .Include(t => t.FoundByQuery)
                .FirstOrDefaultAsync(t => t.Id == id);

            return tender != null ? MapToDto(tender) : null;
        }

        public async Task<bool> ExistsByExternalIdAsync(string externalId)
        {
            return await _context.FoundTenders
                .AnyAsync(t => t.ExternalId == externalId);
        }

        public async Task<FoundTenderDto> AddAsync(FoundTender tender)
        {
            _context.FoundTenders.Add(tender);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added tender: {PurchaseNumber} (External ID: {ExternalId})", 
                tender.PurchaseNumber, tender.ExternalId);
            
            return MapToDto(tender);
        }

        public async Task<int> AddRangeAsync(IEnumerable<FoundTender> tenders)
        {
            int addedCount = 0;
            
            foreach (var tender in tenders)
            {
                // Check for duplicates
                if (!await ExistsByExternalIdAsync(tender.ExternalId))
                {
                    _context.FoundTenders.Add(tender);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added {Count} new tenders", addedCount);
            }

            return addedCount;
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.FoundTenders.CountAsync();
        }

        private static FoundTenderDto MapToDto(FoundTender tender)
        {
            return new FoundTenderDto
            {
                Id = tender.Id,
                ExternalId = tender.ExternalId,
                PurchaseNumber = tender.PurchaseNumber,
                Title = tender.Title,
                CustomerName = tender.CustomerName,
                PublishDate = tender.PublishDate,
                DirectLinkToSource = tender.DirectLinkToSource,
                FoundByQueryId = tender.FoundByQueryId,
                FoundByQueryKeyword = tender.FoundByQuery?.Keyword,
                SavedAt = tender.SavedAt,
                
                // Новые поля
                ApplicationDeadline = tender.ApplicationDeadline,
                MaxPrice = tender.MaxPrice,
                Region = tender.Region,
                CustomerInn = tender.CustomerInn,
                AdditionalInfo = tender.AdditionalInfo
            };
        }
    }
}
