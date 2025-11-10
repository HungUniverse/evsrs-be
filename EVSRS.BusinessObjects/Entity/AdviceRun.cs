using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVSRS.BusinessObjects.Entity;

/// <summary>
/// Audit trail for capacity planning advice runs.
/// Stores inputs and outputs for compliance, debugging, and analytics.
/// </summary>
[Table("advice_runs")]
public class AdviceRun
{
    /// <summary>
    /// Unique identifier for this advice run
    /// </summary>
    [Key]
    [Column("run_id")]
    public Guid RunId { get; set; }

    /// <summary>
    /// When this advice was generated (UTC)
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Input parameters as JSON (target date, constraints, baseline recommendations)
    /// </summary>
    [Column("inputs", TypeName = "jsonb")]
    public string Inputs { get; set; } = null!;

    /// <summary>
    /// Generated advice output as JSON (actions, summary)
    /// </summary>
    [Column("output", TypeName = "jsonb")]
    public string Output { get; set; } = null!;

    /// <summary>
    /// Processing latency in milliseconds
    /// </summary>
    [Column("latency_ms")]
    public int? LatencyMs { get; set; }

    /// <summary>
    /// SHA256 hash of inputs for quick duplicate detection
    /// </summary>
    [Column("input_hash")]
    [MaxLength(64)]
    public string? InputHash { get; set; }
}
