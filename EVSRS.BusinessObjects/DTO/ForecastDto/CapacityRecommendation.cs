using System;

namespace EVSRS.BusinessObjects.DTO.ForecastDto
{
    /// <summary>
    /// Capacity recommendation for a station/vehicle type
    /// </summary>
    public class CapacityRecommendation
    {
        public string StationId { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string VehicleTypeName { get; set; } = string.Empty;
        
        /// <summary>
        /// Peak P90 demand for this station/vehicle combination
        /// </summary>
        public double PeakP90Demand { get; set; }
        
        /// <summary>
        /// Time slot where peak P90 occurs
        /// </summary>
        public SlotKey? PeakSlot { get; set; }
        
        /// <summary>
        /// Required units to meet P90 demand with SLA
        /// Formula: ceil(P90 * avgTripHours / turnaroundHours)
        /// </summary>
        public int RequiredUnits { get; set; }
        
        /// <summary>
        /// Current available units (peak in next 24h)
        /// </summary>
        public int CurrentAvailablePeak24h { get; set; }
        
        /// <summary>
        /// Capacity gap (positive = need more, negative = surplus)
        /// Formula: max(0, required - current)
        /// </summary>
        public int Gap { get; set; }
        
        /// <summary>
        /// Whether SLA is met (gap <= 0)
        /// </summary>
        public bool SlaMet => Gap <= 0;
        
        /// <summary>
        /// Priority for rebalancing (based on gap and demand)
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// Recommended action
        /// </summary>
        public string? RecommendedAction { get; set; }
        
        /// <summary>
        /// Explanation/reason for recommendation
        /// </summary>
        public string? Reason { get; set; }
    }
}
