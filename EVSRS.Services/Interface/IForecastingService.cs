using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EVSRS.BusinessObjects.DTO.ForecastDto;

namespace EVSRS.Services.Interface
{
    /// <summary>
    /// Service for demand forecasting and capacity planning
    /// </summary>
    public interface IForecastingService
    {
        /// <summary>
        /// Get all unique station-vehicle type combinations from historical data
        /// </summary>
        Task<List<(string StationId, string VehicleType)>> GetStationVehicleTypesAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get demand statistics for a specific station and vehicle type
        /// </summary>
        Task<DemandStats?> GetStatsAsync(
            string stationId,
            string vehicleType,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get demand statistics from historical data (last 56 days)
        /// Groups by station, vehicle type, day-of-week, hour, and minute (30-min bins)
        /// Calculates Mean and P90 for each slot
        /// </summary>
        /// <param name="stationIds">Optional filter by station IDs</param>
        /// <param name="vehicleTypes">Optional filter by vehicle type IDs</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary keyed by SlotKey with demand statistics</returns>
        Task<Dictionary<SlotKey, DemandStats>> GetStatsAsync(
            List<string>? stationIds = null,
            List<string>? vehicleTypes = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Calculate required units based on P90 demand
        /// Formula: required = ceil(P90Demand * avgTripHours / turnaroundHours)
        /// </summary>
        int GetRequiredUnits(double p90Demand, double avgTripHours, double turnaroundHours);

        /// <summary>
        /// Load current vehicle availability for a specific station and vehicle type during peak hours (last 24h)
        /// </summary>
        Task<int> LoadCurrentAvailabilityPeak24hAsync(
            string stationId,
            string vehicleType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculate required units and capacity gaps based on demand stats
        /// Formula: required = ceil(maxP90 * avgTripHours / turnaroundHours)
        /// Gap = max(0, required - currentAvailable)
        /// </summary>
        /// <param name="stats">Demand statistics from GetStatsAsync</param>
        /// <param name="avgTripHours">Average trip duration in hours (default 4.0)</param>
        /// <param name="turnaroundHours">Vehicle turnaround time in hours (default 0.5)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of capacity recommendations per station/vehicle type</returns>
        Task<List<CapacityRecommendation>> GetRequiredUnitsAsync(
            Dictionary<SlotKey, DemandStats> stats,
            double avgTripHours = 4.0,
            double turnaroundHours = 0.5,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Load current vehicle availability peak for next 24 hours
        /// Returns minimum available count per station/vehicle type in the next 24h window
        /// </summary>
        /// <param name="stationIds">Optional filter by station IDs</param>
        /// <param name="vehicleTypes">Optional filter by vehicle type IDs</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dictionary keyed by (stationId, vehicleType) with min available count</returns>
        Task<Dictionary<(string StationId, string VehicleType), int>> LoadCurrentAvailabilityPeak24hAsync(
            List<string>? stationIds = null,
            List<string>? vehicleTypes = null,
            CancellationToken cancellationToken = default);
    }
}
