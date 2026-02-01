using System.ComponentModel.DataAnnotations;

namespace TenderTracker.API.DTOs
{
    public class NotificationSettingsDto
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string NotificationType { get; set; } = "email";

        [EmailAddress]
        [MaxLength(255)]
        public string? EmailAddress { get; set; }

        [MaxLength(100)]
        public string? TelegramChatId { get; set; }

        [Url]
        [MaxLength(500)]
        public string? WebhookUrl { get; set; }

        public bool NotifyOnNewTenders { get; set; } = true;
        public bool NotifyOnDeadlineApproaching { get; set; } = true;
        public bool NotifyOnTechnologyMatch { get; set; } = true;

        [Range(1, 30)]
        public int DeadlineWarningDays { get; set; } = 3;

        public Dictionary<string, object>? FilterCriteria { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateNotificationSettingsDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string NotificationType { get; set; } = "email";

        [EmailAddress]
        [MaxLength(255)]
        public string? EmailAddress { get; set; }

        [MaxLength(100)]
        public string? TelegramChatId { get; set; }

        [Url]
        [MaxLength(500)]
        public string? WebhookUrl { get; set; }

        public bool NotifyOnNewTenders { get; set; } = true;
        public bool NotifyOnDeadlineApproaching { get; set; } = true;
        public bool NotifyOnTechnologyMatch { get; set; } = true;

        [Range(1, 30)]
        public int DeadlineWarningDays { get; set; } = 3;

        public Dictionary<string, object>? FilterCriteria { get; set; }
    }

    public class UpdateNotificationSettingsDto
    {
        [MaxLength(100)]
        public string? NotificationType { get; set; }

        [EmailAddress]
        [MaxLength(255)]
        public string? EmailAddress { get; set; }

        [MaxLength(100)]
        public string? TelegramChatId { get; set; }

        [Url]
        [MaxLength(500)]
        public string? WebhookUrl { get; set; }

        public bool? NotifyOnNewTenders { get; set; }
        public bool? NotifyOnDeadlineApproaching { get; set; }
        public bool? NotifyOnTechnologyMatch { get; set; }

        [Range(1, 30)]
        public int? DeadlineWarningDays { get; set; }

        public Dictionary<string, object>? FilterCriteria { get; set; }
    }
}
