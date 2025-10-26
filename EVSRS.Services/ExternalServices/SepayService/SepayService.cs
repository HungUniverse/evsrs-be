using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using AutoMapper;
using EVSRS.BusinessObjects.DTO.SepayDto;
using EVSRS.BusinessObjects.DTO.TransactionDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Helper;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace EVSRS.Services.ExternalServices.SepayService;

public class SepayService : ISepayService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SepaySettings _sepaySettings;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    private readonly ITransactionService _transactionService;
    private readonly ILogger<SepayService> _logger;

    public SepayService(
        IUnitOfWork unitOfWork,
        IOptions<SepaySettings> sepaySettings,
        IMapper mapper,
        IValidationService validationService,
        ITransactionService transactionService,
        ILogger<SepayService> logger
    )
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _validationService = validationService;
        _transactionService = transactionService;
        _sepaySettings = sepaySettings.Value;
        _logger = logger;
    }

    public async Task ProcessPaymentWebhookAsync(SepayWebhookPayload payload, string authHeader)
    {

        // Debug: Log full SePay settings to ensure they're loaded correctly
        _logger.LogInformation("SePay Settings - ApiKey: {HasApiKey}, BaseUri: {BaseUri}, AccountNumber: {AccountNumber}",
            !string.IsNullOrEmpty(_sepaySettings.ApiKey),
            _sepaySettings.ApiBaseUri,
            _sepaySettings.AccountNumber);

        if (!ValidateAuthHeader(authHeader))
        {
            _logger.LogError("API key validation failed. Received header: {AuthHeader}", authHeader);
            throw new ErrorException(StatusCodes.Status401Unauthorized, ApiCodes.UNAUTHORIZED, "Invalid API key");
        }

        var paymentCodeOrOrderCode = ExtractPaymentCodeFromContent(payload.content);
        _logger.LogInformation("Payment content: {Content}", payload.content);
        _logger.LogInformation("Extracted payment code: {PaymentCode}", paymentCodeOrOrderCode);

        if (string.IsNullOrEmpty(paymentCodeOrOrderCode))
        {
            _logger.LogError("No payment code found in payment content: {Content}", payload.content);
            throw new ErrorException(StatusCodes.Status400BadRequest, ApiCodes.INVALID_INPUT,
                "No payment code found in payment content");
        }

        // Try to find order by payment code or order code
        var order = await _unitOfWork.OrderRepository.GetByCodeAsync(paymentCodeOrOrderCode);
        if (order == null)
        {
            _logger.LogError("No order found for payment code: {PaymentCode}", paymentCodeOrOrderCode);
            throw new ErrorException(StatusCodes.Status404NotFound, ApiCodes.NOT_FOUND,
                $"Order not found for payment code {paymentCodeOrOrderCode}");
        }
        _validationService.CheckNotFound(order, $"Order not found for payment code {paymentCodeOrOrderCode}");

        if (order != null && (order.PaymentStatus == PaymentStatus.PAID_FULL ||
            order.PaymentStatus == PaymentStatus.PAID_DEPOSIT_COMPLETED))
        {
            throw new ErrorException(StatusCodes.Status400BadRequest, ApiCodes.BAD_REQUEST, "Order already paid");
        }

        if (order == null) return;

        // Create transaction record
        await CreateTransactionFromWebhook(payload, order.Id, order.Code);

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
            if (order.Type == OrderType.RENTAL)
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
        return authHeader == $"ApiKey {_sepaySettings.ApiKey}";
    }


    private string? ExtractPaymentCodeFromContent(string content)
    {
        var match = Regex.Match(content, @"ORD\d{7}");
        return match.Success ? match.Value : null;
    }




    // private async Task<OrderBooking?> FindOrderByPaymentCodeOrOrderCode(string codeToFind, SepayWebhookPayload payload)
    // {
    //     _logger.LogDebug("Finding order by code: {Code}", codeToFind);

    //     // First try to find by order code (direct match)
    //     if (codeToFind.StartsWith("BK") || codeToFind.StartsWith("ORD"))
    //     {
    //         _logger.LogInformation("Searching for order by order code: {OrderCode}", codeToFind);
    //         var orderByCode = await _unitOfWork.OrderRepository.GetByCodeAsync(codeToFind);
    //         if (orderByCode != null)
    //         {
    //             _logger.LogInformation("Found order by order code: {OrderCode} -> Order ID: {OrderId}", codeToFind, orderByCode.Id);
    //             return orderByCode;
    //         }
    //         else
    //         {
    //             _logger.LogWarning("No order found with code: {OrderCode}", codeToFind);
    //         }
    //     }
    //     return null;
    // }

    private async Task<OrderBooking?> FindOrderByAmountAndTimestamp(decimal amount, string? transactionDate)
    {
        try
        {
            _logger.LogDebug("Finding order by amount: {Amount} and date: {Date}", amount, transactionDate);

            // Parse transaction date if available
            DateTime searchDate = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(transactionDate))
            {
                if (DateTime.TryParse(transactionDate, out var parsedDate))
                {
                    searchDate = parsedDate;
                }
            }

            // Search for orders within a time window (±2 hours) with matching amount
            var startTime = searchDate.AddHours(-2);
            var endTime = searchDate.AddHours(2);

            _logger.LogDebug("Searching orders between {StartTime} and {EndTime} with amount {Amount}",
                startTime, endTime, amount);

            // TODO: This requires a method in OrderRepository to search by amount and date range
            // For now, we'll implement a basic search

            // As a temporary workaround, we could get all recent pending orders
            // and filter by amount (this is not efficient for production)
            var recentOrders = await GetRecentPendingOrders();

            foreach (var order in recentOrders)
            {
                // Check if amounts match (considering deposit vs full payment)
                if (await DoesAmountMatch(order, amount))
                {
                    _logger.LogInformation("Found matching order by amount: {OrderCode}, Amount: {Amount}",
                        order.Code, amount);
                    return order;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding order by amount and timestamp");
            return null;
        }
    }

    private async Task<bool> DoesAmountMatch(OrderBooking order, decimal paymentAmount)
    {
        try
        {
            if (order.PaymentType == PaymentType.DEPOSIT)
            {
                // For deposit payments, check if amount matches deposit amount or 30% of total
                if (decimal.TryParse(order.DepositAmount, out var depositAmount))
                {
                    return Math.Abs(paymentAmount - depositAmount) < 1000; // Allow small difference (VND)
                }

                if (decimal.TryParse(order.TotalAmount, out var totalAmount))
                {
                    var calculatedDeposit = totalAmount * 0.3m;
                    return Math.Abs(paymentAmount - calculatedDeposit) < 1000;
                }
            }
            else
            {
                // For full payments
                if (decimal.TryParse(order.TotalAmount, out var totalAmount))
                {
                    return Math.Abs(paymentAmount - totalAmount) < 1000;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking amount match for order {OrderCode}", order.Code);
            return false;
        }
    }

    private async Task<List<OrderBooking>> GetRecentPendingOrders()
    {
        try
        {
            _logger.LogDebug("Getting recent pending orders");
            var pendingOrders = await _unitOfWork.OrderRepository.GetPendingPaymentOrderBookingsAsync();
            _logger.LogInformation("Found {Count} pending payment orders", pendingOrders.Count);
            return pendingOrders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent pending orders");
            return new List<OrderBooking>();
        }
    }

    private async Task CreateTransactionFromWebhook(SepayWebhookPayload payload, string orderId, string? orderCode)
    {
        try
        {
            _logger.LogInformation("Creating transaction from webhook for order {OrderId}", orderId);

            var transactionRequest = new TransactionRequestDto
            {
                OrderBookingId = orderId,
                SepayId = payload.id.ToString(),
                Gateway = payload.gateway,
                TransactionDate = DateTime.TryParse(payload.transactionDate, out var transDate) ? transDate : DateTime.UtcNow,
                AccountNumber = payload.accountNumber,
                Code = orderCode,
                Content = payload.content,
                TransferType = payload.transferType,
                TranferAmount = payload.transferAmount.ToString(),
                Accumulated = payload.accumulated.ToString(),
                SubAccount = payload.subAccount,
                ReferenceCode = payload.referenceCode,
                Description = payload.description
            };

            await _transactionService.CreateTransactionAsync(transactionRequest);
            _logger.LogInformation("Transaction created successfully for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction for order {OrderId}", orderId);
            // Don't throw here - transaction creation failure shouldn't stop payment processing
        }
    }
}
