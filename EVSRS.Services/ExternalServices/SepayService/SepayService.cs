using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AutoMapper;
using EVSRS.BusinessObjects.DTO.SepayDto;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Helper;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace EVSRS.Services.ExternalServices.SepayService;

public class SepayService : ISepayService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SepaySettings _sepaySettings;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly ITransactionService _transactionService;

    public SepayService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IValidationService validationService,
        ITransactionService transactionService,
        IOptions<SepaySettings> sepaySettings)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _validationService = validationService;
        _transactionService = transactionService;
        _sepaySettings = sepaySettings.Value;
    }

    public async Task ProcessPaymentWebhookAsync(SepayWebhookPayload payload, string authHeader)
    {
        if (!ValidateAuthHeader(authHeader))
        {
            throw new ErrorException(StatusCodes.Status401Unauthorized, ApiCodes.UNAUTHORIZED, "Invalid API key");
        }

        var orderCode = ExtractOrderCodeFromContent(payload.content);
        if (string.IsNullOrEmpty(orderCode))
        {
            throw new ErrorException(StatusCodes.Status400BadRequest, ApiCodes.INVALID_INPUT,
                "No order code found in payment content");
        }

        var order = await _unitOfWork.OrderRepository.GetByCodeAsync(orderCode);
        _validationService.CheckNotFound(order, $"Order with code {orderCode} not found");

        if (order != null && (order.PaymentStatus == PaymentStatus.PAID_FULL ||
            order.PaymentStatus == PaymentStatus.PAID_DEPOSIT_COMPLETED))
        {
            throw new ErrorException(StatusCodes.Status400BadRequest, ApiCodes.BAD_REQUEST, "Order already paid");
        }

        if (order == null) return;

        // Create transaction record (simplified for now)
        // await _transactionService.CreateTransactionAsync(payload, order.Id, order.Code);
        // var txn = await _unitOfWork.TransactionRepository.GetLatestTransactionByOrderIdAsync(order.Id);

        if (order.User == null && !string.IsNullOrEmpty(order.UserId))
        {
            order.User = await _unitOfWork.UserRepository.GetByIdAsync(order.UserId);
        }

        if (order.PaymentType == PaymentType.DEPOSIT)
        {
            if (order.PaymentStatus == PaymentStatus.PENDING)
            {
                // Lần đầu thanh toán (cọc)
                order.Status = OrderBookingStatus.CONFIRMED;
                order.PaymentStatus = PaymentStatus.PAID_DEPOSIT;
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = "SepayWebhook";

                await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(order);
                await _unitOfWork.SaveChangesAsync();

                // TODO: Send payment receipt and notification
            }
            else if (order.PaymentStatus == PaymentStatus.PAID_DEPOSIT)
            {
                // Thanh toán phần còn lại
                order.Status = OrderBookingStatus.CONFIRMED; // Ready for checkout
                order.PaymentStatus = PaymentStatus.PAID_DEPOSIT_COMPLETED;
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = "SepayWebhook";

                await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(order);
                await _unitOfWork.SaveChangesAsync();

                // TODO: Send payment receipt and notification
            }
        }
        else
        {
            // Thanh toán full
            if (order.Type == OrderType.WARRANTY)
            {
                order.Status = OrderBookingStatus.CONFIRMED;
                order.PaymentStatus = PaymentStatus.PAID_FULL;
                order.UpdatedAt = DateTime.UtcNow;
                order.UpdatedBy = "SepayWebhook";

                await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(order);
                await _unitOfWork.SaveChangesAsync();

                // TODO: Handle warranty specific logic
                return;
            }

            // Regular full payment
            order.Status = OrderBookingStatus.CONFIRMED;
            order.PaymentStatus = PaymentStatus.PAID_FULL;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = "SepayWebhook";

            await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // TODO: Send payment receipt and notification
        }
    }

    public async Task<SepayQrResponse> CreatePaymentQrAsync(string orderId)
    {
        var order = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(orderId);
        _validationService.CheckNotFound(order, $"Order with ID {orderId} not found");

        if (order == null)
        {
            return new SepayQrResponse { QrUrl = "" };
        }

        // Determine payment amount based on payment type
        decimal paymentAmount = 0;
        if (order.PaymentType == PaymentType.DEPOSIT)
        {
            if (!decimal.TryParse(order.DepositAmount, out paymentAmount))
            {
                paymentAmount = decimal.Parse(order.TotalAmount ?? "0") * 0.3m; // 30% deposit
            }
        }
        else
        {
            if (!decimal.TryParse(order.TotalAmount, out paymentAmount))
            {
                paymentAmount = 0;
            }
        }


        // Generate Sepay QR URL
        var qrUrl = GenerateSepayQrUrl(
           accountNumber: _sepaySettings.AccountNumber,
           bankCode: _sepaySettings.BankCode,
           amount: paymentAmount,
           description: $"{order.Code}",
           template: "qronly");

        return new SepayQrResponse
        {
            QrUrl = qrUrl,
            OrderBooking = null // This will be populated by the calling service
        };
    }
    private string GenerateSepayQrUrl(string accountNumber, string bankCode, decimal? amount, string description,
            string template)
    {
        string paymentCode = GeneratePaymentCode();
        string fullDescription = $"{paymentCode}{description}";
        var baseUrl = $"{_sepaySettings.ApiBaseUri}";

        var queryParams = new Dictionary<string, string?>
        {
            ["acc"] = accountNumber,
            ["bank"] = bankCode,
            ["amount"] = amount?.ToString("0"),
            ["des"] = fullDescription,
            ["template"] = template
        };

        var queryString = string.Join("&", queryParams
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return $"{baseUrl}?{queryString}";
    }

    private string GeneratePaymentCode()
    {
        var prefix = "TF";
        var randomPart = GenerateRandomString(7);
        return $"{prefix}{randomPart}";
    }

    private string GenerateRandomString(int length)
    {
        const string chars = "0123456789";
        var data = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(data);
        }

        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[data[i] % chars.Length];
        }

        return new string(result);
    }
    public async Task<string> GetPaymentStatusAsync(string orderId)
    {
        var order = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(orderId);
        _validationService.CheckNotFound(order, "Order not found");

        return order.PaymentStatus.ToString();
    }

    private bool ValidateAuthHeader(string authHeader)
    {
        return authHeader == $"Apikey {_sepaySettings.ApiKey}";
    }

    private string ExtractOrderCodeFromContent(string content)
    {
        var match = Regex.Match(content, @"ORD\d{7}");
        return match.Success ? match.Value : null;
    }
}
