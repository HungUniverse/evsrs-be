using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.DTO.ForecastDto;
using EVSRS.Repositories.Implement;
using EVSRS.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EVSRS.Services.Service
{
    /// <summary>
    /// Service dự báo nhu cầu (forecast): thu thập dữ liệu lịch sử, tính thống kê (P90/mean)
    /// và sinh forecast cho các slot 30 phút.
    /// Sử dụng materialized view `vw_rental_demand_30m_last_56d` làm nguồn dữ liệu lịch sử.
    /// </summary>
    public class ForecastingService : IForecastingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ForecastingService> _logger;

        public ForecastingService(
            IUnitOfWork unitOfWork,
            ApplicationDbContext dbContext,
            ILogger<ForecastingService> logger)
        {
            _unitOfWork = unitOfWork;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Dictionary<SlotKey, DemandStats>> GetStatsAsync(
            List<string>? stationIds = null,
            List<string>? vehicleTypes = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting demand stats for stations={StationIds}, vehicles={VehicleTypes}",
                stationIds != null ? string.Join(",", stationIds) : "ALL",
                vehicleTypes != null ? string.Join(",", vehicleTypes) : "ALL");

            // Build SQL query with optional filters
            var sqlQuery = @"
                SELECT 
                    station_id,
                    vehicle_type,
                    EXTRACT(DOW FROM bin_ts)::int AS day_of_week,
                    EXTRACT(HOUR FROM bin_ts)::int AS hour,
                    EXTRACT(MINUTE FROM bin_ts)::int AS minute,
                    demand
                FROM vw_rental_demand_30m_last_56d
                WHERE 1=1";

            var parameters = new List<object>();
            
            if (stationIds != null && stationIds.Count > 0)
            {
                sqlQuery += $" AND station_id = ANY(@p{parameters.Count})";
                parameters.Add(stationIds.ToArray());
            }

            if (vehicleTypes != null && vehicleTypes.Count > 0)
            {
                sqlQuery += $" AND vehicle_type = ANY(@p{parameters.Count})";
                parameters.Add(vehicleTypes.ToArray());
            }

            sqlQuery += " ORDER BY station_id, vehicle_type, day_of_week, hour, minute";

            // Execute raw SQL query
            var connection = _dbContext.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            
            for (int i = 0; i < parameters.Count; i++)
            {
                var param = command.CreateParameter();
                param.ParameterName = $"p{i}";
                param.Value = parameters[i];
                command.Parameters.Add(param);
            }

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            // Read results and group by slot
            var slotGroups = new Dictionary<SlotKey, List<double>>();
            
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var slot = new SlotKey
                    {
                        StationId = reader.GetString(0),
                        VehicleType = reader.GetString(1),
                        DayOfWeek = reader.GetInt32(2),
                        Hour = reader.GetInt32(3),
                        Minute = reader.GetInt32(4)
                    };

                    var demand = Convert.ToDouble(reader.GetValue(5));

                    if (!slotGroups.ContainsKey(slot))
                    {
                        slotGroups[slot] = new List<double>();
                    }
                    slotGroups[slot].Add(demand);
                }
            }

            _logger.LogInformation("Loaded {SlotCount} unique slots from materialized view", slotGroups.Count);

            // Calculate statistics for each slot
            var result = new Dictionary<SlotKey, DemandStats>();
            
            foreach (var (slot, demands) in slotGroups)
            {
                if (demands.Count == 0) continue;

                var sortedDemands = demands.OrderBy(d => d).ToArray();
                
                var stats = new DemandStats
                {
                    Slot = slot,
                    Mean = demands.Average(),
                    P90 = CalculateQuantile(sortedDemands, 0.90),
                    Min = sortedDemands[0],
                    Max = sortedDemands[^1],
                    SampleCount = demands.Count,
                    DemandValues = demands
                };

                result[slot] = stats;
            }

            _logger.LogInformation("Calculated stats for {StatsCount} slots", result.Count);
            return result;
        }

        public async Task<List<CapacityRecommendation>> GetRequiredUnitsAsync(
            Dictionary<SlotKey, DemandStats> stats,
            double avgTripHours = 4.0,
            double turnaroundHours = 0.5,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Calculating required units with avgTrip={AvgTrip}h, turnaround={Turnaround}h",
                avgTripHours, turnaroundHours);

            if (turnaroundHours <= 0)
            {
                throw new ArgumentException("Turnaround hours must be positive", nameof(turnaroundHours));
            }

            // Group by station and vehicle type to find peak P90
            var stationVehicleGroups = stats
                .GroupBy(kv => (kv.Key.StationId, kv.Key.VehicleType))
                .ToList();

            // Load current availability
            var stationIds = stationVehicleGroups.Select(g => g.Key.StationId).Distinct().ToList();
            var vehicleTypes = stationVehicleGroups.Select(g => g.Key.VehicleType).Distinct().ToList();
            var availability = await LoadCurrentAvailabilityPeak24hAsync(stationIds, vehicleTypes, cancellationToken);

            // Load station and vehicle names for display
            var stationNames = await _dbContext.Depots
                .Where(d => stationIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.Name ?? d.Id, cancellationToken);

            var vehicleNames = await _dbContext.Models
                .Where(m => vehicleTypes.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => m.ModelName ?? m.Id, cancellationToken);

            var recommendations = new List<CapacityRecommendation>();

            foreach (var group in stationVehicleGroups)
            {
                var (stationId, vehicleType) = group.Key;
                
                // Find peak P90 across all slots for this station/vehicle
                var peakSlot = group
                    .OrderByDescending(kv => kv.Value.P90)
                    .First();

                var peakP90 = peakSlot.Value.P90;
                
                // Calculate required units
                // Formula: required = ceil(P90 * avgTripHours / turnaroundHours)
                var requiredUnits = (int)Math.Ceiling(peakP90 * avgTripHours / turnaroundHours);
                
                // Get current availability (default to 0 if not found)
                var currentAvailable = availability.GetValueOrDefault((stationId, vehicleType), 0);
                
                // Calculate gap
                var gap = Math.Max(0, requiredUnits - currentAvailable);
                
                // Calculate priority (higher gap = higher priority)
                var priority = gap > 0 ? (int)Math.Min(100, gap * 5 + peakP90 * 2) : 0;
                
                // Determine recommended action
                string? action = null;
                string? reason = null;
                
                if (gap > 0)
                {
                    action = gap >= 10 ? "PURCHASE" : "RELOCATE";
                    reason = $"Peak demand P90={peakP90:F1} at {peakSlot.Key.GetCompositeKey().Split('|')[2..].Aggregate((a, b) => $"{a}|{b}")}. " +
                             $"Need {requiredUnits} units but only {currentAvailable} available. Gap: {gap} units.";
                }
                else if (currentAvailable > requiredUnits * 1.5)
                {
                    action = "SURPLUS";
                    reason = $"Surplus of {currentAvailable - requiredUnits} units. Consider relocating to high-demand stations.";
                }

                var recommendation = new CapacityRecommendation
                {
                    StationId = stationId,
                    StationName = stationNames.GetValueOrDefault(stationId, stationId),
                    VehicleType = vehicleType,
                    VehicleTypeName = vehicleNames.GetValueOrDefault(vehicleType, vehicleType),
                    PeakP90Demand = peakP90,
                    PeakSlot = peakSlot.Key,
                    RequiredUnits = requiredUnits,
                    CurrentAvailablePeak24h = currentAvailable,
                    Gap = gap,
                    Priority = priority,
                    RecommendedAction = action,
                    Reason = reason
                };

                recommendations.Add(recommendation);
            }

            _logger.LogInformation("Generated {RecommendationCount} capacity recommendations", recommendations.Count);
            
            return recommendations
                .OrderByDescending(r => r.Priority)
                .ThenByDescending(r => r.Gap)
                .ToList();
        }

        public async Task<Dictionary<(string StationId, string VehicleType), int>> LoadCurrentAvailabilityPeak24hAsync(
            List<string>? stationIds = null,
            List<string>? vehicleTypes = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Loading current availability peak for next 24h");

            var now = DateTime.UtcNow;
            var next24h = now.AddHours(24);

            // Query InventorySnapshot for next 24h window
            var query = _dbContext.InventorySnapshots
                .Where(s => s.SnapshotTime >= now && s.SnapshotTime <= next24h);

            if (stationIds != null && stationIds.Count > 0)
            {
                query = query.Where(s => stationIds.Contains(s.DepotId));
            }

            if (vehicleTypes != null && vehicleTypes.Count > 0)
            {
                query = query.Where(s => vehicleTypes.Contains(s.ModelId));
            }

            var snapshots = await query
                .Select(s => new
                {
                    s.DepotId,
                    s.ModelId,
                    s.AvailableCount
                })
                .ToListAsync(cancellationToken);

            if (snapshots.Count == 0)
            {
                _logger.LogWarning("No inventory snapshots found for next 24h. Falling back to current CarEV status.");
                return await LoadCurrentAvailabilityFromCarEVAsync(stationIds, vehicleTypes, cancellationToken);
            }

            // Group by station/vehicle and take MINIMUM available in 24h window (peak demand scenario)
            var result = snapshots
                .GroupBy(s => (s.DepotId, s.ModelId))
                .ToDictionary(
                    g => g.Key,
                    g => g.Min(s => s.AvailableCount)
                );

            _logger.LogInformation("Loaded availability for {GroupCount} station/vehicle combinations", result.Count);
            return result;
        }

        /// <summary>
        /// Fallback: Load current availability from CarEV table if no snapshots exist
        /// </summary>
        private async Task<Dictionary<(string StationId, string VehicleType), int>> LoadCurrentAvailabilityFromCarEVAsync(
            List<string>? stationIds = null,
            List<string>? vehicleTypes = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.CarEVs
                .Where(c => c.Status == BusinessObjects.Enum.CarEvStatus.AVAILABLE);

            if (stationIds != null && stationIds.Count > 0)
            {
                query = query.Where(c => c.DepotId != null && stationIds.Contains(c.DepotId));
            }

            if (vehicleTypes != null && vehicleTypes.Count > 0)
            {
                query = query.Where(c => c.ModelId != null && vehicleTypes.Contains(c.ModelId));
            }

            var cars = await query
                .Select(c => new { c.DepotId, c.ModelId })
                .ToListAsync(cancellationToken);

            var result = cars
                .Where(c => c.DepotId != null && c.ModelId != null)
                .GroupBy(c => (c.DepotId!, c.ModelId!))
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );

            _logger.LogInformation("Loaded current availability from CarEV: {GroupCount} groups", result.Count);
            return result;
        }

        /// <summary>
        /// Calculate quantile (e.g., P90) from sorted array
        /// Uses linear interpolation between two nearest values
        /// </summary>
        /// <param name="sortedValues">Pre-sorted array of values</param>
        /// <param name="p">Quantile (0.0 to 1.0, e.g., 0.90 for P90)</param>
        /// <returns>Quantile value</returns>
        private static double CalculateQuantile(double[] sortedValues, double p)
        {
            if (sortedValues == null || sortedValues.Length == 0)
            {
                return 0;
            }

            if (sortedValues.Length == 1)
            {
                return sortedValues[0];
            }

            // Position in array (0-based)
            var pos = (sortedValues.Length - 1) * p;
            var lo = (int)Math.Floor(pos);
            var hi = Math.Min(lo + 1, sortedValues.Length - 1);
            var frac = pos - lo;

            // Linear interpolation
            return sortedValues[lo] + (sortedValues[hi] - sortedValues[lo]) * frac;
        }

        public async Task<List<(string StationId, string VehicleType)>> GetStationVehicleTypesAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting all station-vehicle type combinations");

            var sql = @"
                SELECT DISTINCT station_id, vehicle_type
                FROM vw_rental_demand_30m_last_56d
                ORDER BY station_id, vehicle_type";

            var result = await _dbContext.Database
                .SqlQueryRaw<StationVehicleTypeRow>(sql)
                .ToListAsync(cancellationToken);

            return result.Select(r => (r.station_id, r.vehicle_type)).ToList();
        }

        public async Task<DemandStats?> GetStatsAsync(
            string stationId,
            string vehicleType,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Getting demand stats for station={StationId}, vehicle={VehicleType}, period={StartDate} to {EndDate}",
                stationId, vehicleType, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

            var sql = @"
                SELECT 
                    AVG(demand_count)::double precision AS mean_demand,
                    ARRAY_AGG(demand_count ORDER BY demand_count) AS demand_samples
                FROM vw_rental_demand_30m_last_56d
                WHERE station_id = {0}
                  AND vehicle_type = {1}
                  AND bin_ts >= {2}
                  AND bin_ts < {3}";

            var sqlParams = new object[] { stationId, vehicleType, startDate, endDate };
            
            var result = await _dbContext.Database
                .SqlQueryRaw<AggregatedDemandRow>(sql, sqlParams)
                .FirstOrDefaultAsync(cancellationToken);

            // If the aggregated array is missing or empty, try to synthesize samples
            // by fetching raw demand_count rows ordered by bin_ts. This ensures
            // downstream callers (including any LLM/forecasting adapters) receive
            // a non-empty `demand_samples` when possible.
            List<int>? rawSamples = null;
            if (result == null || result.demand_samples == null || result.demand_samples.Length == 0)
            {
                _logger.LogDebug(
                    "Aggregated demand_samples empty; attempting to fetch raw demand_count rows for station={StationId}, vehicle={VehicleType}",
                    stationId, vehicleType);

                var sampleSql = @"
                    SELECT demand_count
                    FROM vw_rental_demand_30m_last_56d
                    WHERE station_id = {0}
                      AND vehicle_type = {1}
                      AND bin_ts >= {2}
                      AND bin_ts < {3}
                    ORDER BY bin_ts";

                rawSamples = await _dbContext.Database
                    .SqlQueryRaw<int>(sampleSql, stationId, vehicleType, startDate, endDate)
                    .ToListAsync(cancellationToken);

                if (rawSamples == null || rawSamples.Count == 0)
                {
                    _logger.LogWarning(
                        "No demand data for station={StationId}, vehicle={VehicleType}",
                        stationId, vehicleType);
                    return null;
                }
            }

            // Use whichever samples we have: aggregated result first, otherwise rawSamples
            int[] samplesArray = result != null && result.demand_samples != null && result.demand_samples.Length > 0
                ? result.demand_samples
                : rawSamples!.ToArray();

            var sortedSamples = samplesArray.Select(x => (double)x).OrderBy(x => x).ToArray();
            var p90 = CalculateQuantile(sortedSamples, 0.90);

            double mean;
            if (result != null && result.mean_demand.HasValue && !double.IsNaN(result.mean_demand.Value) && result.mean_demand.Value > 0)
            {
                mean = result.mean_demand.Value;
            }
            else
            {
                mean = samplesArray.Length > 0 ? samplesArray.Average() : 0.0;
            }

            return new DemandStats
            {
                Mean = mean,
                P90 = p90
            };
        }

        public int GetRequiredUnits(double p90Demand, double avgTripHours, double turnaroundHours)
        {
            if (p90Demand <= 0) return 0;
            
            var cycleHours = avgTripHours + turnaroundHours;
            var required = Math.Ceiling(p90Demand * avgTripHours / cycleHours);
            return (int)required;
        }

        public async Task<int> LoadCurrentAvailabilityPeak24hAsync(
            string stationId,
            string vehicleType,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug(
                "Loading current availability for station={StationId}, vehicle={VehicleType}",
                stationId, vehicleType);

            // Try to get from InventorySnapshot first (if exists)
            var snapshotSql = @"
                SELECT COALESCE(MIN(""AvailableCount""), 0) AS min_available
                FROM ""InventorySnapshot""
                WHERE ""DepotId"" = {0}
                  AND ""ModelId"" = {1}
                  AND ""SnapshotTime"" >= NOW()
                  AND ""SnapshotTime"" < NOW() + INTERVAL '24 hours'";

            var snapshotResult = await _dbContext.Database
                .SqlQueryRaw<MinAvailableRow>(snapshotSql, stationId, vehicleType)
                .FirstOrDefaultAsync(cancellationToken);

            if (snapshotResult?.min_available > 0)
            {
                _logger.LogDebug("Found {Count} available from snapshots", snapshotResult.min_available);
                return snapshotResult.min_available;
            }

            // Fallback: Count current available cars from CarEV table
            _logger.LogDebug("No snapshots found, counting from CarEV table");
            var carCountSql = @"
                SELECT COUNT(*) AS min_available
                FROM ""CarEV"" c
                INNER JOIN ""Model"" m ON c.""ModelId"" = m.""Id""
                WHERE c.""DepotId"" = {0}
                  AND m.""Id"" = {1}
                  AND c.""Status"" = 0
                  AND c.""IsDeleted"" = FALSE";

            var carCountResult = await _dbContext.Database
                .SqlQueryRaw<MinAvailableRow>(carCountSql, stationId, vehicleType)
                .FirstOrDefaultAsync(cancellationToken);

            var availableCount = carCountResult?.min_available ?? 0;
            _logger.LogDebug("Found {Count} available cars from CarEV", availableCount);
            
            return availableCount;
        }

        // Helper classes for raw SQL queries
        private class StationVehicleTypeRow
        {
            public string station_id { get; set; } = null!;
            public string vehicle_type { get; set; } = null!;
        }

        private class AggregatedDemandRow
        {
            public double? mean_demand { get; set; }
            public int[]? demand_samples { get; set; }
        }

        private class MinAvailableRow
        {
            public int min_available { get; set; }
        }
    }
}
