using System;
using EVSRS.BusinessObjects.DTO.SepayDto;

namespace EVSRS.Services.ExternalServices.SepayService;

public interface ISepayService
{
    Task ProcessPaymentWebhookAsync(SepayWebhookPayload payload, string authHeader);
    Task<SepayQrResponse> CreatePaymentQrAsync(string orderId);
    Task<string> GetPaymentStatusAsync(string orderId);
}
