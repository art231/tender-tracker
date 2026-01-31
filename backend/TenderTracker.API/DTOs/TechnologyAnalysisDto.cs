using System;
using System.Text.Json;

namespace TenderTracker.API.DTOs
{
    public class TechnologyAnalysisDto
    {
        public int Id { get; set; }
        public int TenderId { get; set; }
        public int MatchScore { get; set; } // 0-100%
        public JsonElement MatchedTechnologies { get; set; } // JSON: [{ "technology": ".NET", "count": 5, "mentions": ["..."] }, ...]
        public bool IsCompatible { get; set; }
        public DateTime AnalyzedAt { get; set; }
        public string? AnalysisNotes { get; set; }
        public bool ManuallyVerified { get; set; }
        public int? DocumentId { get; set; }
        
        // Дополнительные вычисляемые поля
        public string CompatibilityStatus { get; set; } = string.Empty;
        public int TechnologiesCount { get; set; }
        public DateTime? DocumentPublishedAt { get; set; }
        public string? DocumentType { get; set; }
    }
    
    public class MatchedTechnologyDto
    {
        public string Technology { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<string> Mentions { get; set; } = new List<string>();
        public double RelevanceScore { get; set; } // 0-1
    }
}
