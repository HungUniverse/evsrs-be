using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EVSRS.Repositories.Repository;

public class HandoverInspectionRepository : GenericRepository<HandoverInspection>, IHandoverInspectionRepository
{
    public HandoverInspectionRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

    public async Task<List<HandoverInspection>> GetHandoverInspectionsByOrderIdAsync(string orderBookingId)
    {
        return await _context.HandoverInspections
            .Where(hi => hi.OrderBookingId == orderBookingId)
            .OrderByDescending(hi => hi.CreatedAt)
            .ToListAsync();
    }

    public async Task<HandoverInspection?> GetHandoverInspectionByOrderAndTypeAsync(string orderBookingId, string type)
    {
        return await _context.HandoverInspections
            .FirstOrDefaultAsync(hi => hi.OrderBookingId == orderBookingId && hi.Type == type);
    }

    public async Task<List<HandoverInspection>> GetHandoverInspectionsByStaffIdAsync(string staffId)
    {
        return await _context.HandoverInspections
            .Where(hi => hi.StaffId == staffId)
            .Include(hi => hi.OrderBooking)
            .OrderByDescending(hi => hi.CreatedAt)
            .ToListAsync();
    }

    public async Task<HandoverInspection?> GetLatestHandoverInspectionAsync(string orderBookingId)
    {
        return await _context.HandoverInspections
            .Where(hi => hi.OrderBookingId == orderBookingId)
            .OrderByDescending(hi => hi.CreatedAt)
            .FirstOrDefaultAsync();
    }
}