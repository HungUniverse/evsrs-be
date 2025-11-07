using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;

namespace EVSRS.Repositories.Interface
{
    public interface IMembershipRepository : IGenericRepository<Membership>
    {
        Task<Membership?> GetByUserIdAsync(string userId);
        Task<List<Membership>> GetMembershipsByConfigIdAsync(string membershipConfigId);
        Task CreateMembershipAsync(Membership membership);
        Task UpdateMembershipAsync(Membership membership);
        Task DeleteMembershipAsync(Membership membership);
    }
}
