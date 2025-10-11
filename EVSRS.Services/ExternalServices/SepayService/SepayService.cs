using System;
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

    public Task<SepayQrResponse> CreatePaymentQrAsync(string orderId)
    {
        // TODO: Implement QR generation logic
        throw new NotImplementedException("QR payment generation will be implemented later");
    }

    public Task<string> GetPaymentStatusAsync(string orderId)
    {
        // TODO: Implement payment status check
        throw new NotImplementedException("Payment status check will be implemented later");
    }

    private bool ValidateAuthHeader(string authHeader)
    {
        // TODO: Implement auth header validation
        return true; // For now, always return true
    }

    private string ExtractOrderCodeFromContent(string content)
    {
        // TODO: Implement order code extraction from payment content
        return content; // For now, return content as is
    }
}
