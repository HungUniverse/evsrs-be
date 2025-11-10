using System.Collections.Generic;

namespace EVSRS.BusinessObjects.DTO.ForecastDto
{
    /// <summary>
    /// LLM-generated capacity advice response
    /// </summary>
    public class CapacityAdviceResponse
    {
        public List<CapacityAction> Actions { get; set; } = new();
        public AdviceSummary Summary { get; set; } = new();
    }

    /// <summary>
    /// Individual capacity action recommended by LLM
    /// </summary>
    public class CapacityAction
    {
        public string StationId { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // BUY, REALLOCATE_IN, REALLOCATE_OUT, SURPLUS, NO_ACTION
        public int Units { get; set; }
        public int Priority { get; set; }
        public string Rationale { get; set; } = string.Empty;
        public decimal? EstimatedCost { get; set; }
        public string? RelatedStationId { get; set; }
    }

    /// <summary>
    /// Summary of capacity advice
    /// </summary>
    public class AdviceSummary
    {
        public decimal TotalCost { get; set; }
        public int StationsAffected { get; set; }
        public int UnitsAdded { get; set; }
        public int UnitsReallocated { get; set; }
        public decimal? BudgetRemaining { get; set; }
        public string? Notes { get; set; }
    }
}
