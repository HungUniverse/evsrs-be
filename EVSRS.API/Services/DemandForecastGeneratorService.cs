using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.DTO.ForecastDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace EVSRS.API.Services;

/// <summary>
/// Background service to periodically generate demand forecasts
/// Runs every 6 hours to predict demand for the next 24 hours
/// </summary>
public class DemandForecastGeneratorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DemandForecastGeneratorService> _logger;
    private readonly TimeSpan _generationInterval = TimeSpan.FromHours(6); // Every 6 hours
    private readonly int _forecastHorizonHours = 24; // Forecast next 24 hours

    public DemandForecastGeneratorService(
        IServiceProvider serviceProvider,
        ILogger<DemandForecastGeneratorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "DemandForecastGeneratorService started. Will generate forecasts every {Interval}",
            _generationInterval);

        // Wait 10 minutes before first forecast to let app fully start
        await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateForecastsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating demand forecasts");
            }

            // Wait for next forecast cycle
            await Task.Delay(_generationInterval, stoppingToken);
        }
    }

    private async Task GenerateForecastsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var forecastingService = scope.ServiceProvider.GetRequiredService<IForecastingService>();

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting demand forecast generation");

        try
        {
            // Get all station-vehicle combinations
            var stationVehicleTypes = await forecastingService.GetStationVehicleTypesAsync(cancellationToken);

            if (!stationVehicleTypes.Any())
            {
                _logger.LogWarning("No station-vehicle combinations found for forecasting");
                return;
            }

            var forecasts = new List<DemandForecast>();
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-7); // Use 7 days historical data

            // Generate forecasts for each station-vehicle combination
            foreach (var (stationId, vehicleType) in stationVehicleTypes)
            {
                try
                {
                    // Get demand stats from ForecastingService
                    var stats = await forecastingService.GetStatsAsync(
                        stationId,
                        vehicleType,
                        startDate,
                        endDate,
                        cancellationToken);

                    if (stats == null)
                    {
                        _logger.LogDebug(
                            "No stats for station={StationId}, vehicle={VehicleType}",
                            stationId, vehicleType);
                        continue;
                    }

                    // Generate forecasts for next 24 hours in 30-minute intervals
                    for (int i = 0; i < _forecastHorizonHours * 2; i++) // *2 because 30-min intervals
                    {
                        var forecastTime = DateTime.UtcNow.AddMinutes(i * 30);
                        
                        // Round to 30-minute bin
                        forecastTime = new DateTime(
                            forecastTime.Year, forecastTime.Month, forecastTime.Day,
                            forecastTime.Hour, forecastTime.Minute < 30 ? 0 : 30, 0,
                            DateTimeKind.Utc);

                        forecasts.Add(new DemandForecast
                        {
                            Id = Guid.NewGuid().ToString(),
                            DepotId = stationId,
                            ModelId = vehicleType,
                            ForecastTime = forecastTime,
                            PredictedDemand = (decimal)stats.P90, // Use P90 as predicted demand
                            ConfidenceScore = CalculateConfidenceScore(stats),
                            Method = "P90",
                            GeneratedAt = DateTime.UtcNow,
                            HorizonMinutes = 30,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = "SYSTEM",
                            IsDeleted = false
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to generate forecast for station={StationId}, vehicle={VehicleType}",
                        stationId, vehicleType);
                }
            }

            if (forecasts.Any())
            {
                // Delete old forecasts for same time slots
                var forecastTimes = forecasts.Select(f => f.ForecastTime).Distinct().ToList();
                await dbContext.DemandForecasts
                    .Where(f => forecastTimes.Contains(f.ForecastTime))
                    .ExecuteDeleteAsync(cancellationToken);

                // Insert new forecasts
                await dbContext.DemandForecasts.AddRangeAsync(forecasts, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "Successfully generated {Count} demand forecasts for {Stations} stations in {Duration}ms",
                    forecasts.Count,
                    stationVehicleTypes.Select(s => s.StationId).Distinct().Count(),
                    duration.TotalMilliseconds);

                // Clean up old forecasts (keep only last 7 days)
                await CleanupOldForecastsAsync(dbContext, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(
                ex,
                "Failed to generate demand forecasts after {Duration}ms",
                duration.TotalMilliseconds);
            throw;
        }
    }

    private static decimal CalculateConfidenceScore(DemandStats stats)
    {
        // Simple confidence based on variance
        // Lower variance = higher confidence
        if (stats.Mean <= 0) return 0.5m;
        
        var coefficientOfVariation = Math.Abs(stats.P90 - stats.Mean) / stats.Mean;
        var confidence = 1.0 - Math.Min(coefficientOfVariation, 0.5);
        
        return (decimal)Math.Max(0.1, Math.Min(1.0, confidence));
    }

    private async Task CleanupOldForecastsAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-7);
            var deletedCount = await dbContext.DemandForecasts
                .Where(f => f.ForecastTime < cutoffDate)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation(
                    "Cleaned up {Count} old demand forecasts (older than 7 days)",
                    deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up old forecasts");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("DemandForecastGeneratorService is stopping");
        await base.StopAsync(cancellationToken);
    }
}
