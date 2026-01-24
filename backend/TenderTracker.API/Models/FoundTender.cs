using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TenderTracker.API.Models
{
    public class FoundTender
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string ExternalId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string PurchaseNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? CustomerName { get; set; }

        public DateTime? PublishDate { get; set; }

        [MaxLength(1000)]
        public string? DirectLinkToSource { get; set; }

        public int? FoundByQueryId { get; set; }

        [ForeignKey("FoundByQueryId")]
        public SearchQuery? FoundByQuery { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
