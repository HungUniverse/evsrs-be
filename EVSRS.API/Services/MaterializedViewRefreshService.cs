using EVSRS.BusinessObjects.DBContext;
using Microsoft.EntityFrameworkCore;

namespace EVSRS.API.Services;

/// <summary>
/// Background service to periodically refresh materialized views for demand forecasting
/// </summary>
public class MaterializedViewRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MaterializedViewRefreshService> _logger;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromHours(1); // Refresh every hour

    public MaterializedViewRefreshService(
        IServiceProvider serviceProvider,
        ILogger<MaterializedViewRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "MaterializedViewRefreshService started. Will refresh every {Interval}",
            _refreshInterval);

        // Wait 5 minutes before first refresh to let app fully start
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshMaterializedViewAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing materialized view");
            }

            // Wait for next refresh cycle
            await Task.Delay(_refreshInterval, stoppingToken);
        }
    }

    private async Task RefreshMaterializedViewAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting refresh of vw_rental_demand_30m_last_56d");

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "REFRESH MATERIALIZED VIEW vw_rental_demand_30m_last_56d",
                cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Successfully refreshed materialized view in {Duration}ms",
                duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(
                ex,
                "Failed to refresh materialized view after {Duration}ms",
                duration.TotalMilliseconds);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MaterializedViewRefreshService is stopping");
        await base.StopAsync(cancellationToken);
    }
}
