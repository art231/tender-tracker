using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TenderTracker.API.Models
{
    public class SearchQuery
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Keyword { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Category { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигационное свойство
        public ICollection<FoundTender> FoundTenders { get; set; } = new List<FoundTender>();
    }
}
