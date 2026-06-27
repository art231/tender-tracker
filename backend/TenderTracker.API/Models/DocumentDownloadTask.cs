using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TenderTracker.API.Models
{
    public class DocumentDownloadTask
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenderId { get; set; }

        [ForeignKey(nameof(TenderId))]
        public FoundTender Tender { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string DocType { get; set; } = string.Empty; // "notification", "all", "specific_type"

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [Required]
        public TaskStatus Status { get; set; } = TaskStatus.Pending;

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;

        public DateTime? NextRetryAt { get; set; }

        [MaxLength(50)]
        public string? Priority { get; set; } = "normal"; // "high", "normal", "low"

        // Настройки загрузки (сериализованные в JSON)
        [Column(TypeName = "jsonb")]
        public string? SettingsJson { get; set; } = "{}";
    }

    public enum TaskStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4
    }
}
