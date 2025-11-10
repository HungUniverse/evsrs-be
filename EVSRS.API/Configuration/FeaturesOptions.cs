namespace EVSRS.API.Configuration;

/// <summary>
/// Feature flags for enabling/disabling features
/// </summary>
public class FeaturesOptions
{
    public const string SectionName = "Features";

    /// <summary>
    /// Enable Forecasting and Capacity Planning APIs
    /// </summary>
    public bool ForecastCapacity { get; set; }
}
