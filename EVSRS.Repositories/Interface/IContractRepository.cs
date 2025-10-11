using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;

namespace EVSRS.Repositories.Interface;

public interface IContractRepository : IGenericRepository<Contract>
{
    Task<Contract?> GetContractByOrderIdAsync(string orderBookingId);
    Task<List<Contract>> GetContractsByUserIdAsync(string userId);
    Task<Contract?> GetContractByNumberAsync(string contractNumber);
    Task<List<Contract>> GetContractsByStatusAsync(string signStatus);
    Task<List<Contract>> GetContractsByDateRangeAsync(DateTime startDate, DateTime endDate);
}