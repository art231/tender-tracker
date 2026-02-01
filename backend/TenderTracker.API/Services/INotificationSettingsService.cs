using TenderTracker.API.DTOs;
using TenderTracker.API.Models;

namespace TenderTracker.API.Services
{
    public interface INotificationSettingsService
    {
        Task<NotificationSettingsDto?> GetByUserIdAsync(int userId);
        Task<NotificationSettingsDto> CreateAsync(CreateNotificationSettingsDto createDto);
        Task<NotificationSettingsDto> UpdateAsync(int userId, UpdateNotificationSettingsDto updateDto);
        Task<bool> DeleteAsync(int userId);
        Task<IEnumerable<NotificationSettingsDto>> GetAllActiveAsync();
        Task<bool> ShouldNotifyForTenderAsync(int userId, FoundTender tender);
    }
}
