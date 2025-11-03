using EVSRS.BusinessObjects.DTO.OrderBookingDto;
using EVSRS.BusinessObjects.DTO.SepayDto;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Helper;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderBookingController : ControllerBase
    {
        private readonly IOrderBookingService _orderBookingService;

        public OrderBookingController(IOrderBookingService orderBookingService)
        {
            _orderBookingService = orderBookingService;
        }

        /// <summary>
        /// Get all order bookings with pagination
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllOrderBookings(int pageNumber = 1, int pageSize = 10)
        {
            var bookings = await _orderBookingService.GetAllOrderBookingsAsync(pageNumber, pageSize);
            return Ok(new ResponseModel<PaginatedList<OrderBookingResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                bookings,
                "Order bookings retrieved successfully"
            ));
        }

        /// <summary>
        /// Get order booking by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetOrderBookingById(string id)
        {
            var booking = await _orderBookingService.GetOrderBookingByIdAsync(id);
            return Ok(new ResponseModel<OrderBookingResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                booking,
                "Order booking retrieved successfully"
            ));
        }

        /// <summary>
        /// Get order bookings by user ID
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetOrderBookingsByUserId(string userId)
        {
            var bookings = await _orderBookingService.GetOrderBookingsByUserIdAsync(userId);
            return Ok(new ResponseModel<List<OrderBookingResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                bookings,
                "User's order bookings retrieved successfully"
            ));
        }

        /// <summary>
        /// Get current user's order bookings
        /// </summary>
        [HttpGet("my-bookings")]
        [Authorize]
        public async Task<IActionResult> GetMyOrderBookings()
        {
            var userId = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest(new ResponseModel<string>(
                    StatusCodes.Status400BadRequest,
                    ApiCodes.BAD_REQUEST,
                    null,
                    "User ID not found in token"
                ));
            }

            var bookings = await _orderBookingService.GetOrderBookingsByUserIdAsync(userId);
            return Ok(new ResponseModel<List<OrderBookingResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                bookings,
                "Your order bookings retrieved successfully"
            ));
        }

        /// <summary>
        /// Get order bookings by depot ID
        /// </summary>
        [HttpGet("depot/{depotId}")]
        [Authorize]
        public async Task<IActionResult> GetOrderBookingsByDepotId(string depotId)
        {
            var bookings = await _orderBookingService.GetOrderBookingsByDepotIdAsync(depotId);
            return Ok(new ResponseModel<List<OrderBookingResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                bookings,
                "Depot's order bookings retrieved successfully"
            ));
        }

        /// <summary>
        /// Create order booking (for app users) - Returns QR code for payment
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrderBooking([FromBody] OrderBookingRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _orderBookingService.CreateOrderBookingAsync(request);
            return Created("", new ResponseModel<object>(
                StatusCodes.Status201Created,
                ApiCodes.SUCCESS,
                result,
                "Order booking created successfully"
            ));
        }

        /// <summary>
        /// Create offline order booking (for depot staff)
        /// </summary>
        [HttpPost("offline")]
        [Authorize]
        public async Task<IActionResult> CreateOfflineOrderBooking([FromBody] OrderBookingOfflineRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _orderBookingService.CreateOfflineOrderBookingAsync(request);
            return Created("", new ResponseModel<SepayQrResponse>(
                StatusCodes.Status201Created,
                ApiCodes.SUCCESS,
                result,
                "Offline order booking created successfully"
            ));
        }

        /// <summary>
        /// Update order booking
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateOrderBooking(string id, [FromBody] OrderBookingRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _orderBookingService.UpdateOrderBookingAsync(id, request);
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Order booking updated successfully"
            ));
        }

        /// <summary>
        /// Update order booking status
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateOrderStatus(string id, [FromBody] UpdateOrderStatusRequest request)
        {
            var result = await _orderBookingService.UpdateOrderStatusAsync(id, request.Status, request.PaymentStatus);
            return Ok(new ResponseModel<OrderBookingResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Order status updated successfully"
            ));
        }

        /// <summary>
        /// Check car availability
        /// </summary>
        [HttpGet("check-availability")]
        public async Task<IActionResult> CheckCarAvailability(string carId, DateTime startDate, DateTime endDate, string? excludeBookingId = null)
        {
            var isAvailable = await _orderBookingService.CheckCarAvailabilityAsync(carId, startDate, endDate, excludeBookingId);
            return Ok(new ResponseModel<object>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                new { IsAvailable = isAvailable },
                "Car availability checked successfully"
            ));
        }

        /// <summary>
        /// Calculate booking cost
        /// </summary>
        [HttpGet("calculate-cost")]
        public async Task<IActionResult> CalculateBookingCost(string carId, DateTime startDate, DateTime endDate)
        {
            var cost = await _orderBookingService.CalculateBookingCostAsync(carId, startDate, endDate);
            var depositAmount = cost * 0.3m;
            var remainingAmount = cost - depositAmount;

            return Ok(new ResponseModel<object>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                new 
                { 
                    TotalCost = cost,
                    DepositAmount = depositAmount,
                    RemainingAmount = remainingAmount
                },
                "Booking cost calculated successfully"
            ));
        }

       

        /// <summary>
        /// Checkout order (start rental)
        /// </summary>
        [HttpPost("{id}/checkout")]
        [Authorize]
        public async Task<IActionResult> CheckoutOrder(string id)
        {
            var result = await _orderBookingService.CheckoutOrderAsync(id);
            return Ok(new ResponseModel<OrderBookingResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Order checked out successfully"
            ));
        }

        /// <summary>
        /// Start order (customer begins using car)
        /// </summary>
        [HttpPost("{id}/start")]
        [Authorize]
        public async Task<IActionResult> StartOrder(string id)
        {
            var result = await _orderBookingService.StartOrderAsync(id);
            return Ok(new ResponseModel<OrderBookingResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Order started successfully"
            ));
        }

        /// <summary>
        /// Process return (customer returns car)
        /// </summary>
        [HttpPost("{id}/process-return")]
        [Authorize]
        public async Task<IActionResult> ProcessReturnOrder(string id)
        {
            var result = await _orderBookingService.ProcessReturnOrderAsync(id);
            return Ok(new ResponseModel<OrderBookingResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Order return processed successfully"
            ));
        }

        /// <summary>
        /// Complete order (finalize and close)
        /// </summary>
        [HttpPost("{id}/complete")]
        [Authorize]
        public async Task<IActionResult> CompleteOrder(string id)
        {
            var result = await _orderBookingService.CompleteOrderAsync(id);
            return Ok(new ResponseModel<OrderBookingResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Order completed successfully"
            ));
        }

        /// <summary>
        /// Return order (end rental)
        /// </summary>
        [HttpPost("{id}/return")]
        [Authorize]
        public async Task<IActionResult> ReturnOrder(string id)
        {
            var result = await _orderBookingService.ReturnOrderAsync(id);
            return Ok(new ResponseModel<OrderBookingResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Order returned successfully"
            ));
        }

        /// <summary>
        /// Cancel order booking
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(string id, [FromBody] CancelOrderRequest request)
        {
            var result = await _orderBookingService.CancelOrderAsync(id, request.Reason);
            return Ok(new ResponseModel<OrderBookingResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Order cancelled successfully"
            ));
        }

        /// <summary>
        /// Get refund-pending orders for admin
        /// </summary>
        [HttpGet("admin/refunds")]
        [Authorize]
        public async Task<IActionResult> GetRefundPendingOrders(int pageNumber = 1, int pageSize = 10)
        {
            var result = await _orderBookingService.GetRefundPendingOrdersAsync(pageNumber, pageSize);
            return Ok(new ResponseModel<PaginatedList<OrderBookingResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Refund pending orders retrieved successfully"
            ));
        }

        /// <summary>
        /// Admin confirm refund and set order to CANCELLED
        /// </summary>
        [HttpPost("admin/{id}/confirm-refund")]
        [Authorize]
        public async Task<IActionResult> ConfirmRefund(string id, [FromBody] ConfirmRefundRequest request)
        {
            var result = await _orderBookingService.ConfirmRefundAsync(id, request.RefundedAmount, request.AdminNote);
            return Ok(new ResponseModel<OrderBookingResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Refund confirmed and order cancelled successfully"
            ));
        }

        /// <summary>
        /// Delete order booking
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteOrderBooking(string id)
        {
            await _orderBookingService.DeleteOrderBookingAsync(id);
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Order booking deleted successfully"
            ));
        }
    }

    public class UpdateOrderStatusRequest
    {
        public OrderBookingStatus Status { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
    }

    public class CancelOrderRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class ConfirmRefundRequest
    {
        public decimal? RefundedAmount { get; set; }
        public string? AdminNote { get; set; }
    }
}