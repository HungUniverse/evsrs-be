using EVSRS.BusinessObjects.DTO.MembershipDto;

namespace EVSRS.Services.Interface
{
    public interface IMembershipService
    {
        Task<MembershipResponseDto?> GetMembershipByUserIdAsync(string userId);
        Task UpdateMembershipAfterOrderCompleteAsync(string userId, decimal orderAmount);
        Task CreateInitialMembershipForUserAsync(string userId);
    }
}
