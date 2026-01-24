using Microsoft.EntityFrameworkCore;
using TenderTracker.API.Models;

namespace TenderTracker.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SearchQuery> SearchQueries { get; set; }
        public DbSet<FoundTender> FoundTenders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфигурация для SearchQuery
            modelBuilder.Entity<SearchQuery>(entity =>
            {
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Конфигурация для FoundTender
            modelBuilder.Entity<FoundTender>(entity =>
            {
                entity.HasIndex(e => e.ExternalId).IsUnique();
                entity.HasIndex(e => e.PublishDate);
                entity.HasIndex(e => e.FoundByQueryId);
                entity.HasIndex(e => e.SavedAt);

                // Внешний ключ
                entity.HasOne(e => e.FoundByQuery)
                      .WithMany(q => q.FoundTenders)
                      .HasForeignKey(e => e.FoundByQueryId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
