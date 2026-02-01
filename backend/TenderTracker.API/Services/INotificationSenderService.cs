using TenderTracker.API.DTOs;
using TenderTracker.API.Models;

namespace TenderTracker.API.Services
{
    public interface INotificationSenderService
    {
        Task<bool> SendNewTenderNotificationAsync(NotificationSettingsDto settings, FoundTender tender);
        Task<bool> SendDeadlineApproachingNotificationAsync(NotificationSettingsDto settings, FoundTender tender);
        Task<bool> SendTechnologyMatchNotificationAsync(NotificationSettingsDto settings, FoundTender tender, TechnologyAnalysis analysis);
        Task<bool> SendTestNotificationAsync(NotificationSettingsDto settings, string message);
    }
}
