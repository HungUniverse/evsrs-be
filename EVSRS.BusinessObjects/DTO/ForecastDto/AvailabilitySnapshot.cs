using System;

namespace EVSRS.BusinessObjects.DTO.ForecastDto
{
    /// <summary>
    /// Current vehicle availability snapshot
    /// </summary>
    public class AvailabilitySnapshot
    {
        public string StationId { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public DateTime SnapshotTime { get; set; }
        public int AvailableCount { get; set; }
        public int ChargingCount { get; set; }
        public int MaintenanceCount { get; set; }
        public int InUseCount { get; set; }
        public int ReservedCount { get; set; }
        
        /// <summary>
        /// Total operational units (available + charging + reserved)
        /// </summary>
        public int TotalOperational => AvailableCount + ChargingCount + ReservedCount;
    }
}
