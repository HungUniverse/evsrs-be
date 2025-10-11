using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;

namespace EVSRS.Repositories.Interface;

public interface IReturnSettlementRepository : IGenericRepository<ReturnSettlement>
{
    Task<ReturnSettlement?> GetReturnSettlementByOrderIdAsync(string orderBookingId);
    Task<List<ReturnSettlement>> GetReturnSettlementsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<ReturnSettlement?> GetReturnSettlementWithItemsAsync(string id);
}