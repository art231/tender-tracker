using System;

namespace TenderTracker.API.DTOs
{
    public class SearchQueryDto
    {
        public int Id { get; set; }
        public string Keyword { get; set; } = string.Empty;
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateSearchQueryDto
    {
        public string Keyword { get; set; } = string.Empty;
        public string? Category { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateSearchQueryDto
    {
        public string? Keyword { get; set; }
        public string? Category { get; set; }
        public bool? IsActive { get; set; }
    }
}
