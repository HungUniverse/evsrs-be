using AutoMapper;
using EVSRS.BusinessObjects.DTO.HandoverInspectionDto;
using EVSRS.BusinessObjects.DTO.ReturnSettlementDto;
using EVSRS.BusinessObjects.DTO.OrderBookingDto;
using EVSRS.BusinessObjects.DTO.SepayDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using EVSRS.Services.ExternalServices.SepayService;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace EVSRS.Services.Service;

public class ReturnService : IReturnService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidationService _validationService;
    private readonly ISepayService _sepayService;
    private readonly IOrderBookingService _orderBookingService;

    public ReturnService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IValidationService validationService,
        ISepayService sepayService,
        IOrderBookingService orderBookingService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _validationService = validationService;
        _sepayService = sepayService;
        _orderBookingService = orderBookingService;
    }

    public async Task<HandoverInspectionResponseDto> CreateReturnInspectionAsync(HandoverInspectionRequestDto request)
    {
        await _validationService.ValidateAndThrowAsync(request);

        // Ensure this is a return inspection
        request.Type = "RETURN";

        // Check if return inspection already exists
        var existingInspection = await _unitOfWork.HandoverInspectionRepository
            .GetHandoverInspectionByOrderAndTypeAsync(request.OrderBookingId, "RETURN");
        _validationService.CheckBadRequest(existingInspection != null, "Return inspection already exists for this order");

        // Validate order booking status
        var orderBooking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(request.OrderBookingId);
        _validationService.CheckNotFound(orderBooking, "Order booking not found");
        _validationService.CheckBadRequest(
            orderBooking?.Status != OrderBookingStatus.IN_USE,
            "Order must be in IN_USE status for return inspection"
        );

        var inspection = _mapper.Map<HandoverInspection>(request);
        inspection.Id = Guid.NewGuid().ToString();
        inspection.CreatedBy = GetCurrentUserName();
        inspection.CreatedAt = DateTime.UtcNow;
        inspection.UpdatedAt = DateTime.UtcNow;

        // tính phí trả xe trễ, cho phép trễ 30 phút
        decimal lateFee = 0m;
        try
        {
            // default values
            int graceMinutes = 30;
            decimal feePerHour = 50000m;

            // try read from system config if available
            var graceConfig = await _unitOfWork.SystemConfigRepository.GetSystemConfigByKeyAsync("LATE_RETURN_GRACE_MINUTES");
            if (graceConfig != null && int.TryParse(graceConfig.Value, out var parsedGrace))
                graceMinutes = parsedGrace;

            var feeConfig = await _unitOfWork.SystemConfigRepository.GetSystemConfigByKeyAsync("LATE_RETURN_FEE_PER_HOUR");
            if (feeConfig != null && decimal.TryParse(feeConfig.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedFee))
                feePerHour = parsedFee;

            if (orderBooking != null && orderBooking.EndAt.HasValue)
            {
                // Use the inspection.CreatedAt as the official returned time
                var endAt = orderBooking.EndAt.Value;
                var overdueMinutes = (inspection.CreatedAt - endAt).TotalMinutes;
                if (overdueMinutes > graceMinutes)
                {
                    var effectiveMinutes = overdueMinutes - graceMinutes;
                    var hoursLate = (int)Math.Ceiling(effectiveMinutes / 60.0);
                    lateFee = hoursLate * feePerHour;
                }
            }
        }
        catch (Exception ex)
        {
            // không block flow nếu có lỗi cấu hình / tính toán
            Console.WriteLine($"Error calculating late fee for order {request.OrderBookingId}: {ex.Message}");
            lateFee = 0m;
        }
       
        

        await _unitOfWork.HandoverInspectionRepository.InsertAsync(inspection);
        await _unitOfWork.SaveChangesAsync();

        var result = await _unitOfWork.HandoverInspectionRepository.GetByIdAsync(inspection.Id);
        var dto = _mapper.Map<HandoverInspectionResponseDto>(result);      
        dto.ReturnLateFee = lateFee;
       

        return dto;
    }

    public async Task<ReturnSettlementResponseDto> CreateReturnSettlementAsync(ReturnSettlementRequestDto request)
    {
        await _validationService.ValidateAndThrowAsync(request);

        // Check if settlement already exists
        var existingSettlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementByOrderIdAsync(request.OrderBookingId);
        _validationService.CheckBadRequest(existingSettlement != null, "Return settlement already exists for this order");

        // Validate order booking status
        var orderBooking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(request.OrderBookingId);
        _validationService.CheckNotFound(orderBooking, "Order booking not found");
        _validationService.CheckBadRequest(
            orderBooking?.Status != OrderBookingStatus.RETURNED,
            "Order must be in RETURNED status to create settlement"
        );

        var settlement = _mapper.Map<ReturnSettlement>(request);
        settlement.Id = Guid.NewGuid().ToString();
        settlement.CalculateAt = DateTime.UtcNow;
        settlement.CreatedBy = GetCurrentUserName();
        settlement.CreatedAt = DateTime.UtcNow;
        settlement.UpdatedAt = DateTime.UtcNow;

        // Create settlement items
        settlement.SettlementItems.Clear(); // Clear to avoid duplicates
        foreach (var itemRequest in request.SettlementItems)
        {
            var item = _mapper.Map<SettlementItem>(itemRequest);
            item.Id = Guid.NewGuid().ToString();
            item.ReturnSettlementId = settlement.Id;
            item.CreatedBy = GetCurrentUserName();
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            settlement.SettlementItems.Add(item);
        }

        await _unitOfWork.ReturnSettlementRepository.InsertAsync(settlement);
        await _unitOfWork.SaveChangesAsync();

        var result = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementWithItemsAsync(settlement.Id);
        return _mapper.Map<ReturnSettlementResponseDto>(result);
    }

    public async Task<ReturnSettlementResponseDto> UpdateReturnSettlementAsync(string id, ReturnSettlementRequestDto request)
    {
        await _validationService.ValidateAndThrowAsync(request);

        var settlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementWithItemsAsync(id);
        _validationService.CheckNotFound(settlement, "Return settlement not found");

        _mapper.Map(request, settlement);
        settlement!.UpdatedBy = GetCurrentUserName();
        settlement.UpdatedAt = DateTime.UtcNow;

        // Update settlement items
        settlement.SettlementItems.Clear();
        foreach (var itemRequest in request.SettlementItems)
        {
            var item = _mapper.Map<SettlementItem>(itemRequest);
            item.Id = Guid.NewGuid().ToString();
            item.ReturnSettlementId = settlement.Id;
            item.CreatedBy = GetCurrentUserName();
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            settlement.SettlementItems.Add(item);
        }

        await _unitOfWork.ReturnSettlementRepository.UpdateAsync(settlement);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReturnSettlementResponseDto>(settlement);
    }

    public async Task<ReturnSettlementResponseDto> GetReturnSettlementByIdAsync(string id)
    {
        var settlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementWithItemsAsync(id);
        _validationService.CheckNotFound(settlement, "Return settlement not found");
        return _mapper.Map<ReturnSettlementResponseDto>(settlement);
    }

    public async Task<ReturnSettlementResponseDto?> GetReturnSettlementByOrderIdAsync(string orderBookingId)
    {
        var settlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementByOrderIdAsync(orderBookingId);
        return settlement != null ? _mapper.Map<ReturnSettlementResponseDto>(settlement) : null;
    }

    public async Task<List<ReturnSettlementResponseDto>> GetReturnSettlementsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var settlements = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementsByDateRangeAsync(startDate, endDate);
        return settlements.Select(s => _mapper.Map<ReturnSettlementResponseDto>(s)).ToList();
    }

    public async Task<HandoverInspectionResponseDto> GetReturnInspectionByOrderIdAsync(string orderBookingId)
    {
        var inspection = await _unitOfWork.HandoverInspectionRepository
            .GetHandoverInspectionByOrderAndTypeAsync(orderBookingId, "RETURN");
        _validationService.CheckNotFound(inspection, "Return inspection not found");
        return _mapper.Map<HandoverInspectionResponseDto>(inspection);
    }

    public async Task DeleteReturnSettlementAsync(string id)
    {
        var settlement = await _unitOfWork.ReturnSettlementRepository.GetByIdAsync(id);
        _validationService.CheckNotFound(settlement, "Return settlement not found");

        await _unitOfWork.ReturnSettlementRepository.DeleteAsync(settlement!);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<OrderBookingResponseDto> CompleteReturnProcessAsync(CompleteReturnRequestDto request)
    {
        await _validationService.ValidateAndThrowAsync(request);

        var orderBooking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(request.OrderBookingId);
        _validationService.CheckNotFound(orderBooking, "Order booking not found");

        // Validate order is in RETURNED status
        _validationService.CheckBadRequest(
            orderBooking?.Status != OrderBookingStatus.RETURNED,
            "Order must be in RETURNED status to complete return"
        );

        // Check if return inspection exists
        var returnInspection = await _unitOfWork.HandoverInspectionRepository
            .GetHandoverInspectionByOrderAndTypeAsync(request.OrderBookingId, "RETURN");
        _validationService.CheckNotFound(returnInspection, "Return inspection must be completed first");

        // Update order status to COMPLETED
        orderBooking!.Status = OrderBookingStatus.COMPLETED;
        orderBooking.UpdatedAt = DateTime.UtcNow;
        orderBooking.UpdatedBy = GetCurrentUserName();

        // Update car status back to AVAILABLE
        if (orderBooking.CarEvs != null)
        {
            orderBooking.CarEvs.Status = CarEvStatus.AVAILABLE;
            orderBooking.CarEvs.UpdatedAt = DateTime.UtcNow;
            orderBooking.CarEvs.UpdatedBy = GetCurrentUserName();
            await _unitOfWork.CarEVRepository.UpdateCarEVAsync(orderBooking.CarEvs);
        }

        await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(orderBooking);
        // ✅ Update membership when order completes via return process
        await _orderBookingService.UpdateMembershipForCompletedOrderAsync(orderBooking);

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<OrderBookingResponseDto>(orderBooking);
    }

    public async Task<ReturnSettlementResponseDto> ProcessReturnSettlementPaymentAsync(ReturnSettlementPaymentRequestDto request)
    {
        await _validationService.ValidateAndThrowAsync(request);

        var settlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementWithItemsAsync(request.ReturnSettlementId);
        _validationService.CheckNotFound(settlement, "Return settlement not found");

        // Validate payment status
        _validationService.CheckBadRequest(
            settlement?.PaymentStatus == "PAID",
            "Return settlement is already paid"
        );

        // Update payment information
        settlement!.PaymentStatus = "PAID";
        settlement.PaymentMethod = request.PaymentMethod;
        settlement.PaymentDate = DateTime.UtcNow;
        settlement.UpdatedAt = DateTime.UtcNow;
        settlement.UpdatedBy = GetCurrentUserName();

        if (!string.IsNullOrEmpty(request.Notes))
        {
            settlement.Notes = string.IsNullOrEmpty(settlement.Notes) 
                ? request.Notes 
                : $"{settlement.Notes}\n\nPayment Notes: {request.Notes}";
        }

        await _unitOfWork.ReturnSettlementRepository.UpdateAsync(settlement);

        // If settlement is paid, mark order as COMPLETED
        if (settlement.OrderBooking != null)
        {
            var orderBooking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(settlement.OrderBookingId!);
            if (orderBooking?.Status == OrderBookingStatus.RETURNED)
            {
                orderBooking.Status = OrderBookingStatus.COMPLETED;
                orderBooking.UpdatedAt = DateTime.UtcNow;
                orderBooking.UpdatedBy = GetCurrentUserName();
                await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(orderBooking);
                
                // ✅ Update membership when order completes via settlement payment
                await _orderBookingService.UpdateMembershipForCompletedOrderAsync(orderBooking);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReturnSettlementResponseDto>(settlement);
    }

    public async Task<string> GenerateSepayQrForReturnSettlementAsync(string returnSettlementId)
    {
        var settlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementWithItemsAsync(returnSettlementId);
        _validationService.CheckNotFound(settlement, "Return settlement not found");

        _validationService.CheckBadRequest(
            settlement?.PaymentStatus == "PAID",
            "Return settlement is already paid"
        );

        _validationService.CheckBadRequest(
            string.IsNullOrEmpty(settlement?.Total) || decimal.Parse(settlement.Total) <= 0,
            "Invalid settlement total amount"
        );

        // Get order booking for user info
        var orderBooking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(settlement!.OrderBookingId!);
        _validationService.CheckNotFound(orderBooking, "Order booking not found");

        // Create a unique settlement payment code
        var settlementCode = $"SETTLEMENT_{returnSettlementId[..8].ToUpper()}";
        
        // Call SePay service to create QR with settlement details
        var sepayQrResponse = await _sepayService.CreateSettlementPaymentQrAsync(new CreateSettlementPaymentQrRequest
        {
            SettlementId = returnSettlementId,
            SettlementCode = settlementCode,
            Amount = decimal.Parse(settlement.Total!),
            Description = $"Additional fees payment for order {orderBooking?.Code ?? "Unknown"}",
            OrderBookingId = settlement.OrderBookingId!,
            UserId = orderBooking?.UserId
        });

        return sepayQrResponse.QrUrl;
    }

    public async Task<ReturnSettlementPaymentStatusDto> GetReturnSettlementPaymentStatusAsync(string returnSettlementId)
    {
        // Get settlement by ID
        var settlement = await _unitOfWork.ReturnSettlementRepository.GetByIdAsync(returnSettlementId);
        _validationService.CheckNotFound(settlement, "Return settlement not found");

        // Get order booking for additional context
        var orderBooking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(settlement!.OrderBookingId!);
        
        // Get user info for context
        var user = orderBooking?.UserId != null 
            ? await _unitOfWork.UserRepository.GetByIdAsync(orderBooking.UserId) 
            : null;

        // Calculate if payment is overdue (e.g., 7 days after settlement creation)
        var paymentDueDate = settlement.CreatedAt.AddDays(7);
        var isPaymentOverdue = settlement.PaymentStatus != "PAID" && DateTime.UtcNow > paymentDueDate;
        
        // Generate QR code URL if payment is still pending
        string? qrCodeUrl = null;
        if (settlement.PaymentStatus == "PENDING" && !string.IsNullOrEmpty(settlement.Total) && decimal.Parse(settlement.Total) > 0)
        {
            try
            {
                qrCodeUrl = await GenerateSepayQrForReturnSettlementAsync(returnSettlementId);
            }
            catch (Exception)
            {
                // If QR generation fails, continue without QR (it's optional for status check)
                qrCodeUrl = null;
            }
        }

        return new ReturnSettlementPaymentStatusDto
        {
            Id = settlement.Id,
            OrderBookingId = settlement.OrderBookingId!,
            PaymentStatus = settlement.PaymentStatus ?? "PENDING",
            PaymentMethod = settlement.PaymentMethod,
            PaymentDate = settlement.PaymentDate,
            Total = settlement.Total,
            IsPaymentRequired = !string.IsNullOrEmpty(settlement.Total) && decimal.Parse(settlement.Total) > 0,
            IsPaymentOverdue = isPaymentOverdue,
            PaymentDueDate = paymentDueDate,
            QrCodeUrl = qrCodeUrl,
            CreatedAt = settlement.CreatedAt,
            UpdatedAt = settlement.UpdatedAt,
            OrderCode = orderBooking?.Code,
            CustomerName = user?.FullName,
            CustomerPhone = user?.PhoneNumber
        };
    }

    private string GetCurrentUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
    }
}