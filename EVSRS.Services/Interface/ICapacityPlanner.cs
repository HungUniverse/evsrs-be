using EVSRS.BusinessObjects.DTO.ForecastDto;

namespace EVSRS.Services.Interface;

/// <summary>
/// High-level capacity planning orchestrator.
/// Coordinates ForecastingService and LlmAdvisor to generate actionable capacity advice.
/// </summary>
public interface ICapacityPlanner
{
    /// <summary>
    /// Generate capacity advice for a specific date with given constraints.
    /// </summary>
    /// <param name="targetDate">Date to generate advice for (typically tomorrow or next week)</param>
    /// <param name="constraints">Planning constraints (budget, SLA, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validated capacity advice with actions and summary</returns>
    Task<CapacityAdviceResponse> GenerateAdviceAsync(
        DateTime targetDate,
        PlanningConstraints constraints,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Constraints for capacity planning optimization.
/// </summary>
public class PlanningConstraints
{
    /// <summary>
    /// Planning objective (e.g., "Minimize shortages during peak hours")
    /// </summary>
    public string Objective { get; set; } = "Minimize vehicle shortages during peak hours while staying within budget";

    /// <summary>
    /// Planning horizon in days (e.g., 7 for weekly, 30 for monthly)
    /// </summary>
    public int HorizonDays { get; set; } = 7;

    /// <summary>
    /// Average trip duration in hours (for required units calculation)
    /// </summary>
    public double AvgTripHours { get; set; } = 2.0;

    /// <summary>
    /// Turnaround time in hours (cleaning, maintenance between trips)
    /// </summary>
    public double TurnaroundHours { get; set; } = 1.0;

    /// <summary>
    /// Maximum budget for purchasing new vehicles (in currency units)
    /// </summary>
    public decimal? Budget { get; set; }

    /// <summary>
    /// Maximum number of vehicles that can be purchased per day
    /// </summary>
    public int? MaxDailyPurchase { get; set; }

    /// <summary>
    /// SLA target: max acceptable wait time in minutes
    /// </summary>
    public int SlaMinutes { get; set; } = 15;
}
