using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using Microsoft.EntityFrameworkCore;

namespace EVSRS.API.Services;

/// <summary>
/// Background service to periodically capture inventory snapshots for demand forecasting
/// Runs every 30 minutes to align with materialized view time bins
/// </summary>
public class InventorySnapshotService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventorySnapshotService> _logger;
    private readonly TimeSpan _snapshotInterval = TimeSpan.FromMinutes(30); // Every 30 minutes

    public InventorySnapshotService(
        IServiceProvider serviceProvider,
        ILogger<InventorySnapshotService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "InventorySnapshotService started. Will capture snapshots every {Interval}",
            _snapshotInterval);

        // Wait 2 minutes before first snapshot to let app fully start
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CaptureSnapshotAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing inventory snapshot");
            }

            // Wait for next snapshot cycle
            await Task.Delay(_snapshotInterval, stoppingToken);
        }
    }

    private async Task CaptureSnapshotAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var startTime = DateTime.UtcNow;
        
        // Round to 30-minute bin (align with materialized view)
        var snapshotTime = new DateTime(
            startTime.Year, startTime.Month, startTime.Day, 
            startTime.Hour, startTime.Minute < 30 ? 0 : 30, 0, DateTimeKind.Utc);

        _logger.LogInformation("Capturing inventory snapshot at {SnapshotTime}", snapshotTime);

        try
        {
            // Get all depot-model combinations with vehicle counts
            var inventoryData = await dbContext.CarEVs
                .Where(c => !c.IsDeleted)
                .GroupBy(c => new { c.DepotId, c.ModelId })
                .Select(g => new
                {
                    DepotId = g.Key.DepotId,
                    ModelId = g.Key.ModelId,
                    AvailableCount = g.Count(c => c.Status == CarEvStatus.AVAILABLE),
                    ChargingCount = g.Count(c => c.Status == CarEvStatus.REPAIRING), // Assuming charging status
                    MaintenanceCount = g.Count(c => c.Status == CarEvStatus.REPAIRING),
                    InUseCount = g.Count(c => c.Status == CarEvStatus.IN_USE),
                    ReservedCount = g.Count(c => c.Status == CarEvStatus.RESERVED)
                })
                .ToListAsync(cancellationToken);

            if (!inventoryData.Any())
            {
                _logger.LogWarning("No inventory data found for snapshot");
                return;
            }

            // Create snapshot records
            var snapshots = inventoryData.Select(data => new InventorySnapshot
            {
                Id = Guid.NewGuid().ToString(),
                DepotId = data.DepotId,
                ModelId = data.ModelId,
                SnapshotTime = snapshotTime,
                AvailableCount = data.AvailableCount,
                ChargingCount = data.ChargingCount,
                MaintenanceCount = data.MaintenanceCount,
                InUseCount = data.InUseCount,
                ReservedCount = data.ReservedCount,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "SYSTEM",
                IsDeleted = false
            }).ToList();

            await dbContext.InventorySnapshots.AddRangeAsync(snapshots, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Successfully captured {Count} inventory snapshots in {Duration}ms",
                snapshots.Count, duration.TotalMilliseconds);

            // Clean up old snapshots (keep only last 30 days)
            await CleanupOldSnapshotsAsync(dbContext, cancellationToken);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(
                ex,
                "Failed to capture inventory snapshot after {Duration}ms",
                duration.TotalMilliseconds);
            throw;
        }
    }

    private async Task CleanupOldSnapshotsAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            var deletedCount = await dbContext.InventorySnapshots
                .Where(s => s.SnapshotTime < cutoffDate)
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation(
                    "Cleaned up {Count} old inventory snapshots (older than 30 days)",
                    deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up old snapshots");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("InventorySnapshotService is stopping");
        await base.StopAsync(cancellationToken);
    }
}
