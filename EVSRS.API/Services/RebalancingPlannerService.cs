using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.DTO.ForecastDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace EVSRS.API.Services;

/// <summary>
/// Background service to periodically generate rebalancing plans
/// Analyzes gaps across all depots and proposes RELOCATE/PURCHASE actions
/// Runs every 12 hours or can be triggered manually via API
/// </summary>
public class RebalancingPlannerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RebalancingPlannerService> _logger;
    private readonly TimeSpan _planningInterval = TimeSpan.FromHours(12); // Every 12 hours

    public RebalancingPlannerService(
        IServiceProvider serviceProvider,
        ILogger<RebalancingPlannerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RebalancingPlannerService started. Will generate plans every {Interval}",
            _planningInterval);

        // Wait 15 minutes before first plan to let app fully start
        await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateRebalancingPlansAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating rebalancing plans");
            }

            // Wait for next planning cycle
            await Task.Delay(_planningInterval, stoppingToken);
        }
    }

    private async Task GenerateRebalancingPlansAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var forecastingService = scope.ServiceProvider.GetRequiredService<IForecastingService>();

        var startTime = DateTime.UtcNow;
        var targetDate = DateTime.UtcNow.AddDays(1); // Plan for tomorrow
        
        _logger.LogInformation("Starting rebalancing plan generation for {TargetDate}", targetDate.ToString("yyyy-MM-dd"));

        try
        {
            // Get all station-vehicle combinations
            var stationVehicleTypes = await forecastingService.GetStationVehicleTypesAsync(cancellationToken);

            if (!stationVehicleTypes.Any())
            {
                _logger.LogWarning("No station-vehicle combinations found for rebalancing");
                return;
            }

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-7);

            // Step 1: Calculate gaps for all depot-model combinations
            var gapAnalysis = new Dictionary<(string StationId, string VehicleType), CapacityGap>();

            foreach (var (stationId, vehicleType) in stationVehicleTypes)
            {
                try
                {
                    var stats = await forecastingService.GetStatsAsync(
                        stationId, vehicleType, startDate, endDate, cancellationToken);

                    if (stats == null) continue;

                    var requiredUnits = forecastingService.GetRequiredUnits(stats.P90, 2.0, 1.0);
                    var currentAvailable = await forecastingService.LoadCurrentAvailabilityPeak24hAsync(
                        stationId, vehicleType, cancellationToken);

                    var gap = requiredUnits - currentAvailable;

                    gapAnalysis[(stationId, vehicleType)] = new CapacityGap
                    {
                        StationId = stationId,
                        VehicleType = vehicleType,
                        RequiredUnits = requiredUnits,
                        CurrentAvailable = currentAvailable,
                        Gap = gap,
                        P90Demand = stats.P90
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to analyze gap for {StationId}-{VehicleType}",
                        stationId, vehicleType);
                }
            }

            // Step 2: Generate rebalancing plans
            var plans = new List<RebalancingPlan>();

            // Group by vehicle type to find surplus/shortage matches
            var byVehicleType = gapAnalysis
                .GroupBy(kvp => kvp.Key.VehicleType)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (vehicleType, gaps) in byVehicleType)
            {
                var shortages = gaps.Where(g => g.Value.Gap > 0).OrderByDescending(g => g.Value.Gap).ToList();
                var surpluses = gaps.Where(g => g.Value.Gap < 0).OrderBy(g => g.Value.Gap).ToList();

                // Match surpluses with shortages
                foreach (var shortage in shortages)
                {
                    var shortageGap = shortage.Value;
                    var needed = shortageGap.Gap;

                    // Try to fill from surpluses first (RELOCATE)
                    foreach (var surplus in surpluses)
                    {
                        if (needed <= 0) break;

                        var surplusGap = surplus.Value;
                        var available = Math.Abs(surplusGap.Gap);

                        if (available <= 0) continue;

                        var relocateQty = Math.Min(needed, available);

                        plans.Add(new RebalancingPlan
                        {
                            Id = Guid.NewGuid().ToString(),
                            PlanDate = targetDate,
                            FromDepotId = surplusGap.StationId,
                            ToDepotId = shortageGap.StationId,
                            ModelId = vehicleType,
                            Quantity = relocateQty,
                            ActionType = "RELOCATE",
                            Priority = CalculatePriority(shortageGap.Gap, shortageGap.P90Demand),
                            EstimatedCost = 0, // Relocate has minimal cost
                            Status = "PROPOSED",
                            Reason = $"Shortage of {shortageGap.Gap} units at destination, surplus of {available} at source",
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = "SYSTEM",
                            IsDeleted = false
                        });

                        needed -= relocateQty;
                        surplusGap.Gap += relocateQty; // Reduce surplus
                    }

                    // If still shortage, propose PURCHASE
                    if (needed > 0)
                    {
                        plans.Add(new RebalancingPlan
                        {
                            Id = Guid.NewGuid().ToString(),
                            PlanDate = targetDate,
                            FromDepotId = null, // No source depot for purchase
                            ToDepotId = shortageGap.StationId,
                            ModelId = vehicleType,
                            Quantity = needed,
                            ActionType = "PURCHASE",
                            Priority = CalculatePriority(shortageGap.Gap, shortageGap.P90Demand),
                            EstimatedCost = needed * 25000m, // Estimate $25k per vehicle
                            Status = "PROPOSED",
                            Reason = $"Cannot fulfill shortage of {needed} units through relocation",
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = "SYSTEM",
                            IsDeleted = false
                        });
                    }
                }
            }

            if (plans.Any())
            {
                // Delete old PROPOSED plans for same date
                await dbContext.RebalancingPlans
                    .Where(p => p.PlanDate.Date == targetDate.Date && p.Status == "PROPOSED")
                    .ExecuteDeleteAsync(cancellationToken);

                // Insert new plans
                await dbContext.RebalancingPlans.AddRangeAsync(plans, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "Successfully generated {Count} rebalancing plans ({Relocate} relocate, {Purchase} purchase) in {Duration}ms",
                    plans.Count,
                    plans.Count(p => p.ActionType == "RELOCATE"),
                    plans.Count(p => p.ActionType == "PURCHASE"),
                    duration.TotalMilliseconds);

                // Clean up old plans (keep only last 30 days)
                await CleanupOldPlansAsync(dbContext, cancellationToken);
            }
            else
            {
                _logger.LogInformation("No rebalancing needed - all depots are balanced");
            }
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Failed to generate rebalancing plans after {Duration}ms",
                duration.TotalMilliseconds);
            throw;
        }
    }

    private static int CalculatePriority(int gap, double p90Demand)
    {
        if (gap <= 0) return 0;
        var shortageRatio = gap / Math.Max(p90Demand, 1.0);
        return (int)Math.Min(shortageRatio * 100, 100);
    }

    private async Task CleanupOldPlansAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            var deletedCount = await dbContext.RebalancingPlans
                .Where(p => p.PlanDate < cutoffDate && p.Status == "PROPOSED")
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation(
                    "Cleaned up {Count} old rebalancing plans (older than 30 days)",
                    deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up old plans");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RebalancingPlannerService is stopping");
        await base.StopAsync(cancellationToken);
    }

    // Helper class
    private class CapacityGap
    {
        public string StationId { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public int RequiredUnits { get; set; }
        public int CurrentAvailable { get; set; }
        public int Gap { get; set; }
        public double P90Demand { get; set; }
    }
}
