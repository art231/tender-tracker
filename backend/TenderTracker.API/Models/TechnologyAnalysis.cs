using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TenderTracker.API.Models
{
    public class TechnologyAnalysis
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TenderId { get; set; }

        [ForeignKey(nameof(TenderId))]
        public FoundTender Tender { get; set; } = null!;

        [Required]
        [Range(0, 100)]
        public int MatchScore { get; set; } // 0-100%

        [Required]
        [Column(TypeName = "jsonb")]
        public string MatchedTechnologiesJson { get; set; } = "[]"; // JSON: [{ "technology": ".NET", "count": 5, "mentions": ["..."] }, ...]

        [Required]
        public bool IsCompatible { get; set; }

        [Required]
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(2000)]
        public string? AnalysisNotes { get; set; } // Для ручной проверки и комментариев

        [Required]
        public bool ManuallyVerified { get; set; } = false;

        // Навигационное свойство для документа (опционально, если анализ привязан к конкретному документу)
        public int? DocumentId { get; set; }

        [ForeignKey(nameof(DocumentId))]
        public TenderDocument? Document { get; set; }
    }
}
