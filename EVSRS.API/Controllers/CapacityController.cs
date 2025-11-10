using EVSRS.API.Configuration;
using EVSRS.API.Constant;
using EVSRS.API.Services;
using EVSRS.BusinessObjects.DTO.ForecastDto;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EVSRS.API.Controllers;

/// <summary>
/// Capacity planning and AI-powered advice generation
/// </summary>
[ApiController]
[Route(ApiEndPointConstant.Capacity.CapacityEndpoint)]
[Authorize]
public class CapacityController : ControllerBase
{
    private readonly ICapacityPlanner _capacityPlanner;
    private readonly IConstraintsCache _constraintsCache;
    private readonly FeaturesOptions _features;
    private readonly ILogger<CapacityController> _logger;

    public CapacityController(
        ICapacityPlanner capacityPlanner,
        IConstraintsCache constraintsCache,
        IOptions<FeaturesOptions> features,
        ILogger<CapacityController> logger)
    {
        _capacityPlanner = capacityPlanner;
        _constraintsCache = constraintsCache;
        _features = features.Value;
        _logger = logger;
    }

    /// <summary>
    /// Store planning constraints temporarily in cache
    /// </summary>
    /// <param name="request">Planning constraints</param>
    /// <returns>Confirmation with cache key</returns>
    /// <response code="200">Constraints saved successfully</response>
    /// <response code="404">Feature disabled</response>
    [HttpPost("constraints")]
    [ProducesResponseType(typeof(ConstraintsSavedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ConstraintsSavedResponse> SaveConstraints([FromBody] SaveConstraintsRequest request)
    {
        if (!_features.ForecastCapacity)
        {
            _logger.LogWarning("Capacity API called but feature is disabled");
            return NotFound(new { error = "Capacity planning feature is not enabled" });
        }

        var cacheKey = request.Key ?? Guid.NewGuid().ToString();
        var expiration = TimeSpan.FromHours(request.ExpirationHours ?? 24);

        _constraintsCache.Set(cacheKey, request.Constraints, expiration);

        _logger.LogInformation(
            "Saved planning constraints with key={Key}, expires in {Hours}h",
            cacheKey, expiration.TotalHours);

        return Ok(new ConstraintsSavedResponse
        {
            Key = cacheKey,
            ExpiresAt = DateTime.UtcNow.Add(expiration),
            Message = "Constraints saved successfully"
        });
    }

    /// <summary>
    /// Get cached planning constraints
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Planning constraints</returns>
    /// <response code="200">Returns cached constraints</response>
    /// <response code="404">Key not found or expired</response>
    [HttpGet("constraints/{key}")]
    [ProducesResponseType(typeof(PlanningConstraints), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PlanningConstraints> GetConstraints([FromRoute] string key)
    {
        if (!_features.ForecastCapacity)
        {
            return NotFound(new { error = "Capacity planning feature is not enabled" });
        }

        if (!_constraintsCache.TryGet(key, out var constraints) || constraints == null)
        {
            return NotFound(new { error = "Constraints not found or expired" });
        }

        return Ok(constraints);
    }

    /// <summary>
    /// Generate AI-powered capacity advice for a specific date
    /// </summary>
    /// <param name="date">Target date (YYYY-MM-DD format). Defaults to tomorrow.</param>
    /// <param name="constraintsKey">Optional cache key for pre-saved constraints</param>
    /// <param name="objective">Planning objective</param>
    /// <param name="horizonDays">Planning horizon in days (default: 7)</param>
    /// <param name="avgTripHours">Average trip duration in hours (default: 2.0)</param>
    /// <param name="turnaroundHours">Turnaround time in hours (default: 1.0)</param>
    /// <param name="budget">Maximum budget for purchases</param>
    /// <param name="maxDailyPurchase">Maximum vehicles to purchase per day</param>
    /// <param name="slaMinutes">SLA target in minutes (default: 15)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Capacity advice with actions and summary</returns>
    /// <response code="200">Returns capacity advice</response>
    /// <response code="404">Feature disabled</response>
    [HttpGet("advice")]
    [ProducesResponseType(typeof(CapacityAdviceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CapacityAdviceResponse>> GetAdvice(
        [FromQuery] string? date = null,
        [FromQuery] string? constraintsKey = null,
        [FromQuery] string? objective = null,
        [FromQuery] int? horizonDays = null,
        [FromQuery] double? avgTripHours = null,
        [FromQuery] double? turnaroundHours = null,
        [FromQuery] decimal? budget = null,
        [FromQuery] int? maxDailyPurchase = null,
        [FromQuery] int? slaMinutes = null,
        CancellationToken cancellationToken = default)
    {
        if (!_features.ForecastCapacity)
        {
            _logger.LogWarning("Capacity advice API called but feature is disabled");
            return NotFound(new { error = "Capacity planning feature is not enabled" });
        }

        // Parse target date
        DateTime targetDate;
        if (string.IsNullOrEmpty(date))
        {
            targetDate = DateTime.UtcNow.Date.AddDays(1); // Default to tomorrow
        }
        else if (!DateTime.TryParse(date, out targetDate))
        {
            return BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD." });
        }

        // Get constraints from cache or query parameters
        PlanningConstraints constraints;
        if (!string.IsNullOrEmpty(constraintsKey))
        {
            if (!_constraintsCache.TryGet(constraintsKey, out var cached) || cached == null)
            {
                return BadRequest(new { error = "Constraints key not found or expired" });
            }
            constraints = cached;
            
            // Override with query params if provided
            if (!string.IsNullOrEmpty(objective)) constraints.Objective = objective;
            if (horizonDays.HasValue) constraints.HorizonDays = horizonDays.Value;
            if (avgTripHours.HasValue) constraints.AvgTripHours = avgTripHours.Value;
            if (turnaroundHours.HasValue) constraints.TurnaroundHours = turnaroundHours.Value;
            if (budget.HasValue) constraints.Budget = budget.Value;
            if (maxDailyPurchase.HasValue) constraints.MaxDailyPurchase = maxDailyPurchase.Value;
            if (slaMinutes.HasValue) constraints.SlaMinutes = slaMinutes.Value;
        }
        else
        {
            // Build from query parameters
            constraints = new PlanningConstraints
            {
                Objective = objective ?? "Minimize vehicle shortages during peak hours while staying within budget",
                HorizonDays = horizonDays ?? 7,
                AvgTripHours = avgTripHours ?? 2.0,
                TurnaroundHours = turnaroundHours ?? 1.0,
                Budget = budget,
                MaxDailyPurchase = maxDailyPurchase,
                SlaMinutes = slaMinutes ?? 15
            };
        }

        _logger.LogInformation(
            "Generating capacity advice for date={Date}, horizon={Horizon}, budget={Budget}",
            targetDate.ToString("yyyy-MM-dd"), constraints.HorizonDays, constraints.Budget);

        try
        {
            var advice = await _capacityPlanner.GenerateAdviceAsync(
                targetDate,
                constraints,
                cancellationToken);

            return Ok(advice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate capacity advice");
            return StatusCode(500, new { error = "Failed to generate advice", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete cached constraints
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Confirmation</returns>
    [HttpDelete("constraints/{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult DeleteConstraints([FromRoute] string key)
    {
        if (!_features.ForecastCapacity)
        {
            return NotFound(new { error = "Capacity planning feature is not enabled" });
        }

        _constraintsCache.Remove(key);
        _logger.LogInformation("Deleted constraints with key={Key}", key);
        
        return NoContent();
    }
}

/// <summary>
/// Request to save planning constraints
/// </summary>
public class SaveConstraintsRequest
{
    /// <summary>
    /// Optional cache key (will be generated if not provided)
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Planning constraints to save
    /// </summary>
    public PlanningConstraints Constraints { get; set; } = null!;

    /// <summary>
    /// Expiration time in hours (default: 24)
    /// </summary>
    public double? ExpirationHours { get; set; }
}

/// <summary>
/// Response after saving constraints
/// </summary>
public class ConstraintsSavedResponse
{
    /// <summary>
    /// Cache key to retrieve constraints later
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// When the cached constraints will expire
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Confirmation message
    /// </summary>
    public string Message { get; set; } = null!;
}
