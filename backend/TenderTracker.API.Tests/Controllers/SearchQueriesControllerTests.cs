using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TenderTracker.API.Controllers;
using TenderTracker.API.DTOs;
using TenderTracker.API.Services;
using FluentAssertions;

namespace TenderTracker.API.Tests.Controllers;

public class SearchQueriesControllerTests : IntegrationTestBase
{
    private readonly Mock<ISearchQueryService> _searchQueryServiceMock;
    private readonly Mock<ILogger<SearchQueriesController>> _loggerMock;
    private readonly SearchQueriesController _controller;

    public SearchQueriesControllerTests()
    {
        _searchQueryServiceMock = new Mock<ISearchQueryService>();
        _loggerMock = new Mock<ILogger<SearchQueriesController>>();
        _controller = new SearchQueriesController(_searchQueryServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetSearchQueries_ReturnsAllQueries()
    {
        // Arrange
        var expectedQueries = new List<SearchQueryDto>
        {
            new SearchQueryDto { Id = 1, Keyword = "разработка .NET", Category = "IT", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new SearchQueryDto { Id = 2, Keyword = "строительство", Category = "Строительство", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-5) }
        };

        _searchQueryServiceMock
            .Setup(s => s.GetAllAsync())
            .ReturnsAsync(expectedQueries);

        // Act
        var result = await _controller.GetSearchQueries();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        
        var queries = okResult.Value as IEnumerable<SearchQueryDto>;
        queries.Should().NotBeNull();
        queries!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSearchQuery_WithValidId_ReturnsQuery()
    {
        // Arrange
        var queryId = 1;
        var expectedQuery = new SearchQueryDto
        {
            Id = queryId,
            Keyword = "разработка .NET",
            Category = "IT",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        _searchQueryServiceMock
            .Setup(s => s.GetByIdAsync(queryId))
            .ReturnsAsync(expectedQuery);

        // Act
        var result = await _controller.GetSearchQuery(queryId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        
        var query = okResult.Value as SearchQueryDto;
        query.Should().NotBeNull();
        query!.Id.Should().Be(queryId);
        query.Keyword.Should().Be("разработка .NET");
    }

    [Fact]
    public async Task GetSearchQuery_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var queryId = 999;
        _searchQueryServiceMock
            .Setup(s => s.GetByIdAsync(queryId))
            .ReturnsAsync((SearchQueryDto?)null);

        // Act
        var result = await _controller.GetSearchQuery(queryId);

        // Assert
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
        notFoundResult.Value.Should().Be($"Search query with ID {queryId} not found");
    }

    [Fact]
    public async Task CreateSearchQuery_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateSearchQueryDto
        {
            Keyword = "новый запрос",
            Category = "Тест",
            IsActive = true
        };

        var createdQuery = new SearchQueryDto
        {
            Id = 10,
            Keyword = createDto.Keyword,
            Category = createDto.Category,
            IsActive = createDto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _searchQueryServiceMock
            .Setup(s => s.CreateAsync(createDto))
            .ReturnsAsync(createdQuery);

        // Act
        var result = await _controller.CreateSearchQuery(createDto);

        // Assert
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(SearchQueriesController.GetSearchQuery));
        createdResult.RouteValues!["id"].Should().Be(10);
        
        var query = createdResult.Value as SearchQueryDto;
        query.Should().NotBeNull();
        query!.Keyword.Should().Be(createDto.Keyword);
    }

    [Fact]
    public async Task CreateSearchQuery_WithEmptyKeyword_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateSearchQueryDto
        {
            Keyword = "",
            Category = "Тест",
            IsActive = true
        };

        // Act
        var result = await _controller.CreateSearchQuery(createDto);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Keyword is required");
    }

    [Fact]
    public async Task UpdateSearchQuery_WithValidData_ReturnsUpdatedQuery()
    {
        // Arrange
        var queryId = 1;
        var updateDto = new UpdateSearchQueryDto
        {
            Keyword = "обновленный запрос",
            Category = "Обновленная категория",
            IsActive = false
        };

        var updatedQuery = new SearchQueryDto
        {
            Id = queryId,
            Keyword = updateDto.Keyword,
            Category = updateDto.Category,
            IsActive = updateDto.IsActive!.Value, // Use .Value for nullable bool
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        _searchQueryServiceMock
            .Setup(s => s.UpdateAsync(queryId, updateDto))
            .ReturnsAsync(updatedQuery);

        // Act
        var result = await _controller.UpdateSearchQuery(queryId, updateDto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        
        var query = okResult.Value as SearchQueryDto;
        query.Should().NotBeNull();
        query!.Keyword.Should().Be(updateDto.Keyword);
        query.Category.Should().Be(updateDto.Category);
        query.IsActive.Should().Be(updateDto.IsActive!.Value);
    }

    [Fact]
    public async Task UpdateSearchQuery_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var queryId = 999;
        var updateDto = new UpdateSearchQueryDto
        {
            Keyword = "обновленный запрос",
            Category = "Обновленная категория",
            IsActive = false
        };

        _searchQueryServiceMock
            .Setup(s => s.UpdateAsync(queryId, updateDto))
            .ReturnsAsync((SearchQueryDto?)null);

        // Act
        var result = await _controller.UpdateSearchQuery(queryId, updateDto);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
        notFoundResult.Value.Should().Be($"Search query with ID {queryId} not found");
    }

    [Fact]
    public async Task DeleteSearchQuery_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var queryId = 1;
        _searchQueryServiceMock
            .Setup(s => s.DeleteAsync(queryId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteSearchQuery(queryId);

        // Assert
        var noContentResult = result as NoContentResult;
        noContentResult.Should().NotBeNull();
        noContentResult!.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task DeleteSearchQuery_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var queryId = 999;
        _searchQueryServiceMock
            .Setup(s => s.DeleteAsync(queryId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteSearchQuery(queryId);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
        notFoundResult.Value.Should().Be($"Search query with ID {queryId} not found");
    }

    [Fact]
    public async Task GetActiveQueries_ReturnsOnlyActiveQueries()
    {
        // Arrange
        var expectedQueries = new List<SearchQueryDto>
        {
            new SearchQueryDto { Id = 1, Keyword = "разработка .NET", Category = "IT", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new SearchQueryDto { Id = 2, Keyword = "строительство", Category = "Строительство", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-5) }
        };

        _searchQueryServiceMock
            .Setup(s => s.GetActiveQueriesAsync())
            .ReturnsAsync(expectedQueries);

        // Act
        var result = await _controller.GetActiveQueries();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        
        var queries = okResult.Value as IEnumerable<SearchQueryDto>;
        queries.Should().NotBeNull();
        queries!.Should().HaveCount(2);
        queries.All(q => q.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task CreateSearchQuery_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var createDto = new CreateSearchQueryDto
        {
            Keyword = "новый запрос",
            Category = "Тест",
            IsActive = true
        };

        _searchQueryServiceMock
            .Setup(s => s.CreateAsync(createDto))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.CreateSearchQuery(createDto);

        // Assert
        var statusCodeResult = result.Result as ObjectResult;
        statusCodeResult.Should().NotBeNull();
        statusCodeResult!.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("Internal server error");
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}
