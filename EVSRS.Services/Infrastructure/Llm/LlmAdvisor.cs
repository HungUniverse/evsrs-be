using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EVSRS.BusinessObjects.DTO.ForecastDto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EVSRS.Services.Infrastructure.Llm
{
    /// <summary>
    /// LLM-based capacity advisor using OpenAI Chat Completions
    /// </summary>
    public class LlmAdvisor : ILlmAdvisor
    {
        private readonly OpenAiOptions _options;
        private readonly ILogger<LlmAdvisor> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _jsonSchema;

        private const string SystemPrompt = @"Bạn là cố vấn vận hành đội xe trạm-based.
Mục tiêu: giảm thiếu xe giờ cao điểm, tránh dư thừa, tuân thủ ngân sách.
Luôn trả về JSON đúng với schema CapacityAdviceResponse đã cung cấp.
Không thêm bất kỳ giải thích nào ngoài JSON.
Ưu tiên REALLOCATE nếu tổng xe hiện có giữa các trạm đủ để cân bằng giờ cao điểm.";

        public LlmAdvisor(
            IOptions<OpenAiOptions> options,
            ILogger<LlmAdvisor> logger,
            IHttpClientFactory httpClientFactory)
        {
            _options = options.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("OpenAI");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Load JSON schema
            var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schemas", "CapacityAdvice.schema.json");
            if (File.Exists(schemaPath))
            {
                _jsonSchema = File.ReadAllText(schemaPath);
            }
            else
            {
                _logger.LogWarning("JSON schema not found at {Path}, using inline schema", schemaPath);
                _jsonSchema = GetInlineSchema();
            }
        }

        public async Task<CapacityAdviceResponse> GetAdviceAsync(
            string objective,
            int horizonDays,
            double avgTripHours,
            double turnaroundHours,
            decimal budget,
            int maxDailyPurchase,
            int slaMinutes,
            List<CapacityRecommendation> baseline,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Requesting LLM advice for {Count} baseline recommendations", baseline.Count);

                // Build user payload
                var userPayload = new
                {
                    objective,
                    horizonDays,
                    assumptions = new
                    {
                        avgTripHours,
                        turnaroundHours
                    },
                    constraints = new
                    {
                        budget,
                        maxDailyPurchase,
                        slaMinutes
                    },
                    baseline = baseline.Select(b => new
                    {
                        stationId = b.StationId,
                        vehicleType = b.VehicleType,
                        requiredUnits = b.RequiredUnits,
                        currentAvailablePeak24h = b.CurrentAvailablePeak24h,
                        peakP90Demand = b.PeakP90Demand,
                        gap = b.Gap,
                        priority = b.Priority,
                        recommendedAction = b.RecommendedAction
                    }).ToList()
                };

                var userMessage = JsonSerializer.Serialize(userPayload, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Build OpenAI request
                var requestBody = new
                {
                    model = _options.ModelName,
                    temperature = 0.2,
                    messages = new[]
                    {
                        new { role = "system", content = SystemPrompt },
                        new { role = "user", content = userMessage }
                    },
                    response_format = new
                    {
                        type = "json_schema",
                        json_schema = new
                        {
                            name = "capacity_advice_response",
                            strict = true,
                            schema = JsonSerializer.Deserialize<object>(_jsonSchema)
                        }
                    }
                };

                var requestJson = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
                {
                    Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
                if (!string.IsNullOrEmpty(_options.OrganizationId))
                {
                    request.Headers.Add("OpenAI-Organization", _options.OrganizationId);
                }

                // Send request
                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("OpenAI response: {Response}", responseContent);

                // Parse response
                var openAiResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (openAiResponse?.Choices == null || openAiResponse.Choices.Count == 0)
                {
                    throw new InvalidOperationException("OpenAI returned empty response");
                }

                var assistantMessage = openAiResponse.Choices[0].Message.Content;
                
                // Validate and parse JSON
                var advice = ValidateAndParseAdvice(assistantMessage);
                
                _logger.LogInformation("LLM advice received: {Actions} actions, total cost {Cost}", 
                    advice.Actions.Count, advice.Summary.TotalCost);

                return advice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get LLM advice, using fallback");
                return GenerateFallbackAdvice(baseline);
            }
        }

        private CapacityAdviceResponse ValidateAndParseAdvice(string json)
        {
            try
            {
                var advice = JsonSerializer.Deserialize<CapacityAdviceResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (advice == null || advice.Actions == null || advice.Summary == null)
                {
                    throw new InvalidOperationException("Invalid JSON structure");
                }

                // Basic validation
                if (advice.Actions.Any(a => string.IsNullOrEmpty(a.StationId) || 
                                            string.IsNullOrEmpty(a.VehicleType) || 
                                            string.IsNullOrEmpty(a.ActionType)))
                {
                    throw new InvalidOperationException("Invalid action in response");
                }

                return advice;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate LLM JSON response: {Json}", json);
                throw;
            }
        }

        private CapacityAdviceResponse GenerateFallbackAdvice(List<CapacityRecommendation> baseline)
        {
            _logger.LogInformation("Generating fallback advice for {Count} recommendations", baseline.Count);

            var actions = baseline
                .Where(b => b.Gap > 0)
                .Select(b => new CapacityAction
                {
                    StationId = b.StationId,
                    VehicleType = b.VehicleType,
                    ActionType = "BUY",
                    Units = b.Gap,
                    Priority = b.Priority,
                    Rationale = $"Fallback: Purchase {b.Gap} units to meet demand (P90={b.PeakP90Demand:F1}, Current={b.CurrentAvailablePeak24h})",
                    EstimatedCost = null
                })
                .OrderByDescending(a => a.Priority)
                .ToList();

            var summary = new AdviceSummary
            {
                TotalCost = 0, // Unknown in fallback
                StationsAffected = actions.Select(a => a.StationId).Distinct().Count(),
                UnitsAdded = actions.Sum(a => a.Units),
                UnitsReallocated = 0,
                Notes = "Fallback advice: LLM unavailable, converted all gaps to BUY actions"
            };

            return new CapacityAdviceResponse
            {
                Actions = actions,
                Summary = summary
            };
        }

        private string GetInlineSchema()
        {
            // Fallback inline schema if file not found
            return @"{
                ""type"": ""object"",
                ""required"": [""actions"", ""summary""],
                ""properties"": {
                    ""actions"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""object"",
                            ""required"": [""stationId"", ""vehicleType"", ""actionType"", ""units"", ""priority"", ""rationale""],
                            ""properties"": {
                                ""stationId"": { ""type"": ""string"" },
                                ""vehicleType"": { ""type"": ""string"" },
                                ""actionType"": { ""type"": ""string"", ""enum"": [""BUY"", ""REALLOCATE_IN"", ""REALLOCATE_OUT"", ""SURPLUS"", ""NO_ACTION""] },
                                ""units"": { ""type"": ""integer"", ""minimum"": 0 },
                                ""priority"": { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 100 },
                                ""rationale"": { ""type"": ""string"" },
                                ""estimatedCost"": { ""type"": ""number"", ""minimum"": 0 },
                                ""relatedStationId"": { ""type"": ""string"" }
                            }
                        }
                    },
                    ""summary"": {
                        ""type"": ""object"",
                        ""required"": [""totalCost"", ""stationsAffected"", ""unitsAdded"", ""unitsReallocated""],
                        ""properties"": {
                            ""totalCost"": { ""type"": ""number"", ""minimum"": 0 },
                            ""stationsAffected"": { ""type"": ""integer"", ""minimum"": 0 },
                            ""unitsAdded"": { ""type"": ""integer"", ""minimum"": 0 },
                            ""unitsReallocated"": { ""type"": ""integer"", ""minimum"": 0 },
                            ""budgetRemaining"": { ""type"": ""number"" },
                            ""notes"": { ""type"": ""string"" }
                        }
                    }
                }
            }";
        }
    }

    // OpenAI API response models
    internal class OpenAiChatResponse
    {
        public List<ChatChoice> Choices { get; set; } = new();
    }

    internal class ChatChoice
    {
        public ChatMessage Message { get; set; } = new();
    }

    internal class ChatMessage
    {
        public string Content { get; set; } = string.Empty;
    }
}
