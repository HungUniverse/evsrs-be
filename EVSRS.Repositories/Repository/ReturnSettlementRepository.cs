using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EVSRS.Repositories.Repository;

public class ReturnSettlementRepository : GenericRepository<ReturnSettlement>, IReturnSettlementRepository
{
    public ReturnSettlementRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

    public async Task<ReturnSettlement?> GetReturnSettlementByOrderIdAsync(string orderBookingId)
    {
        return await _context.ReturnSettlements
            .Include(rs => rs.SettlementItems)
            .FirstOrDefaultAsync(rs => rs.OrderBookingId == orderBookingId);
    }

    public async Task<List<ReturnSettlement>> GetReturnSettlementsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.ReturnSettlements
            .Include(rs => rs.SettlementItems)
            .Include(rs => rs.OrderBooking)
            .Where(rs => rs.CalculateAt >= startDate && rs.CalculateAt <= endDate)
            .OrderByDescending(rs => rs.CalculateAt)
            .ToListAsync();
    }

    public async Task<ReturnSettlement?> GetReturnSettlementWithItemsAsync(string id)
    {
        return await _context.ReturnSettlements
            .Include(rs => rs.SettlementItems)
            .Include(rs => rs.OrderBooking)
            .FirstOrDefaultAsync(rs => rs.Id == id);
    }
}