using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;

namespace EVSRS.Repositories.Interface;

public interface IContractRepository : IGenericRepository<Contract>
{
    Task<Contract?> GetContractByOrderIdAsync(string orderBookingId);
    Task<List<Contract>> GetContractsByUserIdAsync(string userId);
    Task<Contract?> GetContractByNumberAsync(string contractNumber);
    Task<List<Contract>> GetContractsByStatusAsync(SignStatus signStatus);
    Task<List<Contract>> GetContractsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<PaginatedList<Contract>> GetAllContractsAsync();
}