using AutoMapper;
using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using System;

namespace EVSRS.Services.Service;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidationService _validationService;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IValidationService validationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _validationService = validationService;
    }


    public async Task DeleteUserAsync(string userId)
    {
        await _validationService.ValidateAndThrowAsync(userId);
        var existingUser = await _unitOfWork.UserRepository.GetUserByIdAsync(userId);
        if (existingUser == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }
        existingUser.IsDeleted = true;
        existingUser.UpdatedAt = DateTime.UtcNow;
        existingUser.UpdatedBy = GetCurrentUserName();
        await _unitOfWork.UserRepository.UpdateAsync(existingUser);
        await _unitOfWork.SaveChangesAsync();

    }

    public async Task<PaginatedList<UserResponseDto>> GetAllUserAsync(int pageNumber, int pageSize)
    {
        var users = await _unitOfWork.UserRepository.GetUsersAsync();
        var userDtos = users.Items.Select(u => _mapper.Map<UserResponseDto>(u)).ToList();
        var paginatedList = PaginatedList<UserResponseDto>.Create(userDtos, pageNumber, pageSize);
        return paginatedList;


    }

    public async Task<UserResponseDto> GetUserByEmailAsync(string email)
    {
        var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(email);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with email {email} not found.");
        }
        var userDto = _mapper.Map<UserResponseDto>(user);
        return userDto;

    }

    public async Task<UserResponseDto> GetUserByIdAsync(string id)
    {
        var user = await _unitOfWork.UserRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {id} not found.");
        }
        var userDto = _mapper.Map<UserResponseDto>(user);
        return userDto;
    }

    public async Task<UserResponseDto> GetUserByPhoneAsync(string phone)
    {
        var user = await _unitOfWork.UserRepository.GetByPhoneAsync(phone);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with phone number {phone} not found.");
        }
        var userDto = _mapper.Map<UserResponseDto>(user);
        return userDto;
    }

    public async Task<UserResponseDto> GetUserByUsernameAsync(string username)
    {
        var user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with username {username} not found.");
        }
        var userDto = _mapper.Map<UserResponseDto>(user);
        return userDto;
    }

    public Task<UserResponseDto> RegisterUserAsync(RegisterUserRequestDto registerUserRequestDto)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateUserAsync(string userId, UserRequestDto updateUserRequestDto)
    {
        await _validationService.ValidateAndThrowAsync(updateUserRequestDto);
        var existingUser = await _unitOfWork.UserRepository.GetUserByIdAsync(userId);
        if (existingUser == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }
        existingUser.UpdatedBy = GetCurrentUserName();
        existingUser.UpdatedAt = DateTime.UtcNow;
        _mapper.Map(updateUserRequestDto, existingUser);
        await _unitOfWork.UserRepository.UpdateAsync(existingUser);
        await _unitOfWork.SaveChangesAsync();

    }

    private string GetCurrentUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
    }
}
