using EVSRS.BusinessObjects.Base;
using System;

namespace EVSRS.BusinessObjects.Entity
{
    /// <summary>
    /// Represents a time-series snapshot of vehicle inventory at a depot
    /// Used for capacity planning and demand forecasting
    /// </summary>
    public class InventorySnapshot : BaseEntity
    {
        public string DepotId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        
        /// <summary>
        /// Timestamp of this snapshot (typically rounded to 30-minute intervals)
        /// </summary>
        public DateTime SnapshotTime { get; set; }
        
        /// <summary>
        /// Number of vehicles available for rental
        /// </summary>
        public int AvailableCount { get; set; }
        
        /// <summary>
        /// Number of vehicles currently charging
        /// </summary>
        public int ChargingCount { get; set; }
        
        /// <summary>
        /// Number of vehicles in maintenance
        /// </summary>
        public int MaintenanceCount { get; set; }
        
        /// <summary>
        /// Number of vehicles currently in use by customers
        /// </summary>
        public int InUseCount { get; set; }
        
        /// <summary>
        /// Number of vehicles reserved but not yet picked up
        /// </summary>
        public int ReservedCount { get; set; }
        
        // Navigation properties
        public virtual Depot? Depot { get; set; }
        public virtual Model? Model { get; set; }
    }
}
