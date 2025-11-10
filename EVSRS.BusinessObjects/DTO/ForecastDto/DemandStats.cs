using System;
using System.Collections.Generic;

namespace EVSRS.BusinessObjects.DTO.ForecastDto
{
    /// <summary>
    /// Statistical summary of demand for a specific slot
    /// </summary>
    public class DemandStats
    {
        public SlotKey Slot { get; set; } = new SlotKey();
        
        /// <summary>
        /// Mean (average) demand across historical occurrences
        /// </summary>
        public double Mean { get; set; }
        
        /// <summary>
        /// 90th percentile demand (peak handling)
        /// </summary>
        public double P90 { get; set; }
        
        /// <summary>
        /// Minimum demand observed
        /// </summary>
        public double Min { get; set; }
        
        /// <summary>
        /// Maximum demand observed
        /// </summary>
        public double Max { get; set; }
        
        /// <summary>
        /// Number of historical data points used
        /// </summary>
        public int SampleCount { get; set; }
        
        /// <summary>
        /// All demand values for this slot (for P90 calculation)
        /// </summary>
        public List<double> DemandValues { get; set; } = new List<double>();
    }
}
