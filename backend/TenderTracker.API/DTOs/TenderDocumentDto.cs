using System.Text.Json;

namespace TenderTracker.API.DTOs
{
    public class TenderDocumentDto
    {
        public int Id { get; set; }
        public int TenderId { get; set; }
        public string DocType { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public DateTime DownloadedAt { get; set; }
        public JsonElement? SourceJson { get; set; }
        public string? FilePath { get; set; }
        public int? TechnologyAnalysisId { get; set; }
    }

    public class DownloadDocumentRequest
    {
        public string DocType { get; set; } = string.Empty;
    }

    public class ExportDocumentRequest
    {
        public string Format { get; set; } = string.Empty; // "pdf" или "docx"
    }
}
