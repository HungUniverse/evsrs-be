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

        public async Task<bool> IsCarAvailableAsync(string carId, DateTime startDate, DateTime endDate, string? excludeBookingId = null)
        {
            // Kiểm tra overlap thời gian: booking mới overlap với booking hiện tại nếu:
            // startDate <= existingEndDate && endDate >= existingStartDate
            var query = _dbSet
                .Where(x => !x.IsDeleted &&
                           x.CarEVDetailId == carId &&
                           x.Status != OrderBookingStatus.CANCELLED &&
                           x.Status != OrderBookingStatus.COMPLETED &&
                           x.Status != OrderBookingStatus.RETURNED &&
                           x.StartAt <= endDate && x.EndAt >= startDate); // Kiểm tra overlap thời gian
            
            // Loại trừ booking hiện tại nếu có (để update)
            if (!string.IsNullOrEmpty(excludeBookingId))
            {
                query = query.Where(x => x.Id != excludeBookingId);
            }

            var conflictingBookings = await query.AnyAsync();

            return !conflictingBookings;
        }

        public async Task<bool> IsCarAvailableWithBufferAsync(string carId, DateTime startDate, DateTime endDate, int bufferMinutes, string? excludeBookingId = null)
        {
            // Kiểm tra overlap với buffer time:
            // Booking mới cần có khoảng cách tối thiểu bufferMinutes với các booking hiện tại
            // 
            // Ví dụ: Buffer 60 phút
            // Booking hiện tại: 7:00 - 11:00
            // → Booking mới KHÔNG được trong khoảng: 11:00 - bufferMinutes đến 7:00 + bufferMinutes
            // → Tức là: (5:00 đến 12:00) bị block
            //
            // Logic: Thêm buffer vào cả 2 đầu của booking hiện tại để kiểm tra overlap
            var query = _dbSet
                .Where(x => !x.IsDeleted &&
                           x.CarEVDetailId == carId &&
                           x.Status != OrderBookingStatus.CANCELLED &&
                           x.Status != OrderBookingStatus.COMPLETED &&
                           x.Status != OrderBookingStatus.RETURNED);
            
            // Loại trừ booking hiện tại nếu có (để update)
            if (!string.IsNullOrEmpty(excludeBookingId))
            {
                query = query.Where(x => x.Id != excludeBookingId);
            }

            var existingBookings = await query.ToListAsync();

            foreach (var existing in existingBookings)
            {
                // Skip nếu booking không có StartAt hoặc EndAt
                if (!existing.StartAt.HasValue || !existing.EndAt.HasValue)
                    continue;
                
                // Thêm buffer vào thời gian của booking hiện tại
                var existingStartWithBuffer = existing.StartAt.Value.AddMinutes(-bufferMinutes);
                var existingEndWithBuffer = existing.EndAt.Value.AddMinutes(bufferMinutes);
                
                // Kiểm tra overlap với booking có buffer
                bool hasOverlap = startDate <= existingEndWithBuffer && endDate >= existingStartWithBuffer;
                
                if (hasOverlap)
                {
                    return false; // Có conflict
                }
            }

            return true; // Không có conflict
        }

        public async Task UpdateOrderBookingAsync(OrderBooking orderBooking)
        {
            await UpdateAsync(orderBooking);
        }

        public async Task<IEnumerable<OrderBooking>> GetExpiredUnpaidOrdersAsync(DateTime cutoffTime)
        {
            return await _dbSet
                .Include(o => o.User)
                .Where(o => o.Status == OrderBookingStatus.PENDING && 
                           o.PaymentStatus == PaymentStatus.PENDING &&
                           o.CreatedAt < cutoffTime &&
                           !o.IsDeleted)
                .ToListAsync();
        }
    }
}