using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.Repositories.Infrastructure;

namespace EVSRS.Services.Interface;

public interface IUserService
{
    Task<PaginatedList<UserResponseDto>> GetAllUserAsync();
    Task<UserResponseDto> GetUserByIdAsync(string id);
    Task<UserResponseDto> RegisterUserAsync(RegisterUserRequestDto registerUserRequestDto);
    Task DeleteUserAsync(string userId);
}