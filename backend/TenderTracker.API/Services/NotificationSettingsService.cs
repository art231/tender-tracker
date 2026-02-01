using Microsoft.EntityFrameworkCore;
using TenderTracker.API.Data;
using TenderTracker.API.DTOs;
using TenderTracker.API.Models;

namespace TenderTracker.API.Services
{
    public class NotificationSettingsService : INotificationSettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationSettingsService> _logger;

        public NotificationSettingsService(
            ApplicationDbContext context,
            ILogger<NotificationSettingsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<NotificationSettingsDto?> GetByUserIdAsync(int userId)
        {
            try
            {
                var settings = await _context.NotificationSettings
                    .FirstOrDefaultAsync(ns => ns.UserId == userId);

                if (settings == null)
                    return null;

                return MapToDto(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification settings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<NotificationSettingsDto> CreateAsync(CreateNotificationSettingsDto createDto)
        {
            try
            {
                // Check if settings already exist for this user
                var existing = await _context.NotificationSettings
                    .FirstOrDefaultAsync(ns => ns.UserId == createDto.UserId);

                if (existing != null)
                {
                    throw new InvalidOperationException($"Notification settings already exist for user {createDto.UserId}");
                }

                var settings = new NotificationSettings
                {
                    UserId = createDto.UserId,
                    NotificationType = createDto.NotificationType,
                    EmailAddress = createDto.EmailAddress,
                    TelegramChatId = createDto.TelegramChatId,
                    WebhookUrl = createDto.WebhookUrl,
                    NotifyOnNewTenders = createDto.NotifyOnNewTenders,
                    NotifyOnDeadlineApproaching = createDto.NotifyOnDeadlineApproaching,
                    NotifyOnTechnologyMatch = createDto.NotifyOnTechnologyMatch,
                    DeadlineWarningDays = createDto.DeadlineWarningDays,
                    FilterCriteria = createDto.FilterCriteria,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.NotificationSettings.Add(settings);
                await _context.SaveChangesAsync();

                return MapToDto(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification settings for user {UserId}", createDto.UserId);
                throw;
            }
        }

        public async Task<NotificationSettingsDto> UpdateAsync(int userId, UpdateNotificationSettingsDto updateDto)
        {
            try
            {
                var settings = await _context.NotificationSettings
                    .FirstOrDefaultAsync(ns => ns.UserId == userId);

                if (settings == null)
                {
                    throw new KeyNotFoundException($"Notification settings not found for user {userId}");
                }

                // Update only provided fields
                if (updateDto.NotificationType != null)
                    settings.NotificationType = updateDto.NotificationType;
                
                if (updateDto.EmailAddress != null)
                    settings.EmailAddress = updateDto.EmailAddress;
                
                if (updateDto.TelegramChatId != null)
                    settings.TelegramChatId = updateDto.TelegramChatId;
                
                if (updateDto.WebhookUrl != null)
                    settings.WebhookUrl = updateDto.WebhookUrl;
                
                if (updateDto.NotifyOnNewTenders.HasValue)
                    settings.NotifyOnNewTenders = updateDto.NotifyOnNewTenders.Value;
                
                if (updateDto.NotifyOnDeadlineApproaching.HasValue)
                    settings.NotifyOnDeadlineApproaching = updateDto.NotifyOnDeadlineApproaching.Value;
                
                if (updateDto.NotifyOnTechnologyMatch.HasValue)
                    settings.NotifyOnTechnologyMatch = updateDto.NotifyOnTechnologyMatch.Value;
                
                if (updateDto.DeadlineWarningDays.HasValue)
                    settings.DeadlineWarningDays = updateDto.DeadlineWarningDays.Value;
                
                if (updateDto.FilterCriteria != null)
                    settings.FilterCriteria = updateDto.FilterCriteria;

                settings.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return MapToDto(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification settings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int userId)
        {
            try
            {
                var settings = await _context.NotificationSettings
                    .FirstOrDefaultAsync(ns => ns.UserId == userId);

                if (settings == null)
                    return false;

                _context.NotificationSettings.Remove(settings);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification settings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<NotificationSettingsDto>> GetAllActiveAsync()
        {
            try
            {
                var settings = await _context.NotificationSettings
                    .Where(ns => ns.NotifyOnNewTenders || ns.NotifyOnDeadlineApproaching || ns.NotifyOnTechnologyMatch)
                    .ToListAsync();

                return settings.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all active notification settings");
                throw;
            }
        }

        public async Task<bool> ShouldNotifyForTenderAsync(int userId, FoundTender tender)
        {
            try
            {
                var settings = await GetByUserIdAsync(userId);
                if (settings == null)
                    return false;

                // Check if user wants notifications for new tenders
                if (!settings.NotifyOnNewTenders)
                    return false;

                // Apply filter criteria if specified
                if (settings.FilterCriteria != null && settings.FilterCriteria.Any())
                {
                    return CheckFilterCriteria(tender, settings.FilterCriteria);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if should notify user {UserId} for tender {TenderId}", userId, tender.Id);
                return false;
            }
        }

        private bool CheckFilterCriteria(FoundTender tender, Dictionary<string, object> filterCriteria)
        {
            foreach (var criterion in filterCriteria)
            {
                switch (criterion.Key.ToLower())
                {
                    case "minprice":
                        if (tender.MaxPrice.HasValue && tender.MaxPrice.Value < Convert.ToDecimal(criterion.Value))
                            return false;
                        break;
                    
                    case "maxprice":
                        if (tender.MaxPrice.HasValue && tender.MaxPrice.Value > Convert.ToDecimal(criterion.Value))
                            return false;
                        break;
                    
                    case "region":
                        if (tender.Region != null && !tender.Region.Contains(criterion.Value.ToString() ?? ""))
                            return false;
                        break;
                    
                    case "customerinn":
                        if (tender.CustomerInn != null && !tender.CustomerInn.Contains(criterion.Value.ToString() ?? ""))
                            return false;
                        break;
                    
                    case "purchasetype":
                        // Simple check for purchase type in title or additional info
                        var purchaseType = criterion.Value.ToString()?.ToLower();
                        var title = tender.Title?.ToLower() ?? "";
                        var additionalInfo = tender.AdditionalInfo?.ToLower() ?? "";
                        
                        if (!title.Contains(purchaseType ?? "") && !additionalInfo.Contains(purchaseType ?? ""))
                            return false;
                        break;
                }
            }

            return true;
        }

        private NotificationSettingsDto MapToDto(NotificationSettings settings)
        {
            return new NotificationSettingsDto
            {
                Id = settings.Id,
                UserId = settings.UserId,
                NotificationType = settings.NotificationType,
                EmailAddress = settings.EmailAddress,
                TelegramChatId = settings.TelegramChatId,
                WebhookUrl = settings.WebhookUrl,
                NotifyOnNewTenders = settings.NotifyOnNewTenders,
                NotifyOnDeadlineApproaching = settings.NotifyOnDeadlineApproaching,
                NotifyOnTechnologyMatch = settings.NotifyOnTechnologyMatch,
                DeadlineWarningDays = settings.DeadlineWarningDays,
                FilterCriteria = settings.FilterCriteria,
                CreatedAt = settings.CreatedAt,
                UpdatedAt = settings.UpdatedAt
            };
        }
    }
}
