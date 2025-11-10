using System;

namespace EVSRS.BusinessObjects.DTO.ForecastDto
{
    /// <summary>
    /// Represents a time slot identifier for demand grouping
    /// </summary>
    public class SlotKey
    {
        /// <summary>
        /// Station/Depot ID
        /// </summary>
        public string StationId { get; set; } = string.Empty;
        
        /// <summary>
        /// Vehicle model/type ID
        /// </summary>
        public string VehicleType { get; set; } = string.Empty;
        
        /// <summary>
        /// Day of week (0=Sunday, 6=Saturday)
        /// </summary>
        public int DayOfWeek { get; set; }
        
        /// <summary>
        /// Hour of day (0-23)
        /// </summary>
        public int Hour { get; set; }
        
        /// <summary>
        /// Minute bucket (0 or 30)
        /// </summary>
        public int Minute { get; set; }
        
        /// <summary>
        /// Creates a composite key for grouping
        /// </summary>
        public string GetCompositeKey() => $"{StationId}|{VehicleType}|{DayOfWeek}|{Hour:D2}:{Minute:D2}";
        
        public override bool Equals(object? obj)
        {
            if (obj is not SlotKey other) return false;
            return StationId == other.StationId 
                   && VehicleType == other.VehicleType 
                   && DayOfWeek == other.DayOfWeek 
                   && Hour == other.Hour 
                   && Minute == other.Minute;
        }
        
        public override int GetHashCode() => GetCompositeKey().GetHashCode();
        
        public override string ToString() => GetCompositeKey();
    }
}
