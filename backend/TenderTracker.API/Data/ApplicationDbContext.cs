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
        public DbSet<TenderDocument> TenderDocuments { get; set; }
        public DbSet<TechnologyAnalysis> TechnologyAnalyses { get; set; }
        public DbSet<NotificationSettings> NotificationSettings { get; set; }

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

            // Конфигурация для TenderDocument
            modelBuilder.Entity<TenderDocument>(entity =>
            {
                entity.HasIndex(e => e.TenderId);
                entity.HasIndex(e => e.DocType);
                entity.HasIndex(e => e.PublishedAt);
                entity.HasIndex(e => e.DownloadedAt);

                // Внешний ключ к FoundTender
                entity.HasOne(e => e.Tender)
                      .WithMany(t => t.Documents)
                      .HasForeignKey(e => e.TenderId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Связь один-к-одному с TechnologyAnalysis
                entity.HasOne(e => e.TechnologyAnalysis)
                      .WithOne(a => a.Document)
                      .HasForeignKey<TechnologyAnalysis>(a => a.DocumentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Конфигурация для TechnologyAnalysis
            modelBuilder.Entity<TechnologyAnalysis>(entity =>
            {
                entity.HasIndex(e => e.TenderId);
                entity.HasIndex(e => e.MatchScore);
                entity.HasIndex(e => e.IsCompatible);
                entity.HasIndex(e => e.AnalyzedAt);
                entity.HasIndex(e => e.ManuallyVerified);

                // Внешний ключ к FoundTender
                entity.HasOne(e => e.Tender)
                      .WithMany(t => t.TechnologyAnalyses)
                      .HasForeignKey(e => e.TenderId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Внешний ключ к TenderDocument (опционально)
                entity.HasOne(e => e.Document)
                      .WithOne(d => d.TechnologyAnalysis)
                      .HasForeignKey<TechnologyAnalysis>(e => e.DocumentId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Конфигурация для NotificationSettings
            modelBuilder.Entity<NotificationSettings>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.NotificationType);
                entity.HasIndex(e => e.NotifyOnNewTenders);
                entity.HasIndex(e => e.NotifyOnDeadlineApproaching);
                entity.HasIndex(e => e.NotifyOnTechnologyMatch);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.UpdatedAt);
            });
        }
    }
}
