using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TenderTracker.API.Controllers;
using TenderTracker.API.DTOs;
using TenderTracker.API.Services;
using FluentAssertions;

namespace TenderTracker.API.Tests.Controllers;

public class FoundTendersControllerTests : IntegrationTestBase
{
    private readonly Mock<IFoundTenderService> _foundTenderServiceMock;
    private readonly Mock<ILogger<FoundTendersController>> _loggerMock;
    private readonly FoundTendersController _controller;

    public FoundTendersControllerTests()
    {
        _foundTenderServiceMock = new Mock<IFoundTenderService>();
        _loggerMock = new Mock<ILogger<FoundTendersController>>();
        _controller = new FoundTendersController(_foundTenderServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetFoundTenders_WithValidParams_ReturnsOkResult()
    {
        // Arrange
        var expectedResponse = new FoundTenderResponse
        {
            Tenders = new List<FoundTenderDto>
            {
                new FoundTenderDto
                {
                    Id = 1,
                    PurchaseNumber = "1234567890",
                    Title = "Test Tender",
                    CustomerName = "Test Customer",
                    PublishDate = DateTime.UtcNow.AddDays(-1),
                    SavedAt = DateTime.UtcNow
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20,
            TotalPages = 1
        };

        _foundTenderServiceMock
            .Setup(s => s.GetTendersAsync(It.IsAny<TenderSearchParams>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetFoundTenders(page: 1, pageSize: 20);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        
        var response = okResult.Value as FoundTenderResponse;
        response.Should().NotBeNull();
        response!.Tenders.Should().HaveCount(1);
        response.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetFoundTenders_WithInvalidPage_ReturnsFirstPage()
    {
        // Arrange
        var expectedResponse = new FoundTenderResponse
        {
            Tenders = new List<FoundTenderDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20,
            TotalPages = 0
        };

        _foundTenderServiceMock
            .Setup(s => s.GetTendersAsync(It.Is<TenderSearchParams>(p => p.Page == 1)))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetFoundTenders(page: 0, pageSize: 20);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        
        _foundTenderServiceMock.Verify(
            s => s.GetTendersAsync(It.Is<TenderSearchParams>(p => p.Page == 1)),
            Times.Once);
    }

    [Fact]
    public async Task GetFoundTenders_WithSearchFilter_ReturnsFilteredResults()
    {
        // Arrange
        var searchTerm = "строительство";
        var expectedResponse = new FoundTenderResponse
        {
            Tenders = new List<FoundTenderDto>
            {
                new FoundTenderDto
                {
                    Id = 2,
                    PurchaseNumber = "0987654321",
                    Title = "Строительство жилого дома",
                    CustomerName = "ГК СтройПроект",
                    PublishDate = DateTime.UtcNow.AddDays(-3),
                    SavedAt = DateTime.UtcNow.AddDays(-2)
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20,
            TotalPages = 1
        };

        _foundTenderServiceMock
            .Setup(s => s.GetTendersAsync(It.Is<TenderSearchParams>(p => p.Search == searchTerm)))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetFoundTenders(search: searchTerm);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        
        var response = okResult!.Value as FoundTenderResponse;
        response!.Tenders.Should().HaveCount(1);
        response.Tenders[0].Title.Should().Contain("Строительство");
    }

    [Fact]
    public async Task GetFoundTender_WithValidId_ReturnsTender()
    {
        // Arrange
        var tenderId = 1;
        var expectedTender = new FoundTenderDto
        {
            Id = tenderId,
            PurchaseNumber = "1234567890",
            Title = "Test Tender",
            CustomerName = "Test Customer",
            PublishDate = DateTime.UtcNow.AddDays(-1),
            SavedAt = DateTime.UtcNow
        };

        _foundTenderServiceMock
            .Setup(s => s.GetByIdAsync(tenderId))
            .ReturnsAsync(expectedTender);

        // Act
        var result = await _controller.GetFoundTender(tenderId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        
        var tender = okResult.Value as FoundTenderDto;
        tender.Should().NotBeNull();
        tender!.Id.Should().Be(tenderId);
    }

    [Fact]
    public async Task GetFoundTender_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var tenderId = 999;
        _foundTenderServiceMock
            .Setup(s => s.GetByIdAsync(tenderId))
            .ReturnsAsync((FoundTenderDto?)null);

        // Act
        var result = await _controller.GetFoundTender(tenderId);

        // Assert
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
        notFoundResult.Value.Should().Be($"Tender with ID {tenderId} not found");
    }

    [Fact]
    public async Task GetTenderCount_ReturnsCorrectCount()
    {
        // Arrange
        var expectedCount = 4;
        _foundTenderServiceMock
            .Setup(s => s.GetTotalCountAsync())
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.GetTenderCount();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        
        var count = okResult.Value as int?;
        count.Should().Be(expectedCount);
    }

    [Fact]
    public async Task GetStats_ReturnsStatistics()
    {
        // Arrange
        var expectedCount = 4;
        _foundTenderServiceMock
            .Setup(s => s.GetTotalCountAsync())
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _controller.GetStats();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        
        // The controller returns an anonymous object, we can use reflection or dynamic
        // For simplicity, just verify the response contains data
        okResult.Value.Should().NotBeNull();
        
        // Use Newtonsoft.Json to deserialize if needed, but for now just check type
        var stats = okResult.Value;
        stats.Should().NotBeNull();
        
        // Verify properties exist using reflection
        var statsType = stats!.GetType();
        var totalTendersProperty = statsType.GetProperty("TotalTenders");
        totalTendersProperty.Should().NotBeNull();
        
        var lastUpdatedProperty = statsType.GetProperty("LastUpdated");
        lastUpdatedProperty.Should().NotBeNull();
        
        // Get values
        var totalTendersValue = totalTendersProperty!.GetValue(stats);
        totalTendersValue.Should().Be(expectedCount);
        
        var lastUpdatedValue = lastUpdatedProperty!.GetValue(stats);
        lastUpdatedValue.Should().BeOfType<DateTime>();
        
        var lastUpdated = (DateTime)lastUpdatedValue!;
        lastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetFoundTenders_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _foundTenderServiceMock
            .Setup(s => s.GetTendersAsync(It.IsAny<TenderSearchParams>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetFoundTenders();

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
