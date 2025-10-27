using EVSRS.BusinessObjects.DTO.SepayDto;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.ExternalServices.SepayService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EVSRS.API.Controllers
{
    [ApiController]
    [Route("api/sepay-auth")]
    public class SepayController : ControllerBase
    {
        private readonly ISepayService _sepayService;
        private readonly ILogger<SepayController> _logger;

    public SepayController(ISepayService sepayService, ILogger<SepayController> logger)
    {
        _sepayService = sepayService;
        _logger = logger;
    }
    
    [HttpGet("status/{orderId}")]
    public async Task<IActionResult> GetPaymentStatus(string orderId)
    {
        var status = await _sepayService.GetPaymentStatusAsync(orderId);
        return Ok(new ResponseModel<string>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            status
        ));
    }

    [HttpPost("hooks/payment")]
    public async Task<IActionResult> ProcessPaymentWebhook(
        [FromBody] SepayWebhookPayload payload)
    {
        // Log all headers to debug the issue
        _logger.LogInformation("=== SePay Webhook Debug Info ===");
        _logger.LogInformation("Request Headers:");
        foreach (var header in Request.Headers)
        {
            _logger.LogInformation("Header: {Key} = {Value}", header.Key, header.Value);
        }

        // Try to get auth header from different possible names
        string? authHeader = null;
        var authHeaderNames = new[] { "Authorization", "Apikey", "API-Key", "X-API-Key", "Auth" };
        
        foreach (var headerName in authHeaderNames)
        {
            if (Request.Headers.TryGetValue(headerName, out var headerValue))
            {
                authHeader = headerValue.FirstOrDefault();
                _logger.LogInformation("Found auth header '{HeaderName}': {HeaderValue}", headerName, authHeader);
                break;
            }
        }

        if (string.IsNullOrEmpty(authHeader))
        {
            _logger.LogWarning("No authorization header found in any expected format");
            authHeader = string.Empty;
        }

        await _sepayService.ProcessPaymentWebhookAsync(payload, authHeader);
        return Ok(new ResponseModel<string>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            null,
            "Payment processed successfully!"
        ));
    }

    [HttpGet("generate-qr/{orderId}")]
    public async Task<IActionResult> GeneratePaymentQr(string orderId)
    {
        var qrResponse = await _sepayService.CreatePaymentQrAsync(orderId);

        return Ok(new ResponseModel<SepayQrResponse>(
            StatusCodes.Status201Created,
            ApiCodes.SUCCESS,
            qrResponse,
            "QR code generated successfully"
        ));
    }

    /// <summary>
    /// Generate QR code for remaining payment after deposit
    /// </summary>
    [HttpGet("generate-remaining-qr/{orderId}")]
    public async Task<IActionResult> GenerateRemainingPaymentQr(string orderId)
    {
        var qrResponse = await _sepayService.CreateRemainingPaymentQrAsync(orderId);

        return Ok(new ResponseModel<SepayQrResponse>(
            StatusCodes.Status201Created,
            ApiCodes.SUCCESS,
            qrResponse,
            "Remaining payment QR code generated successfully"
        ));
    }
    }
}
