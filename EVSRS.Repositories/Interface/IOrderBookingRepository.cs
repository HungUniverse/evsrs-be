using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Repositories.Interface
{
    public interface IOrderBookingRepository : IGenericRepository<OrderBooking>
    {
        Task<PaginatedList<OrderBooking>> GetOrderBookingListAsync(int pageNumber = 1, int pageSize = 10);
        Task<OrderBooking?> GetOrderBookingByIdAsync(string id);
        Task<OrderBooking?> GetByCodeAsync(string code);
        Task<List<OrderBooking>> GetOrderBookingByUserIdAsync(string userId);
        Task<List<OrderBooking>> GetOrderBookingByDepotIdAsync(string depotId);
        Task<List<OrderBooking>> GetOrderBookingByCarIdAsync(string carId);
        Task CreateOrderBookingAsync(OrderBooking orderBooking);
        Task UpdateOrderBookingAsync(OrderBooking orderBooking);
        Task DeleteOrderBookingAsync(OrderBooking orderBooking);
        Task<bool> IsCarAvailableAsync(string carId, DateTime startDate, DateTime endDate);
        Task<List<OrderBooking>> GetActiveOrderBookingsAsync();
        Task<List<OrderBooking>> GetPendingPaymentOrderBookingsAsync();
    }
}