using Microsoft.EntityFrameworkCore;
using TenderTracker.API.Data;
using TenderTracker.API.Models;

namespace TenderTracker.API.Tests;

public abstract class IntegrationTestBase : IDisposable
{
    protected ApplicationDbContext DbContext { get; private set; }
    protected DbContextOptions<ApplicationDbContext> DbContextOptions { get; private set; }

    protected IntegrationTestBase()
    {
        // Setup InMemory database
        DbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        DbContext = new ApplicationDbContext(DbContextOptions);
        DbContext.Database.EnsureCreated();
        
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test search queries
        var queries = new List<SearchQuery>
        {
            new SearchQuery { Id = 1, Keyword = "разработка .NET", Category = "IT", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new SearchQuery { Id = 2, Keyword = "строительство", Category = "Строительство", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new SearchQuery { Id = 3, Keyword = "медицинское оборудование", Category = "Медицина", IsActive = false, CreatedAt = DateTime.UtcNow.AddDays(-3) }
        };
        
        DbContext.SearchQueries.AddRange(queries);

        // Add test tenders
        var tenders = new List<FoundTender>
        {
            new FoundTender
            {
                Id = 1,
                ExternalId = "ext-001",
                PurchaseNumber = "1234567890",
                Title = "Разработка веб-приложения на .NET Core",
                CustomerName = "ООО Технологии",
                PublishDate = DateTime.UtcNow.AddDays(-2),
                DirectLinkToSource = "https://zakupki.gov.ru/epz/order/notice/1234567890",
                FoundByQueryId = 1,
                SavedAt = DateTime.UtcNow.AddDays(-1)
            },
            new FoundTender
            {
                Id = 2,
                ExternalId = "ext-002",
                PurchaseNumber = "0987654321",
                Title = "Строительство жилого дома",
                CustomerName = "ГК СтройПроект",
                PublishDate = DateTime.UtcNow.AddDays(-3),
                DirectLinkToSource = "https://zakupki.gov.ru/epz/order/notice/0987654321",
                FoundByQueryId = 2,
                SavedAt = DateTime.UtcNow.AddDays(-2)
            },
            new FoundTender
            {
                Id = 3,
                ExternalId = "ext-003",
                PurchaseNumber = "5555555555",
                Title = "Закупка медицинского оборудования",
                CustomerName = "ГБУЗ Городская больница №1",
                PublishDate = DateTime.UtcNow.AddDays(-1),
                DirectLinkToSource = "https://zakupki.gov.ru/epz/order/notice/5555555555",
                FoundByQueryId = 3,
                SavedAt = DateTime.UtcNow
            },
            new FoundTender
            {
                Id = 4,
                ExternalId = "ext-004",
                PurchaseNumber = "4444444444",
                Title = "Разработка мобильного приложения",
                CustomerName = "ООО МобайлТех",
                PublishDate = DateTime.UtcNow.AddDays(-4),
                DirectLinkToSource = "https://zakupki.gov.ru/epz/order/notice/4444444444",
                FoundByQueryId = 1,
                SavedAt = DateTime.UtcNow.AddDays(-3)
            }
        };
        
        DbContext.FoundTenders.AddRange(tenders);
        DbContext.SaveChanges();
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
