using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;

namespace EVSRS.Repositories.Interface;

public interface IHandoverInspectionRepository : IGenericRepository<HandoverInspection>
{
    Task<List<HandoverInspection>> GetHandoverInspectionsByOrderIdAsync(string orderBookingId);
    Task<HandoverInspection?> GetHandoverInspectionByOrderAndTypeAsync(string orderBookingId, string type);
    Task<List<HandoverInspection>> GetHandoverInspectionsByStaffIdAsync(string staffId);
    Task<HandoverInspection?> GetLatestHandoverInspectionAsync(string orderBookingId);
}