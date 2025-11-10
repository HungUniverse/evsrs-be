using EVSRS.BusinessObjects.DTO.ForecastDto;
using EVSRS.Services.Infrastructure.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using Xunit;

namespace EVSRS.Services.Tests.Infrastructure.Llm;

public class LlmAdvisorTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ILogger<LlmAdvisor>> _mockLogger;
    private readonly IOptions<OpenAiOptions> _options;

    public LlmAdvisorTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<LlmAdvisor>>();
        _options = Options.Create(new OpenAiOptions
        {
            ApiKey = "test-key",
            ModelName = "gpt-4o-mini"
        });
    }

    private LlmAdvisor CreateService(HttpMessageHandler mockHandler)
    {
        var httpClient = new HttpClient(mockHandler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        _mockHttpClientFactory.Setup(f => f.CreateClient("OpenAI")).Returns(httpClient);
        return new LlmAdvisor(_options, _mockLogger.Object, _mockHttpClientFactory.Object);
    }

    private List<CapacityRecommendation> CreateBaselineWithGaps()
    {
        return new List<CapacityRecommendation>
        {
            new CapacityRecommendation
            {
                StationId = "S1",
                VehicleType = "SUV",
                RequiredUnits = 10,
                CurrentAvailablePeak24h = 5,
                PeakP90Demand = 9.5,
                Gap = 5, // Need to buy
                Priority = 90,
                RecommendedAction = "BUY"
            },
            new CapacityRecommendation
            {
                StationId = "S2",
                VehicleType = "Sedan",
                RequiredUnits = 8,
                CurrentAvailablePeak24h = 8,
                PeakP90Demand = 7.5,
                Gap = 0, // No gap, should not appear in fallback
                Priority = 50,
                RecommendedAction = "OK"
            },
            new CapacityRecommendation
            {
                StationId = "S3",
                VehicleType = "Van",
                RequiredUnits = 12,
                CurrentAvailablePeak24h = 9,
                PeakP90Demand = 11.2,
                Gap = 3, // Need to buy
                Priority = 80,
                RecommendedAction = "BUY"
            }
        };
    }

    [Fact]
    public async Task GetAdviceAsync_WhenLlmReturnsMalformedJson_UsesFallback()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("""
                    {
                        "choices": [{ "message": { "content": "this is not valid json{ broken" } }]
                    }
                    """, Encoding.UTF8, "application/json")
            });

        var service = CreateService(mockHandler.Object);
        var baseline = CreateBaselineWithGaps();

        // Act
        var result = await service.GetAdviceAsync(
            objective: "Test",
            horizonDays: 7,
            avgTripHours: 2.0,
            turnaroundHours: 1.0,
            budget: 100000,
            maxDailyPurchase: 5,
            slaMinutes: 15,
            baseline: baseline,
            cancellationToken: CancellationToken.None
        );

        // Assert - Should use fallback
        Assert.NotNull(result);
        Assert.NotNull(result.Actions);
        Assert.Equal(2, result.Actions.Count); // Only S1 and S3 have Gap > 0
        
        // Check first action (highest priority S1)
        var firstAction = result.Actions[0];
        Assert.Equal("S1", firstAction.StationId);
        Assert.Equal("SUV", firstAction.VehicleType);
        Assert.Equal("BUY", firstAction.ActionType);
        Assert.Equal(5, firstAction.Units); // Gap
        Assert.Equal(90, firstAction.Priority);
        Assert.Contains("Fallback", firstAction.Rationale);
        Assert.Contains("P90=9.5", firstAction.Rationale);
        Assert.Contains("Current=5", firstAction.Rationale);

        // Check second action (priority 80, S3)
        var secondAction = result.Actions[1];
        Assert.Equal("S3", secondAction.StationId);
        Assert.Equal("Van", secondAction.VehicleType);
        Assert.Equal(3, secondAction.Units);
        Assert.Equal(80, secondAction.Priority);

        // Check summary
        Assert.NotNull(result.Summary);
        Assert.Equal(0m, result.Summary.TotalCost); // Fallback doesn't have cost info
        Assert.Equal(2, result.Summary.StationsAffected);
        Assert.Equal(8, result.Summary.UnitsAdded); // 5 + 3
        Assert.Equal(0, result.Summary.UnitsReallocated);
        Assert.Contains("Fallback advice: LLM unavailable", result.Summary.Notes);
    }

    [Fact]
    public async Task GetAdviceAsync_WhenLlmReturnsMissingRequiredFields_UsesFallback()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("""
                    {
                        "choices": [{
                            "message": {
                                "content": "{\"actions\":[{\"stationId\":\"S1\"}],\"summary\":{}}"
                            }
                        }]
                    }
                    """, Encoding.UTF8, "application/json")
            });

        var service = CreateService(mockHandler.Object);
        var baseline = CreateBaselineWithGaps();

        // Act
        var result = await service.GetAdviceAsync(
            objective: "Test",
            horizonDays: 7,
            avgTripHours: 2.0,
            turnaroundHours: 1.0,
            budget: 100000,
            maxDailyPurchase: 5,
            slaMinutes: 15,
            baseline: baseline,
            cancellationToken: CancellationToken.None
        );

        // Assert - Should use fallback because missing required fields
        Assert.NotNull(result);
        Assert.Equal(2, result.Actions.Count);
        Assert.Contains("Fallback advice", result.Summary.Notes);
    }

    [Fact]
    public async Task GetAdviceAsync_WhenHttpRequestFails_UsesFallback()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService(mockHandler.Object);
        var baseline = CreateBaselineWithGaps();

        // Act
        var result = await service.GetAdviceAsync(
            objective: "Test",
            horizonDays: 7,
            avgTripHours: 2.0,
            turnaroundHours: 1.0,
            budget: 100000,
            maxDailyPurchase: 5,
            slaMinutes: 15,
            baseline: baseline,
            cancellationToken: CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Actions.Count);
        Assert.Contains("Fallback advice", result.Summary.Notes);
    }

    [Fact]
    public async Task GetAdviceAsync_WhenLlmTimesOut_UsesFallback()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var service = CreateService(mockHandler.Object);
        var baseline = CreateBaselineWithGaps();

        // Act
        var result = await service.GetAdviceAsync(
            objective: "Test",
            horizonDays: 7,
            avgTripHours: 2.0,
            turnaroundHours: 1.0,
            budget: 100000,
            maxDailyPurchase: 5,
            slaMinutes: 15,
            baseline: baseline,
            cancellationToken: CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Actions.Count);
        Assert.Contains("Fallback advice", result.Summary.Notes);
    }

    [Fact]
    public async Task GetAdviceAsync_FallbackOrdersByPriorityDescending()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Simulated failure"));

        var service = CreateService(mockHandler.Object);
        
        var baseline = new List<CapacityRecommendation>
        {
            new CapacityRecommendation { StationId = "S1", VehicleType = "SUV", Gap = 5, Priority = 50, RequiredUnits = 10, CurrentAvailablePeak24h = 5, PeakP90Demand = 9.5, RecommendedAction = "BUY" },
            new CapacityRecommendation { StationId = "S2", VehicleType = "Sedan", Gap = 3, Priority = 90, RequiredUnits = 8, CurrentAvailablePeak24h = 5, PeakP90Demand = 7.5, RecommendedAction = "BUY" },
            new CapacityRecommendation { StationId = "S3", VehicleType = "Van", Gap = 2, Priority = 70, RequiredUnits = 6, CurrentAvailablePeak24h = 4, PeakP90Demand = 5.5, RecommendedAction = "BUY" }
        };

        // Act
        var result = await service.GetAdviceAsync(
            objective: "Test",
            horizonDays: 7,
            avgTripHours: 2.0,
            turnaroundHours: 1.0,
            budget: 100000,
            maxDailyPurchase: 5,
            slaMinutes: 15,
            baseline: baseline,
            cancellationToken: CancellationToken.None
        );

        // Assert - Should be ordered by Priority DESC: S2(90), S3(70), S1(50)
        Assert.Equal(3, result.Actions.Count);
        Assert.Equal("S2", result.Actions[0].StationId);
        Assert.Equal(90, result.Actions[0].Priority);
        Assert.Equal("S3", result.Actions[1].StationId);
        Assert.Equal(70, result.Actions[1].Priority);
        Assert.Equal("S1", result.Actions[2].StationId);
        Assert.Equal(50, result.Actions[2].Priority);
    }

    [Fact]
    public async Task GetAdviceAsync_WhenNoGaps_FallbackReturnsEmptyActions()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Simulated failure"));

        var service = CreateService(mockHandler.Object);
        
        var baseline = new List<CapacityRecommendation>
        {
            new CapacityRecommendation { StationId = "S1", VehicleType = "SUV", Gap = 0, Priority = 50, RequiredUnits = 5, CurrentAvailablePeak24h = 5, PeakP90Demand = 4.5, RecommendedAction = "OK" },
            new CapacityRecommendation { StationId = "S2", VehicleType = "Sedan", Gap = -2, Priority = 50, RequiredUnits = 3, CurrentAvailablePeak24h = 5, PeakP90Demand = 2.5, RecommendedAction = "SURPLUS" }
        };

        // Act
        var result = await service.GetAdviceAsync(
            objective: "Test",
            horizonDays: 7,
            avgTripHours: 2.0,
            turnaroundHours: 1.0,
            budget: 100000,
            maxDailyPurchase: 5,
            slaMinutes: 15,
            baseline: baseline,
            cancellationToken: CancellationToken.None
        );

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Actions);
        Assert.Equal(0, result.Summary.UnitsAdded);
        Assert.Contains("Fallback advice", result.Summary.Notes);
    }

    [Fact]
    public async Task GetAdviceAsync_WhenValidLlmResponse_ReturnsLlmAdvice()
    {
        // Arrange
        var validLlmResponse = """
            {
                "choices": [{
                    "message": {
                        "content": "{\"actions\":[{\"stationId\":\"S1\",\"vehicleType\":\"SUV\",\"actionType\":\"REALLOCATE_IN\",\"units\":3,\"priority\":90,\"rationale\":\"Reallocate from S2\",\"relatedStationId\":\"S2\"},{\"stationId\":\"S2\",\"vehicleType\":\"SUV\",\"actionType\":\"REALLOCATE_OUT\",\"units\":3,\"priority\":90,\"rationale\":\"Has surplus\"}],\"summary\":{\"totalCost\":0,\"stationsAffected\":2,\"unitsAdded\":0,\"unitsReallocated\":3,\"notes\":\"Reallocation strategy\"}}"
                    }
                }]
            }
            """;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(validLlmResponse, Encoding.UTF8, "application/json")
            });

        var service = CreateService(mockHandler.Object);
        var baseline = CreateBaselineWithGaps();

        // Act
        var result = await service.GetAdviceAsync(
            objective: "Test",
            horizonDays: 7,
            avgTripHours: 2.0,
            turnaroundHours: 1.0,
            budget: 100000,
            maxDailyPurchase: 5,
            slaMinutes: 15,
            baseline: baseline,
            cancellationToken: CancellationToken.None
        );

        // Assert - Should use LLM response, not fallback
        Assert.NotNull(result);
        Assert.Equal(2, result.Actions.Count);
        Assert.Equal("REALLOCATE_IN", result.Actions[0].ActionType);
        Assert.Equal("REALLOCATE_OUT", result.Actions[1].ActionType);
        Assert.Equal(3, result.Summary.UnitsReallocated);
        Assert.DoesNotContain("Fallback", result.Summary.Notes ?? "");
    }
}
