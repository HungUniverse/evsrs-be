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

    /// <summary>
    /// Tạo bản kiểm tra (handover) khi khách trả xe cho một đơn hàng.
    /// - Xác thực dữ liệu vào, kiểm tra đơn đang ở trạng thái IN_USE.
    /// - Tính phí trả trễ (nếu có), lưu vào trường <c>ReturnLateFee</c> của bản kiểm tra.
    /// - Lưu bản kiểm tra vào cơ sở dữ liệu và trả về DTO đã lưu.
    /// Phương thức này chỉ tạo bản kiểm tra, không thực hiện việc hoàn tất đơn (complete).
    /// </summary>
    /// <param name="request">Dữ liệu yêu cầu kiểm tra trả xe.</param>
    /// <returns>DTO của bản kiểm tra đã được lưu.</returns>
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
            decimal feePerHour = 10000m;

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
                var returnedAtUtc = inspection.CreatedAt.Kind == DateTimeKind.Utc ? inspection.CreatedAt : inspection.CreatedAt.ToUniversalTime();
                var endAtUtc = endAt.Kind == DateTimeKind.Utc ? endAt : endAt.ToUniversalTime();

                var overdueMinutes = (returnedAtUtc - endAtUtc).TotalMinutes;
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
       
        

        // persist late fee on the inspection entity as string (new field)
        inspection.ReturnLateFee = lateFee.ToString(CultureInfo.InvariantCulture);

        await _unitOfWork.HandoverInspectionRepository.InsertAsync(inspection);
        await _unitOfWork.SaveChangesAsync();

        var result = await _unitOfWork.HandoverInspectionRepository.GetByIdAsync(inspection.Id);
        return _mapper.Map<HandoverInspectionResponseDto>(result);

        
    }

    /// <summary>
    /// Tạo bản thanh toán (return settlement) cho một đơn đã trả.
    /// - Kiểm tra dữ liệu đầu vào và ngăn chặn tạo trùng cho cùng một đơn.
    /// - Tạo các mục settlement (SettlementItems), lưu bản settlement và trả về DTO đã lưu.
    /// </summary>
    /// <param name="request">Dữ liệu tạo settlement.</param>
    /// <returns>DTO settlement đã lưu.</returns>
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

    /// <summary>
    /// Cập nhật một return settlement hiện có và các mục kèm theo.
    /// - Xác thực yêu cầu, cập nhật trường của settlement và thay thế danh sách SettlementItems.
    /// - Lưu các thay đổi và trả về DTO cập nhật.
    /// </summary>
    /// <param name="id">Id của settlement cần cập nhật.</param>
    /// <param name="request">Dữ liệu settlement mới.</param>
    /// <returns>DTO settlement sau khi cập nhật.</returns>
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

    /// <summary>
    /// Lấy return settlement theo id, bao gồm các mục (items).
    /// Ném lỗi nếu không tìm thấy settlement tương ứng.
    /// </summary>
    /// <param name="id">Id của settlement.</param>
    /// <returns>DTO của settlement.</returns>
    public async Task<ReturnSettlementResponseDto> GetReturnSettlementByIdAsync(string id)
    {
        var settlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementWithItemsAsync(id);
        _validationService.CheckNotFound(settlement, "Return settlement not found");
        return _mapper.Map<ReturnSettlementResponseDto>(settlement);
    }

    /// <summary>
    /// Lấy settlement của một đơn theo OrderBookingId nếu có.
    /// Trả về null nếu chưa có settlement cho đơn đó.
    /// </summary>
    /// <param name="orderBookingId">Id đơn hàng.</param>
    /// <returns>DTO settlement hoặc null.</returns>
    public async Task<ReturnSettlementResponseDto?> GetReturnSettlementByOrderIdAsync(string orderBookingId)
    {
        var settlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementByOrderIdAsync(orderBookingId);
        return settlement != null ? _mapper.Map<ReturnSettlementResponseDto>(settlement) : null;
    }

    /// <summary>
    /// Lấy danh sách return settlement được tạo trong khoảng thời gian (startDate - endDate).
    /// </summary>
    /// <param name="startDate">Ngày bắt đầu (bao gồm).</param>
    /// <param name="endDate">Ngày kết thúc (bao gồm).</param>
    /// <returns>Danh sách DTO settlement trong khoảng.</returns>
    public async Task<List<ReturnSettlementResponseDto>> GetReturnSettlementsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var settlements = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementsByDateRangeAsync(startDate, endDate);
        return settlements.Select(s => _mapper.Map<ReturnSettlementResponseDto>(s)).ToList();
    }

    /// <summary>
    /// Lấy bản kiểm tra trả xe (handover inspection) theo OrderBookingId.
    /// Ném lỗi nếu không tìm thấy bản kiểm tra.
    /// </summary>
    /// <param name="orderBookingId">Id đơn hàng.</param>
    /// <returns>DTO của bản kiểm tra.</returns>
    public async Task<HandoverInspectionResponseDto> GetReturnInspectionByOrderIdAsync(string orderBookingId)
    {
        var inspection = await _unitOfWork.HandoverInspectionRepository
            .GetHandoverInspectionByOrderAndTypeAsync(orderBookingId, "RETURN");
        _validationService.CheckNotFound(inspection, "Return inspection not found");
        return _mapper.Map<HandoverInspectionResponseDto>(inspection);
    }

    /// <summary>
    /// Xóa một return settlement theo id. Ném lỗi nếu không tồn tại.
    /// </summary>
    /// <param name="id">Id của settlement cần xóa.</param>
    public async Task DeleteReturnSettlementAsync(string id)
    {
        var settlement = await _unitOfWork.ReturnSettlementRepository.GetByIdAsync(id);
        _validationService.CheckNotFound(settlement, "Return settlement not found");

        await _unitOfWork.ReturnSettlementRepository.DeleteAsync(settlement!);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Hoàn tất quá trình trả xe cho một đơn hàng.
    /// - Kiểm tra các điều kiện cần thiết (inspection/settlement đã có), chuyển trạng thái đơn sang <c>COMPLETED</c>.
    /// - Cập nhật trạng thái xe về AVAILABLE và cập nhật membership nếu có.
    /// </summary>
    /// <param name="request">Dữ liệu yêu cầu hoàn tất trả xe (chứa OrderBookingId).</param>
    /// <returns>DTO đơn hàng sau khi hoàn tất.</returns>
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

    /// <summary>
    /// Xử lý thanh toán cho một return settlement: xác thực, đánh dấu PAID và nếu cần hoàn tất đơn.
    /// Cập nhật thông tin thanh toán và trả về DTO settlement đã cập nhật.
    /// </summary>
    /// <param name="request">Dữ liệu thanh toán cho settlement.</param>
    /// <returns>DTO settlement sau khi xử lý thanh toán.</returns>
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

    /// <summary>
    /// Tạo URL QR mã thanh toán SePay cho một return settlement để khách hàng thanh toán.
    /// Kiểm tra tính hợp lệ của settlement và gọi dịch vụ SePay để tạo QR.
    /// </summary>
    /// <param name="returnSettlementId">Id settlement cần tạo QR.</param>
    /// <returns>Chuỗi URL của QR code.</returns>
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

    /// <summary>
    /// Lấy trạng thái thanh toán cho một return settlement và (tuỳ) tạo QR nếu thanh toán đang chờ.
    /// Trả về DTO chứa thông tin thanh toán và URL QR khi có.
    /// </summary>
    /// <param name="returnSettlementId">Id settlement.</param>
    /// <returns>DTO trạng thái thanh toán.</returns>
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