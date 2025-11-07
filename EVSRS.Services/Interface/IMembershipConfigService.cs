using EVSRS.BusinessObjects.DTO.MembershipDto;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.Services.Interface
{
    public interface IMembershipConfigService
    {
        Task<List<MembershipConfigResponseDto>> GetAllMembershipConfigsAsync();
        Task<MembershipConfigResponseDto?> GetMembershipConfigByIdAsync(string id);
        Task<MembershipConfigResponseDto?> GetMembershipConfigByLevelAsync(MembershipLevel level);
        Task<MembershipConfigResponseDto> CreateMembershipConfigAsync(CreateMembershipConfigDto dto);
        Task<MembershipConfigResponseDto> UpdateMembershipConfigAsync(string id, UpdateMembershipConfigDto dto);
        Task DeleteMembershipConfigAsync(string id);
    }
}
