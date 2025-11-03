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

            // Tính số giờ thuê - làm tròn lên nếu có phút lẻ
            var duration = endDate - startDate;
            var totalHours = (int)Math.Ceiling(duration.TotalHours);
            if (totalHours <= 0) totalHours = 1; // Tối thiểu 1 giờ
            
            // Ví dụ: 17:10 - 16:00 = 1 giờ 10 phút -> làm tròn thành 2 giờ

            // Giá thuê theo ngày và giảm giá
            var dailyPrice = (decimal)(model!.Price ?? 0);
            var discount = (decimal)(model.Sale ?? 0) / 100;
            var discountedDailyPrice = dailyPrice * (1 - discount);

            // Tính giá thuê theo giờ: (giá_ngày_sau_giảm_giá / 24) * số_giờ
            var hourlyRate = discountedDailyPrice / 24;
            var totalCost = hourlyRate * totalHours;

            return totalCost;
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

        public async Task<OrderBookingResponseDto> CreateOfflineOrderBookingAsync(OrderBookingRequestDto request)
        {
            await _validationService.ValidateAndThrowAsync(request);

            // Validate car availability
            var isAvailable = await CheckCarAvailabilityAsync(request.CarEVDetailId, request.StartAt, request.EndAt);
            _validationService.CheckBadRequest(!isAvailable, "Car is not available for the selected dates");

            // Calculate costs
            var totalCost = await CalculateBookingCostAsync(request.CarEVDetailId, request.StartAt, request.EndAt);
            var depositAmount = totalCost * 0.3m; // 30% deposit
            var remainingAmount = totalCost - depositAmount;

            var booking = _mapper.Map<OrderBooking>(request);
            booking.Id = Guid.NewGuid().ToString();
            booking.Code = GenerateBookingCode(); // Generate unique booking code
            booking.UserId = null; // Offline booking doesn't have user
            booking.Status = OrderBookingStatus.PENDING;
            booking.PaymentStatus = PaymentStatus.PENDING;
            booking.SubTotal = totalCost.ToString();
            booking.TotalAmount = totalCost.ToString();
            booking.DepositAmount = request.PaymentType == PaymentType.DEPOSIT ? depositAmount.ToString() : totalCost.ToString();
            booking.RemainingAmount = request.PaymentType == PaymentType.DEPOSIT ? remainingAmount.ToString() : "0";
            booking.CreatedBy = GetCurrentUserName();
            booking.CreatedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            // Store customer info in note for offline booking
            if (request.IsOfflineBooking)
            {
                booking.Note = $"Offline Booking - Customer: {request.CustomerName}, Phone: {request.CustomerPhone}, Email: {request.CustomerEmail}, Address: {request.CustomerAddress}. {request.Note}";
            }

            await _unitOfWork.OrderRepository.CreateOrderBookingAsync(booking);

            // Reserve the car
            var car = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(request.CarEVDetailId);
            if (car != null)
            {
                car.Status = CarEvStatus.RESERVED;
                await _unitOfWork.CarEVRepository.UpdateCarEVAsync(car);
            }

            await _unitOfWork.SaveChangesAsync();

            var result = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(booking.Id);
            return _mapper.Map<OrderBookingResponseDto>(result);
        }

        public async Task<SepayQrResponse> CreateOrderBookingAsync(OrderBookingRequestDto request)
        {
            await _validationService.ValidateAndThrowAsync(request);

            var currentUserId = GetCurrentUserId();
            _validationService.CheckBadRequest(string.IsNullOrEmpty(currentUserId), "User must be logged in to create booking");

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
    }
}