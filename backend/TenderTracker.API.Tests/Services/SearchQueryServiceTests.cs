using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TenderTracker.API.Data;
using TenderTracker.API.DTOs;
using TenderTracker.API.Models;
using TenderTracker.API.Services;
using FluentAssertions;

namespace TenderTracker.API.Tests.Services;

public class SearchQueryServiceTests : IntegrationTestBase
{
    private readonly Mock<ILogger<SearchQueryService>> _loggerMock;
    private readonly SearchQueryService _service;

    public SearchQueryServiceTests()
    {
        _loggerMock = new Mock<ILogger<SearchQueryService>>();
        _service = new SearchQueryService(DbContext, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllQueriesOrderedByCreatedAtDesc()
    {
        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        
        // Check ordering (newest first)
        var queries = result.ToList();
        queries[0].CreatedAt.Should().BeAfter(queries[1].CreatedAt);
        queries[1].CreatedAt.Should().BeAfter(queries[2].CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsQuery()
    {
        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Keyword.Should().Be("разработка .NET");
        result.Category.Should().Be("IT");
        result.IsActive.Should().BeTrue();
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
    public async Task CreateAsync_WithValidData_CreatesQuery()
    {
        // Arrange
        var createDto = new CreateSearchQueryDto
        {
            Keyword = "новый запрос",
            Category = "Тест",
            IsActive = true
        };

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Keyword.Should().Be(createDto.Keyword);
        result.Category.Should().Be(createDto.Category);
        result.IsActive.Should().Be(createDto.IsActive);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify it was saved to database
        var savedQuery = await DbContext.SearchQueries.FindAsync(result.Id);
        savedQuery.Should().NotBeNull();
        savedQuery!.Keyword.Should().Be(createDto.Keyword);
    }

    [Fact]
    public async Task UpdateAsync_WithValidIdAndData_UpdatesQuery()
    {
        // Arrange
        var updateDto = new UpdateSearchQueryDto
        {
            Keyword = "обновленный запрос",
            Category = "Обновленная категория",
            IsActive = false
        };

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Keyword.Should().Be(updateDto.Keyword);
        result.Category.Should().Be(updateDto.Category);
        result.IsActive.Should().Be(updateDto.IsActive!.Value);
        
        // Verify it was updated in database
        var updatedQuery = await DbContext.SearchQueries.FindAsync(1);
        updatedQuery.Should().NotBeNull();
        updatedQuery!.Keyword.Should().Be(updateDto.Keyword);
        updatedQuery.Category.Should().Be(updateDto.Category);
        updatedQuery.IsActive.Should().Be(updateDto.IsActive.Value);
    }

    [Fact]
    public async Task UpdateAsync_WithPartialData_UpdatesOnlySpecifiedFields()
    {
        // Arrange
        var originalQuery = await DbContext.SearchQueries.FindAsync(1);
        originalQuery.Should().NotBeNull();
        
        var updateDto = new UpdateSearchQueryDto
        {
            Keyword = "только ключевое слово обновлено"
            // Category and IsActive are null, so they should remain unchanged
        };

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Keyword.Should().Be(updateDto.Keyword);
        result.Category.Should().Be(originalQuery!.Category); // Should remain unchanged
        result.IsActive.Should().Be(originalQuery.IsActive); // Should remain unchanged
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateSearchQueryDto
        {
            Keyword = "обновленный запрос"
        };

        // Act
        var result = await _service.UpdateAsync(999, updateDto);

        // Assert
        result.Should().BeNull();
        
        // Verify no changes were made
        var unchangedQuery = await DbContext.SearchQueries.FindAsync(1);
        unchangedQuery.Should().NotBeNull();
        unchangedQuery!.Keyword.Should().Be("разработка .NET"); // Original value
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_DeletesQuery()
    {
        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();
        
        // Verify it was deleted from database
        var deletedQuery = await DbContext.SearchQueries.FindAsync(1);
        deletedQuery.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveQueriesAsync_ReturnsOnlyActiveQueries()
    {
        // Act
        var result = await _service.GetActiveQueriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Only 2 active queries in test data
        result.All(q => q.IsActive).Should().BeTrue();
        
        // Check ordering (by keyword ascending)
        var queries = result.ToList();
        queries[0].Keyword.Should().Be("разработка .NET");
        queries[1].Keyword.Should().Be("строительство");
    }

    [Fact]
    public async Task CreateAsync_LogsInformation()
    {
        // Arrange
        var createDto = new CreateSearchQueryDto
        {
            Keyword = "тестовый запрос",
            Category = "Тест",
            IsActive = true
        };

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created search query") && v.ToString()!.Contains(createDto.Keyword)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_LogsInformation()
    {
        // Arrange
        var updateDto = new UpdateSearchQueryDto
        {
            Keyword = "обновленный запрос"
        };

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated search query ID: 1")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_LogsInformation()
    {
        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleted search query ID: 1")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}
