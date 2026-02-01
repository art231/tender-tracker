using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TenderTracker.API.Models
{
    public class NotificationSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string NotificationType { get; set; } = "email"; // email, telegram, webhook

        [MaxLength(255)]
        public string? EmailAddress { get; set; }

        [MaxLength(100)]
        public string? TelegramChatId { get; set; }

        [MaxLength(500)]
        public string? WebhookUrl { get; set; }

        public bool NotifyOnNewTenders { get; set; } = true;
        public bool NotifyOnDeadlineApproaching { get; set; } = true;
        public bool NotifyOnTechnologyMatch { get; set; } = true;

        public int DeadlineWarningDays { get; set; } = 3;

        [MaxLength(1000)]
        public string? FilterCriteriaJson { get; set; } // JSON с критериями фильтрации для уведомлений

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public Dictionary<string, object>? FilterCriteria
        {
            get => !string.IsNullOrEmpty(FilterCriteriaJson) 
                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(FilterCriteriaJson)
                : new Dictionary<string, object>();
            set => FilterCriteriaJson = value != null 
                ? System.Text.Json.JsonSerializer.Serialize(value)
                : null;
        }
    }
}
