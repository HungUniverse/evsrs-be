using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EVSRS.BusinessObjects.DTO.ForecastDto;

namespace EVSRS.Services.Infrastructure.Llm
{
    /// <summary>
    /// LLM-based capacity advisor
    /// </summary>
    public interface ILlmAdvisor
    {
        /// <summary>
        /// Get capacity advice from LLM based on forecasts and constraints
        /// </summary>
        /// <param name="objective">Business objective description</param>
        /// <param name="horizonDays">Planning horizon in days</param>
        /// <param name="avgTripHours">Average trip duration in hours</param>
        /// <param name="turnaroundHours">Turnaround time between trips in hours</param>
        /// <param name="budget">Budget constraint</param>
        /// <param name="maxDailyPurchase">Maximum units to purchase per day</param>
        /// <param name="slaMinutes">SLA target in minutes</param>
        /// <param name="baseline">Baseline capacity recommendations</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>LLM-generated capacity advice</returns>
        Task<CapacityAdviceResponse> GetAdviceAsync(
            string objective,
            int horizonDays,
            double avgTripHours,
            double turnaroundHours,
            decimal budget,
            int maxDailyPurchase,
            int slaMinutes,
            List<CapacityRecommendation> baseline,
            CancellationToken cancellationToken = default);
    }
}
