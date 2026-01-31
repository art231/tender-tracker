using System;
using System.Text.Json;

namespace TenderTracker.API.DTOs
{
    public class TenderDetailsDto
    {
        public int Id { get; set; }
        public string ExternalId { get; set; } = string.Empty;
        public string PurchaseNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public DateTime? PublishDate { get; set; }
        public string? DirectLinkToSource { get; set; }
        public int? FoundByQueryId { get; set; }
        public string? FoundByQueryKeyword { get; set; }
        public DateTime SavedAt { get; set; }
        
        // Дополнительные поля
        public DateTime? ApplicationDeadline { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Region { get; set; }
        public string? CustomerInn { get; set; }
        public string? AdditionalInfo { get; set; }
        
        // Документы
        public List<TenderDocumentDto> Documents { get; set; } = new List<TenderDocumentDto>();
        
        // Технологический анализ
        public List<TechnologyAnalysisDto> TechnologyAnalyses { get; set; } = new List<TechnologyAnalysisDto>();
        
        // Статистика
        public TenderDetailsStatsDto Stats { get; set; } = new TenderDetailsStatsDto();
    }
    
    public class TenderDetailsStatsDto
    {
        public int DocumentsCount { get; set; }
        public int AnalysesCount { get; set; }
        public bool HasNotification { get; set; }
        public bool HasProtocols { get; set; }
        public DateTime? LastDocumentDate { get; set; }
        public DateTime? LastAnalysisDate { get; set; }
    }
}
