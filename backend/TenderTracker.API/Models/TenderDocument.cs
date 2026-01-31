using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TenderTracker.API.Models
{
    public class TenderDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenderId { get; set; }

        [ForeignKey(nameof(TenderId))]
        public FoundTender Tender { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string DocType { get; set; } = string.Empty; // "notification", "protocol", "clarification", etc.

        public DateTime? PublishedAt { get; set; }

        [Required]
        public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "jsonb")]
        public string SourceJson { get; set; } = string.Empty; // Оригинальный JSON документа

        [MaxLength(500)]
        public string? FilePath { get; set; } // Путь к экспортированному файлу (PDF/DOCX)

        // Навигационное свойство для анализа
        public TechnologyAnalysis? TechnologyAnalysis { get; set; }
    }
}
