using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EVSRS.Repositories.Repository;

public class ContractRepository : GenericRepository<Contract>, IContractRepository
{
    public ContractRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
    {
    }

    public async Task<Contract?> GetContractByOrderIdAsync(string orderBookingId)
    {
        return await _context.Contracts
            .Include(c => c.Users)
            .Include(c => c.OrderBooking)
            .FirstOrDefaultAsync(c => c.OrderBookingId == orderBookingId);
    }

    public async Task<List<Contract>> GetContractsByUserIdAsync(string userId)
    {
        return await _context.Contracts
            .Include(c => c.OrderBooking)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Contract?> GetContractByNumberAsync(string contractNumber)
    {
        return await _context.Contracts
            .Include(c => c.Users)
            .Include(c => c.OrderBooking)
            .FirstOrDefaultAsync(c => c.ContractNumber == contractNumber);
    }

    public async Task<List<Contract>> GetContractsByStatusAsync(SignStatus signStatus)
    {
        return await _context.Contracts
            .Include(c => c.Users)
            .Include(c => c.OrderBooking)
            .Where(c => c.SignStatus == signStatus)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Contract>> GetContractsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Contracts
            .Include(c => c.Users)
            .Include(c => c.OrderBooking)
            .Where(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<PaginatedList<Contract>> GetAllContractsAsync()
    {
        var response = await _dbSet.Include(x => x.Users).Include(x => x.OrderBooking).Where(x => !x.IsDeleted).ToListAsync();
        return PaginatedList<Contract>.Create(response, 1, response.Count);
    }
}