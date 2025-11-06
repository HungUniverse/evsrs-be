using AutoMapper;
using EVSRS.BusinessObjects.DTO.OrderBookingDto;
using EVSRS.BusinessObjects.DTO.SepayDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Helper;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.ExternalServices.SepayService;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Service
{
    public class OrderBookingService : IOrderBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidationService _validationService;
        private readonly IServiceProvider _serviceProvider;

        public OrderBookingService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IValidationService validationService,
            IServiceProvider serviceProvider)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _validationService = validationService;
            _serviceProvider = serviceProvider;
        }

        public async Task<decimal> CalculateBookingCostAsync(string carId, DateTime startDate, DateTime endDate)
        {
            var car = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(carId);
            _validationService.CheckNotFound(car, $"Car with ID {carId} not found");

            var model = await _unitOfWork.ModelRepository.GetModelByIdAsync(car!.ModelId!);
            _validationService.CheckNotFound(model, $"Car model not found");

            // Giá thuê theo ngày và giảm giá
            var dailyPrice = (decimal)(model!.Price ?? 0);
            var discount = (decimal)(model.Sale ?? 0) / 100;
            var discountedDailyPrice = dailyPrice * (1 - discount);

            // Tính hệ số thuê dựa trên ca làm việc
            var rentalCoefficient = CalculateRentalCoefficient(startDate, endDate);

            // Tổng tiền = giá_ngày_sau_giảm_giá * hệ_số_thuê
            var totalCost = discountedDailyPrice * rentalCoefficient;

            // Làm tròn đến 2 chữ số thập phân (tiền tệ)
            return Math.Round(totalCost, 2, MidpointRounding.AwayFromZero);
        }

        private decimal CalculateRentalCoefficient(DateTime startDate, DateTime endDate)
        {
            // Depot hoạt động từ 6:00 - 22:00
            // Ca sáng: 6:00 - 12:00 (hệ số 0.35)
            // Ca chiều: 12:00 - 22:00 (hệ số 0.65)
            // Cả ngày: 6:00 - 22:00 (hệ số 1.0)

            // ✅ DEBUG LOG
            Console.WriteLine($"DEBUG - Raw StartDate: {startDate} (Kind: {startDate.Kind})");
            Console.WriteLine($"DEBUG - Raw EndDate: {endDate} (Kind: {endDate.Kind})");
            
            // Convert to Vietnam timezone using helper method
            var startDateVN = ConvertToVietnamTime(startDate);
            var endDateVN = ConvertToVietnamTime(endDate);
            
            Console.WriteLine($"DEBUG - VN StartDate: {startDateVN} (TimeOfDay: {startDateVN.TimeOfDay})");
            Console.WriteLine($"DEBUG - VN EndDate: {endDateVN} (TimeOfDay: {endDateVN.TimeOfDay})");

            var morningStart = new TimeSpan(6, 0, 0);
            var afternoonStart = new TimeSpan(12, 0, 0);
            var depotClose = new TimeSpan(22, 0, 0);

            // Validate operating hours using Vietnam time
            _validationService.CheckBadRequest(
                startDateVN.TimeOfDay < morningStart || startDateVN.TimeOfDay > depotClose,
                $"Start time must be within depot operating hours (6:00 AM - 10:00 PM). Received: {startDateVN.TimeOfDay} (VN time)"
            );
            
            _validationService.CheckBadRequest(
                endDateVN.TimeOfDay < morningStart || endDateVN.TimeOfDay > depotClose,
                $"End time must be within depot operating hours (6:00 AM - 10:00 PM). Received: {endDateVN.TimeOfDay} (VN time)"
            );

            // Use Vietnam time for all calculations
            return CalculateCoefficient(startDateVN, endDateVN);
        }

        private decimal CalculateCoefficient(DateTime startDateVN, DateTime endDateVN)
        {
            var morningStart = new TimeSpan(6, 0, 0);
            var afternoonStart = new TimeSpan(12, 0, 0);
            var depotClose = new TimeSpan(22, 0, 0);
            var earlyMorningEnd = new TimeSpan(7, 0, 0); // ✅ THÊM: 7:00 sáng để miễn phí

            // Nếu thuê trong cùng 1 ngày
            if (startDateVN.Date == endDateVN.Date)
            {
                var startTime = startDateVN.TimeOfDay;
                var endTime = endDateVN.TimeOfDay;

                // Thuê chỉ trong ca sáng (6:00-12:00)
                if (startTime >= morningStart && endTime <= afternoonStart)
                {
                    return 0.4m; // Ca sáng
                }
                // Thuê chỉ trong ca chiều (12:00-22:00)
                else if (startTime >= afternoonStart && endTime <= depotClose)
                {
                    return 0.6m; // Ca chiều
                }
                // Thuê cả 2 ca trong ngày
                else if (startTime >= morningStart && endTime <= depotClose)
                {
                    return 1.0m; // Cả ngày
                }
                else
                {
                    throw new ArgumentException("Invalid rental time within the same day");
                }
            }

            // Nếu thuê qua nhiều ngày
            var daysDifference = (endDateVN.Date - startDateVN.Date).Days;
            decimal totalCoefficient = 0m;

            // Xử lý ngày đầu tiên
            var firstDayStartTime = startDateVN.TimeOfDay;
            if (firstDayStartTime >= morningStart && firstDayStartTime < afternoonStart)
            {
                // Bắt đầu từ ca sáng → tính full ngày đầu
                totalCoefficient += 1.0m;
            }
            else if (firstDayStartTime >= afternoonStart && firstDayStartTime <= depotClose)
            {
                // Bắt đầu từ ca chiều → chỉ tính ca chiều ngày đầu
                totalCoefficient += 0.6m;
            }

            // Các ngày ở giữa (nếu có) → mỗi ngày tính full
            if (daysDifference > 1)
            {
                totalCoefficient += (daysDifference - 1) * 1.0m;
            }

            // Xử lý ngày cuối cùng
            var lastDayEndTime = endDateVN.TimeOfDay;
            
            // ✅ CHECK: Đây có phải đơn hàng từ 2 ca trở lên không?
            bool isMultiShiftRental = totalCoefficient >= 0.6m; // Ít nhất 1 ca chiều hoặc multi-day
            
            if (lastDayEndTime <= afternoonStart && lastDayEndTime >= morningStart)
            {
                // Kết thúc trong ca sáng
                
                // ✅ SPECIAL RULE: Nếu trả xe 6:00-7:00 sáng và là đơn từ 2 ca trở lên → MIỄN PHÍ
                if (isMultiShiftRental && lastDayEndTime >= morningStart && lastDayEndTime <= earlyMorningEnd)
                {
                    // MIỄN PHÍ ca sáng - không cộng thêm gì
                    totalCoefficient += 0m;
                    Console.WriteLine($"DEBUG - Free early morning return: {lastDayEndTime} (multi-shift rental, coefficient: {totalCoefficient})");
                }
                else
                {
                    // Trả xe sau 7:00 sáng hoặc đơn hàng < 2 ca → tính phí ca sáng
                    totalCoefficient += 0.4m;
                    Console.WriteLine($"DEBUG - Charged morning return: {lastDayEndTime} (coefficient: {totalCoefficient})");
                }
            }
            else if (lastDayEndTime <= depotClose && lastDayEndTime > afternoonStart)
            {
                // Kết thúc trong ca chiều → tính full ngày cuối
                totalCoefficient += 1.0m;
            }

            return totalCoefficient;
        }

        public async Task<OrderBookingResponseDto> CancelOrderAsync(string id, string reason)
        {
            var booking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(id);
            _validationService.CheckNotFound(booking, $"Order booking with ID {id} not found");

            _validationService.CheckBadRequest(
                booking.Status != OrderBookingStatus.PENDING && booking.Status != OrderBookingStatus.CONFIRMED && booking.Status != OrderBookingStatus.READY_FOR_CHECKOUT,
                "Only pending, confirmed or ready_for_checkout bookings can be cancelled"
            );

            // Check if payment has been made
            bool hasBeenPaid = booking.PaymentStatus == PaymentStatus.PAID_DEPOSIT ||
                               booking.PaymentStatus == PaymentStatus.PAID_FULL ||
                               booking.PaymentStatus == PaymentStatus.PAID_DEPOSIT_COMPLETED;

            decimal refundAmount = 0m;

            if (!hasBeenPaid)
            {
                // Not paid yet → Cancel directly, no refund needed
                booking.Status = OrderBookingStatus.CANCELLED;
                refundAmount = 0m;

                var noteBuilder = new StringBuilder(booking.Note ?? string.Empty);
                noteBuilder.AppendLine($"Cancelled before payment - Reason: {reason}");
                booking.Note = noteBuilder.ToString();
            }
            else
            {
                // Already paid → Calculate refund and wait for admin processing
                decimal depositAmount = 0m;
                if (!string.IsNullOrEmpty(booking.DepositAmount))
                {
                    decimal.TryParse(booking.DepositAmount, out depositAmount);
                }

                var now = DateTime.UtcNow;
                if (booking.StartAt.HasValue && now > booking.StartAt.Value)
                {
                    // no refund after vehicle pickup
                    refundAmount = 0m;
                }
                else
                {
                    var hoursSinceBooking = (now - booking.CreatedAt).TotalHours;
                    if (hoursSinceBooking <= 24)
                    {
                        refundAmount = depositAmount; // full deposit
                    }
                    else
                    {
                        refundAmount = depositAmount / 2m; // half deposit
                    }
                }

                if (refundAmount > 0)
                {
                    // Need refund → Wait for admin processing
                    booking.Status = OrderBookingStatus.REFUND_PENDING;
                    var customerName = booking.User?.FullName ?? "(Offline/Unknown)";
                    var customerPhone = booking.User?.PhoneNumber ?? "(Unknown)";
                    booking.Note = $"{booking.Note}\nCancellation reason: {reason}\nRefundAmount: {refundAmount:C}\nCustomer: {customerName} - {customerPhone}";
                }
                else
                {
                    // Already paid but no refund (cancelled after pickup time) → Cancel directly
                    booking.Status = OrderBookingStatus.CANCELLED;
                    booking.Note = $"{booking.Note}\nCancellation reason: {reason}\nNo refund (cancelled after pickup time)";
                }
            }

            booking.UpdatedBy = GetCurrentUserName();
            booking.UpdatedAt = DateTime.UtcNow;

            // Free up the car
            var car = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(booking.CarEVDetailId);
            if (car != null)
            {
                car.Status = CarEvStatus.AVAILABLE;
                await _unitOfWork.CarEVRepository.UpdateCarEVAsync(car);
            }

            await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<OrderBookingResponseDto>(booking);
            // Populate computed refund amount on returned DTO (not persisted)
            resultDto.RefundAmount = refundAmount;
            return resultDto;
        }

        public async Task<PaginatedList<OrderBookingResponseDto>> GetRefundPendingOrdersAsync(int pageNumber, int pageSize)
        {
            var bookings = await _unitOfWork.OrderRepository.GetOrderBookingListAsync(pageNumber, pageSize);
            var pending = bookings.Items.Where(b => b.Status == OrderBookingStatus.REFUND_PENDING).ToList();
            var dtos = pending.Select(b => _mapper.Map<OrderBookingResponseDto>(b)).ToList();
            // compute and populate refund amount for admin display
            for (int i = 0; i < pending.Count; i++)
            {
                try
                {
                    dtos[i].RefundAmount = ComputeRefundAmount(pending[i]);
                }
                catch
                {
                    // If any unexpected issue occurs, leave RefundAmount null to avoid breaking the listing
                    dtos[i].RefundAmount = null;
                }
            }

            return new PaginatedList<OrderBookingResponseDto>(dtos, pending.Count, pageNumber, pageSize);
        }

        public async Task<OrderBookingResponseDto> ConfirmRefundAsync(string id, decimal? refundedAmount = null, string? adminNote = null)
        {
            var booking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(id);
            _validationService.CheckNotFound(booking, $"Order booking with ID {id} not found");

            _validationService.CheckBadRequest(
                booking.Status != OrderBookingStatus.REFUND_PENDING,
                "Only bookings in REFUND_PENDING status can be confirmed for refund"
            );

            // Update status/payment
            booking.Status = OrderBookingStatus.CANCELLED;
            booking.PaymentStatus = PaymentStatus.REFUNDED;

            // Append admin confirmation info to note
            var noteBuilder = new StringBuilder(booking.Note ?? string.Empty);
            noteBuilder.AppendLine();
            noteBuilder.AppendLine($"Admin confirmed refund at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} by {GetCurrentUserName()}.");
            if (refundedAmount.HasValue)
            {
                noteBuilder.AppendLine($"Refunded Amount: {refundedAmount.Value:C}");
            }
            if (!string.IsNullOrWhiteSpace(adminNote))
            {
                noteBuilder.AppendLine($"Admin note: {adminNote}");
            }
            booking.Note = noteBuilder.ToString();

            booking.UpdatedBy = GetCurrentUserName();
            booking.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OrderBookingResponseDto>(booking);
        }

        public async Task<bool> CheckCarAvailabilityAsync(string carId, DateTime startDate, DateTime endDate, string? excludeBookingId = null)
        {
            // Check if car exists and is available
            var car = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(carId);
            if (car == null || car.Status != CarEvStatus.AVAILABLE)
                return false;

            return await _unitOfWork.OrderRepository.IsCarAvailableAsync(carId, startDate, endDate, excludeBookingId);
        }



        public async Task<OrderBookingResponseDto> CheckoutOrderAsync(string id)
        {
            var booking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(id);
            _validationService.CheckNotFound(booking, $"Order booking with ID {id} not found");

            _validationService.CheckBadRequest(
                booking.Status != OrderBookingStatus.READY_FOR_CHECKOUT,
                "Only bookings in READY_FOR_CHECKOUT status can be checked out"
            );

            _validationService.CheckBadRequest(
                booking.PaymentStatus != PaymentStatus.PAID_FULL && booking.PaymentStatus != PaymentStatus.PAID_DEPOSIT_COMPLETED,
                "Payment must be completed before checkout"
            );

            // Check if handover inspection exists
            var handoverInspection = await _unitOfWork.HandoverInspectionRepository
                .GetHandoverInspectionByOrderAndTypeAsync(id, "HANDOVER");
            _validationService.CheckBadRequest(handoverInspection == null,
                "Handover inspection must be completed before checkout");

            booking.Status = OrderBookingStatus.CHECKED_OUT;
            booking.CheckOutedAt = DateTime.UtcNow;
            booking.UpdatedBy = GetCurrentUserName();
            booking.UpdatedAt = DateTime.UtcNow;

            // Update car status
            var car = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(booking.CarEVDetailId);
            if (car != null)
            {
                car.Status = CarEvStatus.IN_USE;
                await _unitOfWork.CarEVRepository.UpdateCarEVAsync(car);
            }

            await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OrderBookingResponseDto>(booking);
        }

        public async Task<OrderBookingResponseDto> StartOrderAsync(string id)
        {
            var booking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(id);
            _validationService.CheckNotFound(booking, $"Order booking with ID {id} not found");

            _validationService.CheckBadRequest(
                booking!.Status != OrderBookingStatus.CHECKED_OUT,
                "Only checked out bookings can be started"
            );

            booking.Status = OrderBookingStatus.IN_USE;
            booking.UpdatedBy = GetCurrentUserName();
            booking.UpdatedAt = DateTime.UtcNow;

            // Update car status  
            if (!string.IsNullOrEmpty(booking.CarEVDetailId))
            {
                var car = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(booking.CarEVDetailId);
                if (car != null)
                {
                    car.Status = CarEvStatus.IN_USE;
                    await _unitOfWork.CarEVRepository.UpdateCarEVAsync(car);
                }
            }

            await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OrderBookingResponseDto>(booking);
        }

        public async Task<OrderBookingResponseDto> ProcessReturnOrderAsync(string id)
        {
            var booking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(id);
            _validationService.CheckNotFound(booking, $"Order booking with ID {id} not found");

            _validationService.CheckBadRequest(
                booking!.Status != OrderBookingStatus.IN_USE,
                "Only orders in use can be returned"
            );

            // Check if return inspection exists
            var returnInspection = await _unitOfWork.HandoverInspectionRepository
                .GetHandoverInspectionByOrderAndTypeAsync(id, "RETURN");
            _validationService.CheckBadRequest(returnInspection == null,
                "Return inspection must be completed before processing return");

            booking.Status = OrderBookingStatus.RETURNED;
            booking.ReturnedAt = DateTime.UtcNow;
            booking.UpdatedBy = GetCurrentUserName();
            booking.UpdatedAt = DateTime.UtcNow;

            // Update car status
            if (!string.IsNullOrEmpty(booking.CarEVDetailId))
            {
                var car = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(booking.CarEVDetailId);
                if (car != null)
                {
                    car.Status = CarEvStatus.AVAILABLE;
                    await _unitOfWork.CarEVRepository.UpdateCarEVAsync(car);
                }
            }

            await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OrderBookingResponseDto>(booking);
        }

        public async Task<OrderBookingResponseDto> CompleteOrderAsync(string id)
        {
            var booking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(id);
            _validationService.CheckNotFound(booking, $"Order booking with ID {id} not found");

            _validationService.CheckBadRequest(
                booking!.Status != OrderBookingStatus.RETURNED,
                "Only returned orders can be completed"
            );

            // Check if return settlement exists (if there are additional fees)
            var returnSettlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementByOrderIdAsync(id);
            // Return settlement is optional - only needed if there are additional fees

            booking.Status = OrderBookingStatus.COMPLETED;
            booking.UpdatedBy = GetCurrentUserName();
            booking.UpdatedAt = DateTime.UtcNow;

            // Update car status back to AVAILABLE when order is completed
            if (!string.IsNullOrEmpty(booking.CarEVDetailId))
            {
                var car = await _unitOfWork.CarEVRepository.GetByIdAsync(booking.CarEVDetailId);
                if (car != null)
                {
                    car.Status = CarEvStatus.AVAILABLE;
                    car.UpdatedBy = GetCurrentUserName();
                    car.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.CarEVRepository.UpdateAsync(car);
                }
            }

            await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OrderBookingResponseDto>(booking);
        }

        public async Task<SepayQrResponse> CreateOfflineOrderBookingAsync(OrderBookingOfflineRequestDto request)
        {
            await _validationService.ValidateAndThrowAsync(request);

            // Get current staff's depot ID
            var currentUserId = GetCurrentUserId();
            _validationService.CheckBadRequest(string.IsNullOrEmpty(currentUserId), "Staff must be logged in to create offline booking");

            var currentStaff = await _unitOfWork.UserRepository.GetUserByIdAsync(currentUserId);
            _validationService.CheckNotFound(currentStaff, "Current staff not found");
            _validationService.CheckBadRequest(string.IsNullOrEmpty(currentStaff?.DepotId), "Staff must be assigned to a depot");

            // ✅ THÊM: Kiểm tra user trong request đã có booking active chưa (nếu có UserId)
            if (!string.IsNullOrEmpty(request.UserId))
            {
                var hasActiveBooking = await HasActiveBookingAsync(request.UserId);
                _validationService.CheckBadRequest(hasActiveBooking, 
                    "This customer already has an active booking. Please complete or cancel their current booking before creating a new one.");
            }

            // Validate car availability
            var isAvailable = await CheckCarAvailabilityAsync(request.CarEVDetailId, request.StartAt, request.EndAt);
            _validationService.CheckBadRequest(!isAvailable, "Car is not available for the selected dates");

            // Calculate costs
            var totalCost = await CalculateBookingCostAsync(request.CarEVDetailId, request.StartAt, request.EndAt);

            var booking = _mapper.Map<OrderBooking>(request);
            booking.Id = Guid.NewGuid().ToString();
            booking.Code = GenerateBookingCode(); // Generate unique booking code
            booking.UserId = request.UserId;
            booking.DepotId = currentStaff!.DepotId; // Set depot ID from current staff
            booking.CarEVDetailId = request.CarEVDetailId;
            booking.Status = OrderBookingStatus.CONFIRMED;
            booking.PaymentType = PaymentType.FULL;
            booking.PaymentStatus = PaymentStatus.PENDING;
            booking.SubTotal = totalCost.ToString();
            booking.TotalAmount = totalCost.ToString();
            booking.CreatedBy = GetCurrentUserName();
            booking.CreatedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;



            await _unitOfWork.OrderRepository.CreateOrderBookingAsync(booking);

            // Reserve the car
            var car = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(request.CarEVDetailId);
            if (car != null)
            {
                car.Status = CarEvStatus.RESERVED;
                await _unitOfWork.CarEVRepository.UpdateCarEVAsync(car);
            }

            await _unitOfWork.SaveChangesAsync();
            var qrResponse = new SepayQrResponse();
            if (request.PaymentMethod == PaymentMethod.BANKING)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var sepayService = scope.ServiceProvider.GetRequiredService<ISepayService>();
                        qrResponse = await sepayService.CreatePaymentQrAsync(booking.Id);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the booking creation
                    // QR can be generated later if needed
                    Console.WriteLine($"Failed to generate QR URL: {ex.Message}");
                    qrResponse.QrUrl = "";
                }
            }

            var result = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(booking.Id);
            qrResponse.OrderBooking = _mapper.Map<OrderBookingResponseDto>(result);

            return qrResponse;


        }

        public async Task<SepayQrResponse> CreateOrderBookingAsync(OrderBookingRequestDto request)
        {
            await _validationService.ValidateAndThrowAsync(request);

            var currentUserId = GetCurrentUserId();
            _validationService.CheckBadRequest(string.IsNullOrEmpty(currentUserId), "User must be logged in to create booking");

            // ✅ THÊM: Kiểm tra user đã có booking active chưa
            var hasActiveBooking = await HasActiveBookingAsync(currentUserId);
            _validationService.CheckBadRequest(hasActiveBooking, 
                "You already have an active booking. Please complete or cancel your current booking before creating a new one.");

            // Validate car availability
            var isAvailable = await CheckCarAvailabilityAsync(request.CarEVDetailId, request.StartAt, request.EndAt);
            _validationService.CheckBadRequest(!isAvailable, "Car is not available for the selected dates");

            // Calculate costs
            var totalCost = await CalculateBookingCostAsync(request.CarEVDetailId, request.StartAt, request.EndAt);
            var depositFee = await _unitOfWork.SystemConfigRepository.GetSystemConfigByKeyAsync("DEPOSIT_FEE_PERCENTAGE");
            decimal depositPercent = 30m;
            if (depositFee != null && !string.IsNullOrWhiteSpace(depositFee.Value) && decimal.TryParse(depositFee.Value, out var parsedPercent))
            {
                depositPercent = parsedPercent;
            }
            var depositAmount = totalCost * depositPercent / 100m;
            var remainingAmount = totalCost - depositAmount;

            var booking = _mapper.Map<OrderBooking>(request);
            booking.Id = Guid.NewGuid().ToString();
            booking.Code = GenerateBookingCode(); // Generate unique booking code
            booking.UserId = currentUserId;
            booking.Status = OrderBookingStatus.PENDING;
            booking.PaymentStatus = PaymentStatus.PENDING;
            booking.SubTotal = totalCost.ToString();
            booking.TotalAmount = totalCost.ToString();
            booking.DepositAmount = request.PaymentType == PaymentType.DEPOSIT ? depositAmount.ToString() : totalCost.ToString();
            booking.RemainingAmount = request.PaymentType == PaymentType.DEPOSIT ? remainingAmount.ToString() : "0";
            booking.CreatedBy = GetCurrentUserName();
            booking.CreatedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.OrderRepository.CreateOrderBookingAsync(booking);

            // Reserve the car
            var car = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(request.CarEVDetailId);
            if (car != null)
            {
                car.Status = CarEvStatus.RESERVED;
                await _unitOfWork.CarEVRepository.UpdateCarEVAsync(car);
            }

            await _unitOfWork.SaveChangesAsync();

            // Generate payment QR using service locator to avoid circular dependency
            var qrResponse = new SepayQrResponse();
            if (request.PaymentMethod == PaymentMethod.BANKING)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var sepayService = scope.ServiceProvider.GetRequiredService<ISepayService>();
                        qrResponse = await sepayService.CreatePaymentQrAsync(booking.Id);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the booking creation
                    // QR can be generated later if needed
                    Console.WriteLine($"Failed to generate QR URL: {ex.Message}");
                    qrResponse.QrUrl = "";
                }
            }

            var result = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(booking.Id);
            qrResponse.OrderBooking = _mapper.Map<OrderBookingResponseDto>(result);

            return qrResponse;
        }

        public async Task DeleteOrderBookingAsync(string id)
        {
            var booking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(id);
            _validationService.CheckNotFound(booking, $"Order booking with ID {id} not found");

            _validationService.CheckBadRequest(
                booking?.Status != OrderBookingStatus.PENDING && booking?.Status != OrderBookingStatus.CANCELLED,
                "Only pending or cancelled bookings can be deleted"
            );

            await _unitOfWork.OrderRepository.DeleteOrderBookingAsync(booking!);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PaginatedList<OrderBookingResponseDto>> GetAllOrderBookingsAsync(int pageNumber, int pageSize)
        {
            var bookings = await _unitOfWork.OrderRepository.GetOrderBookingListAsync(pageNumber, pageSize);
            var bookingDtos = bookings.Items.Select(b => _mapper.Map<OrderBookingResponseDto>(b)).ToList();

            // Compute and populate refund amount for orders that can be refunded or have been refunded
            var refundableStatuses = new[] {
                OrderBookingStatus.PENDING,           // Can cancel → show potential refund
                OrderBookingStatus.CONFIRMED,         // Can cancel → show potential refund  
                OrderBookingStatus.READY_FOR_CHECKOUT,// Can cancel → show potential refund
                OrderBookingStatus.REFUND_PENDING,    // Waiting for refund → show refund amount
                OrderBookingStatus.CANCELLED          // Already cancelled → show refunded amount
            };

            var bookingsList = bookings.Items.ToList();
            for (int i = 0; i < bookingsList.Count; i++)
            {
                if (bookingsList[i].Status.HasValue && refundableStatuses.Contains(bookingsList[i].Status.Value))
                {
                    try
                    {
                        bookingDtos[i].RefundAmount = ComputeRefundAmount(bookingsList[i]);
                    }
                    catch
                    {
                        // If any unexpected issue occurs, leave RefundAmount null to avoid breaking the listing
                        bookingDtos[i].RefundAmount = null;
                    }
                }
                // Other statuses (COMPLETED, IN_USE, CHECKED_OUT) → RefundAmount = null
            }

            return new PaginatedList<OrderBookingResponseDto>(bookingDtos, bookings.TotalCount, pageNumber, pageSize);
        }

        public async Task<OrderBookingResponseDto> GetOrderBookingByIdAsync(string id)
        {
            var booking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(id);
            _validationService.CheckNotFound(booking, $"Order booking with ID {id} not found");
            return _mapper.Map<OrderBookingResponseDto>(booking);
        }

        public async Task<List<OrderBookingResponseDto>> GetOrderBookingsByDepotIdAsync(string depotId)
        {
            var bookings = await _unitOfWork.OrderRepository.GetOrderBookingByDepotIdAsync(depotId);
            return bookings.Select(b => _mapper.Map<OrderBookingResponseDto>(b)).ToList();
        }

        public async Task<List<OrderBookingResponseDto>> GetOrderBookingsByUserIdAsync(string userId)
        {
            var bookings = await _unitOfWork.OrderRepository.GetOrderBookingByUserIdAsync(userId);
            return bookings.Select(b => _mapper.Map<OrderBookingResponseDto>(b)).ToList();
        }

        public async Task<OrderBookingResponseDto> ReturnOrderAsync(string id)
        {
            var booking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(id);
            _validationService.CheckNotFound(booking, $"Order booking with ID {id} not found");

            _validationService.CheckBadRequest(
                booking!.Status != OrderBookingStatus.IN_USE,
                "Only orders in use can be returned"
            );

            booking.Status = OrderBookingStatus.RETURNED;
            booking.ReturnedAt = DateTime.UtcNow;
            booking.UpdatedBy = GetCurrentUserName();
            booking.UpdatedAt = DateTime.UtcNow;

            // Update car status
            if (!string.IsNullOrEmpty(booking.CarEVDetailId))
            {
                var car = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(booking.CarEVDetailId);
                if (car != null)
                {
                    car.Status = CarEvStatus.AVAILABLE;
                    await _unitOfWork.CarEVRepository.UpdateCarEVAsync(car);
                }
            }

            await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OrderBookingResponseDto>(booking);
        }

        public async Task UpdateOrderBookingAsync(string id, OrderBookingRequestDto request)
        {
            var booking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(id);
            _validationService.CheckNotFound(booking, $"Order booking with ID {id} not found");

            _validationService.CheckBadRequest(
                booking!.Status != OrderBookingStatus.PENDING,
                "Only pending bookings can be updated"
            );

            await _validationService.ValidateAndThrowAsync(request);

            // If car or dates changed, check availability
            if (booking.CarEVDetailId != request.CarEVDetailId ||
                booking.StartAt != request.StartAt ||
                booking.EndAt != request.EndAt)
            {
                var isAvailable = await CheckCarAvailabilityAsync(request.CarEVDetailId, request.StartAt, request.EndAt, id);
                _validationService.CheckBadRequest(!isAvailable, "Car is not available for the selected dates");

                // Free up old car if changed
                if (booking.CarEVDetailId != request.CarEVDetailId && !string.IsNullOrEmpty(booking.CarEVDetailId))
                {
                    var oldCar = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(booking.CarEVDetailId);
                    if (oldCar != null)
                    {
                        oldCar.Status = CarEvStatus.AVAILABLE;
                        await _unitOfWork.CarEVRepository.UpdateCarEVAsync(oldCar);
                    }

                    // Reserve new car
                    var newCar = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(request.CarEVDetailId);
                    if (newCar != null)
                    {
                        newCar.Status = CarEvStatus.RESERVED;
                        await _unitOfWork.CarEVRepository.UpdateCarEVAsync(newCar);
                    }
                }

                // Recalculate costs
                var totalCost = await CalculateBookingCostAsync(request.CarEVDetailId, request.StartAt, request.EndAt);
                var depositAmount = totalCost * 0.3m;
                var remainingAmount = totalCost - depositAmount;

                booking.SubTotal = totalCost.ToString();
                booking.TotalAmount = totalCost.ToString();
                booking.DepositAmount = request.PaymentType == PaymentType.DEPOSIT ? depositAmount.ToString() : totalCost.ToString();
                booking.RemainingAmount = request.PaymentType == PaymentType.DEPOSIT ? remainingAmount.ToString() : "0";
            }

            _mapper.Map(request, booking);
            booking.UpdatedBy = GetCurrentUserName();
            booking.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(booking);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<OrderBookingResponseDto> UpdateOrderStatusAsync(string id, OrderBookingStatus status, PaymentStatus? paymentStatus = null)
        {
            var booking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(id);
            _validationService.CheckNotFound(booking, $"Order booking with ID {id} not found");

            booking!.Status = status;
            if (paymentStatus.HasValue)
            {
                booking.PaymentStatus = paymentStatus.Value;
            }
            booking.UpdatedBy = GetCurrentUserName();
            booking.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(booking);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<OrderBookingResponseDto>(booking);
        }

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }

        private string GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value ?? string.Empty;
        }

        private static readonly Random _random = new Random();

        private DateTime ConvertToVietnamTime(DateTime dateTime)
        {
            var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            
            try
            {
                // Nếu DateTime có offset info (từ JSON với timezone)
                if (dateTime.Kind != DateTimeKind.Unspecified)
                {
                    return TimeZoneInfo.ConvertTime(dateTime, vietnamTimeZone);
                }
                
                // Nếu Unspecified, assume là local time và convert
                var localDateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
                return TimeZoneInfo.ConvertTime(localDateTime, vietnamTimeZone);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Timezone conversion error: {ex.Message}");
                // Fallback: assume input is already Vietnam time
                return dateTime;
            }
        }

        private async Task<bool> HasActiveBookingAsync(string userId)
        {
            var activeBookings = await _unitOfWork.OrderRepository.GetOrderBookingByUserIdAsync(userId);
            
            // Các trạng thái được coi là "active" (chưa hoàn thành)
            var activeStatuses = new[] {
                OrderBookingStatus.PENDING,
                OrderBookingStatus.CONFIRMED,
                OrderBookingStatus.READY_FOR_CHECKOUT,
                OrderBookingStatus.CHECKED_OUT,
                OrderBookingStatus.IN_USE,
                OrderBookingStatus.RETURNED
            };
            
            return activeBookings.Any(b => b.Status.HasValue && activeStatuses.Contains(b.Status.Value));
        }

        private string GenerateBookingCode()
        {
            string prefix = "ORD";
            string randomPart = _random.Next(1000000, 9999999).ToString();
            return $"{prefix}{randomPart}";
        }

        // Compute refund amount according to the cancellation rules (no persistence)
        private decimal ComputeRefundAmount(OrderBooking booking)
        {
            if (booking == null) return 0m;

            decimal depositAmount = 0m;
            if (!string.IsNullOrEmpty(booking.DepositAmount))
            {
                decimal.TryParse(booking.DepositAmount, out depositAmount);
            }

            var now = DateTime.UtcNow;
            if (booking.StartAt.HasValue && now > booking.StartAt.Value)
            {
                return 0m;
            }

            var hoursSinceBooking = (now - booking.CreatedAt).TotalHours;
            if (hoursSinceBooking <= 24)
            {
                return depositAmount; // full deposit
            }

            return depositAmount / 2m; // half deposit
        }

        public async Task CancelExpiredUnpaidOrdersAsync()
        {
            try
            {
                // Lấy timeout từ SystemConfig
                var timeoutConfig = await _unitOfWork.SystemConfigRepository.GetSystemConfigByKeyAsync("ORDER_PAYMENT_TIMEOUT_HOURS");
                int timeoutHours = 1; // Default 1 hour
                if (timeoutConfig != null && !string.IsNullOrWhiteSpace(timeoutConfig.Value) 
                    && int.TryParse(timeoutConfig.Value, out var parsedTimeout))
                {
                    timeoutHours = parsedTimeout;
                }

                var cutoffTime = DateTime.UtcNow.AddHours(-timeoutHours);

                // Tìm các đơn hàng PENDING chưa thanh toán và quá thời hạn
                var expiredOrders = await _unitOfWork.OrderRepository.GetExpiredUnpaidOrdersAsync(cutoffTime);

                foreach (var order in expiredOrders)
                {
                    try
                    {
                        // Tự động hủy đơn hàng
                        await CancelExpiredOrderAsync(order);
                        Console.WriteLine($"Auto-cancelled expired order {order.Code} (ID: {order.Id})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to cancel expired order {order.Code} (ID: {order.Id}): {ex.Message}");
                    }
                }

                if (expiredOrders.Any())
                {
                    Console.WriteLine($"Processed {expiredOrders.Count()} expired orders");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CancelExpiredUnpaidOrdersAsync: {ex.Message}");
                throw;
            }
        }

        private async Task CancelExpiredOrderAsync(OrderBooking order)
        {
            // Cập nhật status
            order.Status = OrderBookingStatus.CANCELLED;
            order.PaymentStatus = PaymentStatus.FAILED;
            
            // Thêm note về lý do hủy
            var noteBuilder = new StringBuilder(order.Note ?? string.Empty);
            noteBuilder.AppendLine($"Auto-cancelled at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Payment timeout (not paid within required timeframe)");
            order.Note = noteBuilder.ToString();
            
            order.UpdatedBy = "System";
            order.UpdatedAt = DateTime.UtcNow;

            // Giải phóng xe
            if (!string.IsNullOrEmpty(order.CarEVDetailId))
            {
                var car = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(order.CarEVDetailId);
                if (car != null && car.Status == CarEvStatus.RESERVED)
                {
                    car.Status = CarEvStatus.AVAILABLE;
                    car.UpdatedBy = "System";
                    car.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.CarEVRepository.UpdateCarEVAsync(car);
                }
            }

            await _unitOfWork.OrderRepository.UpdateOrderBookingAsync(order);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}