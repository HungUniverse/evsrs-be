using EVSRS.BusinessObjects.DTO.OrderBookingDto;
using EVSRS.BusinessObjects.DTO.SepayDto;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Interface
{
    public interface IOrderBookingService
    {
        Task<PaginatedList<OrderBookingResponseDto>> GetAllOrderBookingsAsync(int pageNumber, int pageSize);
        Task<OrderBookingResponseDto> GetOrderBookingByIdAsync(string id);
        Task<List<OrderBookingResponseDto>> GetOrderBookingsByUserIdAsync(string userId);
        Task<List<OrderBookingResponseDto>> GetOrderBookingsByDepotIdAsync(string depotId);
        Task<SepayQrResponse> CreateOrderBookingAsync(OrderBookingRequestDto request);
        Task<OrderBookingResponseDto> CreateOfflineOrderBookingAsync(OrderBookingRequestDto request);
        Task UpdateOrderBookingAsync(string id, OrderBookingRequestDto request);
        Task DeleteOrderBookingAsync(string id);
        Task<OrderBookingResponseDto> UpdateOrderStatusAsync(string id, OrderBookingStatus status, PaymentStatus? paymentStatus = null);
        Task<bool> CheckCarAvailabilityAsync(string carId, DateTime startDate, DateTime endDate, string? excludeBookingId = null);
        
        Task<OrderBookingResponseDto> CheckoutOrderAsync(string id);
        Task<OrderBookingResponseDto> StartOrderAsync(string id);
        Task<OrderBookingResponseDto> ProcessReturnOrderAsync(string id);
        Task<OrderBookingResponseDto> CompleteOrderAsync(string id);
        Task<OrderBookingResponseDto> ReturnOrderAsync(string id);
        Task<OrderBookingResponseDto> CancelOrderAsync(string id, string reason);
        Task<decimal> CalculateBookingCostAsync(string carId, DateTime startDate, DateTime endDate);
    }
}