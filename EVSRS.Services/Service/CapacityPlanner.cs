using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.DTO.ForecastDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Services.Infrastructure.Llm;
using EVSRS.Services.Interface;
using Microsoft.Extensions.Logging;

namespace EVSRS.Services.Service
{
    /// <summary>
    /// Planner tính toán nhu cầu và đề xuất hành động (mua/lease/rebalance) dựa trên forecast và inventory.
    /// </summary>
   
/// High-level capacity planning orchestrator.
/// Coordinates forecasting, LLM advisory, and audit logging.
/// </summary>
public class CapacityPlanner : ICapacityPlanner

    {
        private readonly IForecastingService _forecastingService;
        private readonly ILlmAdvisor _llmAdvisor;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<CapacityPlanner> _logger;

        public CapacityPlanner(
            IForecastingService forecastingService,
            ILlmAdvisor llmAdvisor,
            ApplicationDbContext dbContext,
            ILogger<CapacityPlanner> logger)
        {
            _forecastingService = forecastingService;
            _llmAdvisor = llmAdvisor;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<CapacityAdviceResponse> GenerateAdviceAsync(
            DateTime targetDate,
            PlanningConstraints constraints,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var runId = Guid.NewGuid();

            try
            {
                _logger.LogInformation(
                    "Starting capacity planning run {RunId} for date {TargetDate}",
                    runId, targetDate.ToString("yyyy-MM-dd"));

                // Step 1: Get baseline recommendations from ForecastingService
                _logger.LogDebug("Step 1: Loading baseline capacity recommendations");
                var baseline = await GetBaselineRecommendationsAsync(
                    targetDate,
                    constraints,
                    cancellationToken);

                _logger.LogInformation(
                    "Loaded {Count} baseline recommendations (shortages: {Shortages}, surplus: {Surplus})",
                    baseline.Count,
                    baseline.Count(b => b.Gap > 0),
                    baseline.Count(b => b.Gap < 0));

                // Step 2: Get LLM-enhanced advice
                _logger.LogDebug("Step 2: Invoking LLM advisor");
                var advice = await _llmAdvisor.GetAdviceAsync(
                    objective: constraints.Objective,
                    horizonDays: constraints.HorizonDays,
                    avgTripHours: constraints.AvgTripHours,
                    turnaroundHours: constraints.TurnaroundHours,
                    budget: constraints.Budget ?? 0m,
                    maxDailyPurchase: constraints.MaxDailyPurchase ?? 999,
                    slaMinutes: constraints.SlaMinutes,
                    baseline: baseline,
                    cancellationToken: cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "Capacity planning completed in {LatencyMs}ms. Generated {ActionCount} actions affecting {StationsAffected} stations",
                    stopwatch.ElapsedMilliseconds,
                    advice.Actions.Count,
                    advice.Summary.StationsAffected);

                // Step 3: Save audit trail
                await SaveAuditTrailAsync(
                    runId,
                    targetDate,
                    constraints,
                    baseline,
                    advice,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken);

                return advice;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Capacity planning run {RunId} failed after {LatencyMs}ms",
                    runId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// Get baseline capacity recommendations using forecasting service.
        /// </summary>
        private async Task<List<CapacityRecommendation>> GetBaselineRecommendationsAsync(
            DateTime targetDate,
            PlanningConstraints constraints,
            CancellationToken cancellationToken)
        {
            // Get all unique station-vehicle combinations from historical data
            var stationVehicleTypes = await _forecastingService.GetStationVehicleTypesAsync(cancellationToken);

            var recommendations = new List<CapacityRecommendation>();

            foreach (var (stationId, vehicleType) in stationVehicleTypes)
            {
                // Get demand statistics for this combination
                var stats = await _forecastingService.GetStatsAsync(
                    stationId,
                    vehicleType,
                    targetDate.AddDays(-constraints.HorizonDays),
                    targetDate,
                    cancellationToken);

                if (stats == null)
                {
                    _logger.LogWarning(
                        "No demand data for station {StationId} vehicle type {VehicleType}",
                        stationId, vehicleType);
                    continue;
                }

                // Calculate required units based on P90 demand
                var requiredUnits = _forecastingService.GetRequiredUnits(
                    stats.P90,
                    constraints.AvgTripHours,
                    constraints.TurnaroundHours);

                // Get current availability during peak hours (last 24h)
                var currentAvailable = await _forecastingService.LoadCurrentAvailabilityPeak24hAsync(
                    stationId,
                    vehicleType,
                    cancellationToken);

                // Calculate gap and determine recommendation
                var gap = requiredUnits - currentAvailable;
                var recommendedAction = gap switch
                {
                    > 0 => "BUY",
                    < 0 => "SURPLUS",
                    _ => "OK"
                };

                // Calculate priority (0-100, higher = more urgent)
                var priority = CalculatePriority(gap, stats.P90, constraints.SlaMinutes);

                recommendations.Add(new CapacityRecommendation
                {
                    StationId = stationId,
                    VehicleType = vehicleType,
                    RequiredUnits = requiredUnits,
                    CurrentAvailablePeak24h = currentAvailable,
                    PeakP90Demand = stats.P90,
                    Gap = gap,
                    Priority = priority,
                    RecommendedAction = recommendedAction
                });
            }

            return recommendations
                .OrderByDescending(r => r.Priority)
                .ToList();
        }

        /// <summary>
        /// Calculate priority score (0-100) based on gap severity and demand.
        /// Higher score = more urgent.
        /// </summary>
        private int CalculatePriority(int gap, double peakDemand, int slaMinutes)
        {
            if (gap <= 0)
                return 0; // No shortage

            // Base priority on shortage severity
            var shortageRatio = gap / Math.Max(peakDemand, 1.0);
            var basePriority = Math.Min(shortageRatio * 100, 100);

            // Boost priority if SLA is tight
            var slaBoost = slaMinutes <= 10 ? 20 : (slaMinutes <= 15 ? 10 : 0);

            return (int)Math.Min(basePriority + slaBoost, 100);
        }

        /// <summary>
        /// Save audit trail to advice_runs table for compliance and analytics.
        /// </summary>
        private async Task SaveAuditTrailAsync(
            Guid runId,
            DateTime targetDate,
            PlanningConstraints constraints,
            List<CapacityRecommendation> baseline,
            CapacityAdviceResponse advice,
            long latencyMs,
            CancellationToken cancellationToken)
        {
            try
            {
                // Prepare inputs object
                var inputs = new
                {
                    targetDate = targetDate.ToString("yyyy-MM-dd"),
                    constraints,
                    baselineCount = baseline.Count,
                    baselineSummary = new
                    {
                        totalShortages = baseline.Sum(b => Math.Max(b.Gap, 0)),
                        totalSurplus = baseline.Sum(b => Math.Abs(Math.Min(b.Gap, 0))),
                        highPriorityCount = baseline.Count(b => b.Priority >= 70)
                    }
                };

                // Prepare truncated output (don't store full rationales to save space)
                var outputTruncated = new
                {
                    actionCount = advice.Actions.Count,
                    actions = advice.Actions.Select(a => new
                    {
                        a.StationId,
                        a.VehicleType,
                        a.ActionType,
                        a.Units,
                        a.Priority,
                        rationaleLength = a.Rationale?.Length ?? 0,
                        a.EstimatedCost,
                        a.RelatedStationId
                    }).ToList(),
                    summary = advice.Summary
                };

                var inputsJson = JsonSerializer.Serialize(inputs, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var outputJson = JsonSerializer.Serialize(outputTruncated, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Calculate input hash for duplicate detection
                var inputHash = ComputeSha256Hash(inputsJson);

                var adviceRun = new AdviceRun
                {
                    RunId = runId,
                    CreatedAt = DateTime.UtcNow,
                    Inputs = inputsJson,
                    Output = outputJson,
                    LatencyMs = (int)latencyMs,
                    InputHash = inputHash
                };

                _dbContext.AdviceRuns.Add(adviceRun);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Saved audit trail for run {RunId} (input hash: {InputHash})",
                    runId, inputHash);
            }
            catch (Exception ex)
            {
                // Don't fail the entire operation if audit logging fails
                _logger.LogError(ex,
                    "Failed to save audit trail for run {RunId}",
                    runId);
            }
        }

        /// <summary>
        /// Compute SHA256 hash of input string for duplicate detection.
        /// </summary>
        private static string ComputeSha256Hash(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
