using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Repositories.Repository
{
    public class OrderBookingRepository : GenericRepository<OrderBooking>, IOrderBookingRepository
    {
        public OrderBookingRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public async Task CreateOrderBookingAsync(OrderBooking orderBooking)
        {
            await InsertAsync(orderBooking);
        }

        public async Task DeleteOrderBookingAsync(OrderBooking orderBooking)
        {
            await DeleteAsync(orderBooking);
        }

        public async Task<List<OrderBooking>> GetActiveOrderBookingsAsync()
        {
            return await _dbSet
                .Where(x => !x.IsDeleted && 
                           (x.Status == OrderBookingStatus.CONFIRMED ||
                            x.Status == OrderBookingStatus.CHECKED_OUT ||
                            x.Status == OrderBookingStatus.IN_USE))
                .Include(x => x.User)
                .Include(x => x.CarEvs)
                .ThenInclude(c => c.Model)
                .Include(x => x.Depot)
                .ToListAsync();
        }

        public async Task<OrderBooking?> GetByCodeAsync(string code)
        {
            return await _dbSet
                .Where(x => !x.IsDeleted && (x.Code == code || x.Id == code)) // Check both Code field and Id
                .Include(x => x.User)
                .Include(x => x.CarEvs)
                .ThenInclude(c => c.Model)
                .Include(x => x.Depot)
                .Include(x => x.Transactions)
                .FirstOrDefaultAsync();
        }

        public async Task<OrderBooking?> GetOrderBookingByIdAsync(string id)
        {
            return await _dbSet
                .Where(x => !x.IsDeleted && x.Id == id)
                .Include(x => x.User)
                .Include(x => x.CarEvs)
                .ThenInclude(c => c.Model)
                .Include(x => x.Depot)
                .Include(x => x.Transactions)
                .Include(x => x.Feedbacks)
                .Include(x => x.Contracts)
                .Include(x => x.HandoverInspections)
                .FirstOrDefaultAsync();
        }

        public async Task<List<OrderBooking>> GetOrderBookingByCarIdAsync(string carId)
        {
            return await _dbSet
                .Where(x => !x.IsDeleted && x.CarEVDetailId == carId)
                .Include(x => x.User)
                .Include(x => x.CarEvs)
                .ThenInclude(c => c.Model)
                .Include(x => x.Depot)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<OrderBooking>> GetOrderBookingByDepotIdAsync(string depotId)
        {
            return await _dbSet
                .Where(x => !x.IsDeleted && x.DepotId == depotId)
                .Include(x => x.User)
                .Include(x => x.CarEvs)
                .ThenInclude(c => c.Model)
                .Include(x => x.Depot)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<OrderBooking>> GetOrderBookingByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(x => !x.IsDeleted && x.UserId == userId)
                .Include(x => x.User)
                .Include(x => x.CarEvs)
                .ThenInclude(c => c.Model)
                .Include(x => x.Depot)
                .Include(x => x.Transactions)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<PaginatedList<OrderBooking>> GetOrderBookingListAsync(int pageNumber = 1, int pageSize = 10)
        {
            var query = _dbSet
                .Where(x => !x.IsDeleted)
                .Include(x => x.User)
                .Include(x => x.CarEvs)
                .ThenInclude(c => c.Model)
                .Include(x => x.Depot)
                .OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedList<OrderBooking>(items, totalCount, pageNumber, pageSize);
        }

        public async Task<List<OrderBooking>> GetPendingPaymentOrderBookingsAsync()
        {
            return await _dbSet
                .Where(x => !x.IsDeleted && 
                           (x.PaymentStatus == PaymentStatus.PENDING ||
                            x.PaymentStatus == PaymentStatus.PAID_DEPOSIT))
                .Include(x => x.User)
                .Include(x => x.CarEvs)
                .ThenInclude(c => c.Model)
                .Include(x => x.Depot)
                .ToListAsync();
        }

        public async Task<bool> IsCarAvailableAsync(string carId, DateTime startDate, DateTime endDate)
        {
            var conflictingBookings = await _dbSet
                .Where(x => !x.IsDeleted && 
                           x.CarEVDetailId == carId &&
                           x.Status != OrderBookingStatus.CANCELLED &&
                           x.Status != OrderBookingStatus.COMPLETED &&
                           ((x.StartAt <= endDate && x.EndAt >= startDate)))
                .AnyAsync();

            return !conflictingBookings;
        }

        public async Task UpdateOrderBookingAsync(OrderBooking orderBooking)
        {
            await UpdateAsync(orderBooking);
        }
    }
}