using System;
using EVSRS.BusinessObjects.DTO.SepayDto;

namespace EVSRS.Services.ExternalServices.SepayService;

public interface ISepayService
{
    Task ProcessPaymentWebhookAsync(SepayWebhookPayload payload, string authHeader);
    Task<SepayQrResponse> CreatePaymentQrAsync(string orderId);
    Task<SepayQrResponse> CreateRemainingPaymentQrAsync(string orderId);
    Task<SepayQrResponse> CreateSettlementPaymentQrAsync(CreateSettlementPaymentQrRequest request);
    Task<string> GetPaymentStatusAsync(string orderId);
}
