using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.Repositories.Infrastructure;

namespace EVSRS.Services.Interface;

public interface IUserService
{
    Task<PaginatedList<UserResponseDto>> GetAllUserAsync(int pageNumber, int pageSize);
    Task<UserResponseDto> GetUserByIdAsync(string id);
    Task<UserResponseDto> GetUserByEmailAsync(string email);    
    Task<UserResponseDto> GetUserByPhoneAsync(string phone);
    Task<UserResponseDto> GetUserByUsernameAsync(string username);
    Task UpdateUserAsync(string userId, UserRequestDto updateUserRequestDto);
    Task UpdateStaffDepotIdAsync(string userId, string depotId);

    Task<UserResponseDto> RegisterUserAsync(RegisterUserRequestDto registerUserRequestDto);
    Task<UserResponseDto> CreateStaffAsync(CreateStaffRequestDto createStaffRequestDto);
    Task<PaginatedList<UserResponseDto>> GetStaffByDepotIdAsync(string depotId, int pageNumber, int pageSize);
    Task DeleteUserAsync(string userId);
}