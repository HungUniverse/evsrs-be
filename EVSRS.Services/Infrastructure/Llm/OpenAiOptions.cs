namespace EVSRS.Services.Infrastructure.Llm
{
    /// <summary>
    /// OpenAI configuration options
    /// </summary>
    public class OpenAiOptions
    {
        public const string SectionName = "OpenAI";

        /// <summary>
        /// OpenAI API key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// OpenAI model name (e.g., gpt-4o, gpt-4o-mini)
        /// </summary>
        public string ModelName { get; set; } = "gpt-4o-mini";

        /// <summary>
        /// API base URL (optional, defaults to OpenAI endpoint)
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Organization ID (optional)
        /// </summary>
        public string? OrganizationId { get; set; }
    }
}
