using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TenderTracker.API.Data;
using TenderTracker.API.DTOs;
using TenderTracker.API.Models;
using TenderTracker.API.Services;
using FluentAssertions;

namespace TenderTracker.API.Tests.Services;

public class FoundTenderServiceTests : IntegrationTestBase
{
    private readonly Mock<ILogger<FoundTenderService>> _loggerMock;
    private readonly FoundTenderService _service;

    public FoundTenderServiceTests()
    {
        _loggerMock = new Mock<ILogger<FoundTenderService>>();
        _service = new FoundTenderService(DbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task GetTendersAsync_WithNoFilters_ReturnsAllTenders()
    {
        // Arrange
        var searchParams = new TenderSearchParams
        {
            Page = 1,
            PageSize = 20
        };

        // Act
        var result = await _service.GetTendersAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Tenders.Should().HaveCount(4);
        result.TotalCount.Should().Be(4);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetTendersAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var searchParams = new TenderSearchParams
        {
            Page = 1,
            PageSize = 2
        };

        // Act
        var result = await _service.GetTendersAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Tenders.Should().HaveCount(2);
        result.TotalCount.Should().Be(4);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetTendersAsync_WithSearchFilter_ReturnsFilteredResults()
    {
        // Arrange
        var searchParams = new TenderSearchParams
        {
            Page = 1,
            PageSize = 20,
            Search = "строительство"
        };

        // Act
        var result = await _service.GetTendersAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Tenders.Should().HaveCount(1);
        result.Tenders[0].Title.Should().Contain("Строительство");
    }

    [Fact]
    public async Task GetTendersAsync_WithQueryIdFilter_ReturnsTendersForQuery()
    {
        // Arrange
        var searchParams = new TenderSearchParams
        {
            Page = 1,
            PageSize = 20,
            QueryId = 1
        };

        // Act
        var result = await _service.GetTendersAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Tenders.Should().HaveCount(2); // Two tenders with FoundByQueryId = 1
        result.Tenders.All(t => t.FoundByQueryId == 1).Should().BeTrue();
    }

    [Fact]
    public async Task GetTendersAsync_WithDateFilter_ReturnsTendersInDateRange()
    {
        // Arrange
        var searchParams = new TenderSearchParams
        {
            Page = 1,
            PageSize = 20,
            FromDate = DateTime.UtcNow.AddDays(-2),
            ToDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GetTendersAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Tenders.Should().HaveCount(2); // Tenders saved in the last 2 days
        result.Tenders.All(t => t.SavedAt >= DateTime.UtcNow.AddDays(-2)).Should().BeTrue();
    }

    [Fact]
    public async Task GetTendersAsync_WithSorting_ReturnsSortedResults()
    {
        // Arrange
        var searchParams = new TenderSearchParams
        {
            Page = 1,
            PageSize = 20,
            SortBy = "Title",
            SortDescending = false // Ascending
        };

        // Act
        var result = await _service.GetTendersAsync(searchParams);

        // Assert
        result.Should().NotBeNull();
        result.Tenders.Should().HaveCount(4);
        
        // Check if titles are sorted ascending
        var titles = result.Tenders.Select(t => t.Title).ToList();
        var sortedTitles = titles.OrderBy(t => t).ToList();
        titles.Should().BeEquivalentTo(sortedTitles, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsTender()
    {
        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.PurchaseNumber.Should().Be("1234567890");
        result.Title.Should().Be("Разработка веб-приложения на .NET Core");
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsByExternalIdAsync_WithExistingId_ReturnsTrue()
    {
        // Act
        var result = await _service.ExistsByExternalIdAsync("ext-001");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByExternalIdAsync_WithNonExistingId_ReturnsFalse()
    {
        // Act
        var result = await _service.ExistsByExternalIdAsync("non-existing");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_WithNewTender_AddsToDatabase()
    {
        // Arrange
        var newTender = new FoundTender
        {
            ExternalId = "ext-005",
            PurchaseNumber = "9999999999",
            Title = "Новый тендер",
            CustomerName = "Новый заказчик",
            PublishDate = DateTime.UtcNow,
            DirectLinkToSource = "https://zakupki.gov.ru/epz/order/notice/9999999999",
            FoundByQueryId = 2,
            SavedAt = DateTime.UtcNow
        };

        // Act
        var result = await _service.AddAsync(newTender);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.PurchaseNumber.Should().Be(newTender.PurchaseNumber);
        
        // Verify it was saved to database
        var savedTender = await DbContext.FoundTenders.FindAsync(result.Id);
        savedTender.Should().NotBeNull();
        savedTender!.PurchaseNumber.Should().Be(newTender.PurchaseNumber);
    }

    [Fact]
    public async Task AddRangeAsync_WithNewTenders_AddsOnlyNonDuplicates()
    {
        // Arrange
        var newTenders = new List<FoundTender>
        {
            new FoundTender
            {
                ExternalId = "ext-005", // New
                PurchaseNumber = "9999999999",
                Title = "Новый тендер 1",
                CustomerName = "Заказчик 1",
                PublishDate = DateTime.UtcNow,
                DirectLinkToSource = "https://zakupki.gov.ru/epz/order/notice/9999999999",
                FoundByQueryId = 1,
                SavedAt = DateTime.UtcNow
            },
            new FoundTender
            {
                ExternalId = "ext-001", // Already exists
                PurchaseNumber = "1234567890",
                Title = "Дубликат",
                CustomerName = "Заказчик 2",
                PublishDate = DateTime.UtcNow,
                DirectLinkToSource = "https://zakupki.gov.ru/epz/order/notice/1234567890",
                FoundByQueryId = 1,
                SavedAt = DateTime.UtcNow
            },
            new FoundTender
            {
                ExternalId = "ext-006", // New
                PurchaseNumber = "8888888888",
                Title = "Новый тендер 2",
                CustomerName = "Заказчик 3",
                PublishDate = DateTime.UtcNow,
                DirectLinkToSource = "https://zakupki.gov.ru/epz/order/notice/8888888888",
                FoundByQueryId = 2,
                SavedAt = DateTime.UtcNow
            }
        };

        // Act
        var addedCount = await _service.AddRangeAsync(newTenders);

        // Assert
        addedCount.Should().Be(2); // Only 2 new tenders should be added
        
        // Verify new tenders were added
        var newTender1 = await DbContext.FoundTenders.FirstOrDefaultAsync(t => t.ExternalId == "ext-005");
        newTender1.Should().NotBeNull();
        
        var newTender2 = await DbContext.FoundTenders.FirstOrDefaultAsync(t => t.ExternalId == "ext-006");
        newTender2.Should().NotBeNull();
        
        // Verify duplicate was not added (should still have original title)
        var existingTender = await DbContext.FoundTenders.FirstOrDefaultAsync(t => t.ExternalId == "ext-001");
        existingTender.Should().NotBeNull();
        existingTender!.Title.Should().Be("Разработка веб-приложения на .NET Core"); // Original title
    }

    [Fact]
    public async Task GetTotalCountAsync_ReturnsCorrectCount()
    {
        // Act
        var count = await _service.GetTotalCountAsync();

        // Assert
        count.Should().Be(4);
    }

    [Fact]
    public async Task MapToDto_IncludesQueryKeyword()
    {
        // Act
        var tender = await _service.GetByIdAsync(1);

        // Assert
        tender.Should().NotBeNull();
        tender!.FoundByQueryKeyword.Should().Be("разработка .NET");
    }
}
