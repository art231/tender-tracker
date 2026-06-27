using System.ComponentModel.DataAnnotations;

namespace TenderTracker.API.DTOs
{
    public class DocumentDownloadTaskDto
    {
        public int Id { get; set; }
        
        [Required]
        public int TenderId { get; set; }
        
        public string? TenderTitle { get; set; }
        public string? PurchaseNumber { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string DocType { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        [Required]
        public string Status { get; set; } = string.Empty;
        
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime? NextRetryAt { get; set; }
        
        [MaxLength(50)]
        public string? Priority { get; set; } = "normal";
        
        public string? SettingsJson { get; set; }
    }

    public class CreateDocumentDownloadTaskDto
    {
        [Required]
        public int TenderId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string DocType { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? Priority { get; set; } = "normal";
    }

    public class UpdateDocumentDownloadTaskDto
    {
        [MaxLength(50)]
        public string? Priority { get; set; }
        
        public string? Status { get; set; }
    }

    public class DocumentDownloadTaskStatsDto
    {
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int FailedTasks { get; set; }
        public int CancelledTasks { get; set; }
        public int TasksWithErrors { get; set; }
        public double AverageCompletionTimeHours { get; set; }
        public double SuccessRate { get; set; }
    }
}
