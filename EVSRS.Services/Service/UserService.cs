using System;
using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;

namespace EVSRS.Services.Service;

public class UserService : IUserService
{
    public Task DeleteUserAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public Task<PaginatedList<UserResponseDto>> GetAllUserAsync()
    {
        throw new NotImplementedException();
    }

    public Task<UserResponseDto> GetUserByIdAsync(string id)
    {
        throw new NotImplementedException();
    }

    public Task<UserResponseDto> RegisterUserAsync(RegisterUserRequestDto registerUserRequestDto)
    {
        throw new NotImplementedException();
    }
}
