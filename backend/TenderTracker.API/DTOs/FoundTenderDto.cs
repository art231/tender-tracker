using System;

namespace TenderTracker.API.DTOs
{
    public class FoundTenderDto
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
    }

    public class FoundTenderResponse
    {
        public List<FoundTenderDto> Tenders { get; set; } = new List<FoundTenderDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class TenderSearchParams
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public int? QueryId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SortBy { get; set; } = "SavedAt";
        public bool SortDescending { get; set; } = true;
    }
}
