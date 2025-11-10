using EVSRS.API.Configuration;
using EVSRS.API.Constant;
using EVSRS.BusinessObjects.DTO.ForecastDto;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EVSRS.API.Controllers;

/// <summary>
/// Demand forecasting and baseline capacity recommendations
/// </summary>
[ApiController]
[Route(ApiEndPointConstant.Forecast.ForecastEndpoint)]
[Authorize]
public class ForecastController : ControllerBase
{
    private readonly IForecastingService _forecastingService;
    private readonly FeaturesOptions _features;
    private readonly ILogger<ForecastController> _logger;

    public ForecastController(
        IForecastingService forecastingService,
        IOptions<FeaturesOptions> features,
        ILogger<ForecastController> logger)
    {
        _forecastingService = forecastingService;
        _features = features.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get demand statistics and baseline capacity recommendations for a station
    /// </summary>
    /// <param name="stationId">Station ID to forecast</param>
    /// <param name="vehicleType">Optional vehicle type filter</param>
    /// <param name="horizonDays">Forecast horizon in days (default: 7)</param>
    /// <param name="avgTripHours">Average trip duration in hours (default: 2.0)</param>
    /// <param name="turnaroundHours">Turnaround time in hours (default: 1.0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Demand statistics and capacity recommendations</returns>
    /// <response code="200">Returns forecast data</response>
    /// <response code="404">Feature disabled or no data found</response>
    [HttpGet("{stationId}")]
    [ProducesResponseType(typeof(ForecastResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ForecastResponse>> GetForecast(
        [FromRoute] string stationId,
        [FromQuery] string? vehicleType = null,
        [FromQuery] int horizonDays = 7,
        [FromQuery] double avgTripHours = 2.0,
        [FromQuery] double turnaroundHours = 1.0,
        CancellationToken cancellationToken = default)
    {
        if (!_features.ForecastCapacity)
        {
            _logger.LogWarning("Forecast API called but feature is disabled");
            return NotFound(new { error = "Forecast feature is not enabled" });
        }

        _logger.LogInformation(
            "Getting forecast for station={StationId}, vehicleType={VehicleType}, horizon={Horizon}",
            stationId, vehicleType ?? "ALL", horizonDays);

        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddDays(-horizonDays);

        var recommendations = new List<CapacityRecommendation>();

        if (!string.IsNullOrEmpty(vehicleType))
        {
            // Single vehicle type
            var stats = await _forecastingService.GetStatsAsync(
                stationId,
                vehicleType,
                startDate,
                endDate,
                cancellationToken);

            if (stats == null)
            {
                return NotFound(new { error = $"No data found for station {stationId}, vehicle type {vehicleType}" });
            }

            var requiredUnits = _forecastingService.GetRequiredUnits(
                stats.P90,
                avgTripHours,
                turnaroundHours);

            var currentAvailable = await _forecastingService.LoadCurrentAvailabilityPeak24hAsync(
                stationId,
                vehicleType,
                cancellationToken);

            var gap = requiredUnits - currentAvailable;
            var recommendedAction = gap > 0 ? "BUY" : (gap < 0 ? "SURPLUS" : "OK");

            recommendations.Add(new CapacityRecommendation
            {
                StationId = stationId,
                VehicleType = vehicleType,
                RequiredUnits = requiredUnits,
                CurrentAvailablePeak24h = currentAvailable,
                PeakP90Demand = stats.P90,
                Gap = gap,
                Priority = CalculatePriority(gap, stats.P90),
                RecommendedAction = recommendedAction
            });
        }
        else
        {
            // All vehicle types for this station
            var stationVehicleTypes = await _forecastingService.GetStationVehicleTypesAsync(cancellationToken);
            var stationTypes = stationVehicleTypes
                .Where(sv => sv.StationId == stationId)
                .ToList();

            if (!stationTypes.Any())
            {
                return NotFound(new { error = $"No data found for station {stationId}" });
            }

            foreach (var (_, vType) in stationTypes)
            {
                var stats = await _forecastingService.GetStatsAsync(
                    stationId,
                    vType,
                    startDate,
                    endDate,
                    cancellationToken);

                if (stats == null) continue;

                var requiredUnits = _forecastingService.GetRequiredUnits(
                    stats.P90,
                    avgTripHours,
                    turnaroundHours);

                var currentAvailable = await _forecastingService.LoadCurrentAvailabilityPeak24hAsync(
                    stationId,
                    vType,
                    cancellationToken);

                var gap = requiredUnits - currentAvailable;
                var recommendedAction = gap > 0 ? "BUY" : (gap < 0 ? "SURPLUS" : "OK");

                recommendations.Add(new CapacityRecommendation
                {
                    StationId = stationId,
                    VehicleType = vType,
                    RequiredUnits = requiredUnits,
                    CurrentAvailablePeak24h = currentAvailable,
                    PeakP90Demand = stats.P90,
                    Gap = gap,
                    Priority = CalculatePriority(gap, stats.P90),
                    RecommendedAction = recommendedAction
                });
            }
        }

        var response = new ForecastResponse
        {
            StationId = stationId,
            ForecastPeriod = new DateRange
            {
                StartDate = startDate,
                EndDate = endDate
            },
            HorizonDays = horizonDays,
            Recommendations = recommendations.OrderByDescending(r => r.Priority).ToList(),
            GeneratedAt = DateTime.UtcNow
        };

        return Ok(response);
    }

    private static int CalculatePriority(int gap, double peakDemand)
    {
        if (gap <= 0) return 0;
        
        var shortageRatio = gap / Math.Max(peakDemand, 1.0);
        return (int)Math.Min(shortageRatio * 100, 100);
    }
}

/// <summary>
/// Forecast response with recommendations
/// </summary>
public class ForecastResponse
{
    /// <summary>
    /// Station ID
    /// </summary>
    public string StationId { get; set; } = null!;

    /// <summary>
    /// Period used for forecast analysis
    /// </summary>
    public DateRange ForecastPeriod { get; set; } = null!;

    /// <summary>
    /// Forecast horizon in days
    /// </summary>
    public int HorizonDays { get; set; }

    /// <summary>
    /// Capacity recommendations ordered by priority
    /// </summary>
    public List<CapacityRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// When this forecast was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Date range
/// </summary>
public class DateRange
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
