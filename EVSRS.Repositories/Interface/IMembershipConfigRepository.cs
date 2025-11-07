using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;

namespace EVSRS.Repositories.Interface
{
    public interface IMembershipConfigRepository : IGenericRepository<MembershipConfig>
    {
        Task<List<MembershipConfig>> GetAllMembershipConfigsAsync();
        Task<MembershipConfig?> GetMembershipConfigByIdAsync(string id);
        Task<MembershipConfig?> GetMembershipConfigByLevelAsync(MembershipLevel level);
        Task<MembershipConfig?> GetMembershipConfigForAmountAsync(decimal totalAmount);
        Task CreateMembershipConfigAsync(MembershipConfig config);
        Task UpdateMembershipConfigAsync(MembershipConfig config);
        Task DeleteMembershipConfigAsync(MembershipConfig config);
        Task<bool> IsLevelExistsAsync(MembershipLevel level);
    }
}
