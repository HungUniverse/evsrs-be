using AutoMapper;
using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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

    public async Task<UserResponseDto> CreateStaffAsync(CreateStaffRequestDto createStaffRequestDto)
    {
        await _validationService.ValidateAndThrowAsync(createStaffRequestDto);

        // Check if depot exists
        var depot = await _unitOfWork.DepotRepository.GetByIdAsync(createStaffRequestDto.DepotId);
        _validationService.CheckNotFound(depot, "Depot not found");

        // Check if email already exists
        var existingUser = await _unitOfWork.UserRepository.GetUserByEmailAsync(createStaffRequestDto.UserEmail);
        _validationService.CheckBadRequest(existingUser != null, "Email already exists");

        // Check if username already exists
        var existingUserName = await _unitOfWork.UserRepository.GetUserByUsernameAsync(createStaffRequestDto.UserName);
        _validationService.CheckBadRequest(existingUserName != null, "Username already exists");

        var newStaff = _mapper.Map<ApplicationUser>(createStaffRequestDto);
        newStaff.Id = Guid.NewGuid().ToString();
        newStaff.CreatedBy = GetCurrentUserName();
        newStaff.CreatedAt = DateTime.UtcNow;
        newStaff.UpdatedAt = DateTime.UtcNow;
        newStaff.Role = Role.STAFF;
        newStaff.IsVerify = true; // Staff accounts are pre-verified

        // Generate temporary password
        var tempPassword = GenerateTemporaryPassword();
        var salt = GenerateSalt();
        newStaff.Salt = salt;
        newStaff.HashPassword = HashPassword(tempPassword, salt);

        await _unitOfWork.UserRepository.CreateUserAsync(newStaff);
        await _unitOfWork.SaveChangesAsync();

        // TODO: Send email with temporary password to staff

        var result = await _unitOfWork.UserRepository.GetUserByIdAsync(newStaff.Id);
        return _mapper.Map<UserResponseDto>(result);
    }

    public async Task<PaginatedList<UserResponseDto>> GetStaffByDepotIdAsync(string depotId, int pageNumber, int pageSize)
    {
        var staffList = await _unitOfWork.UserRepository.GetStaffByDepotIdAsync(depotId, pageNumber, pageSize);
        var staffDtos = staffList.Items.Select(s => _mapper.Map<UserResponseDto>(s)).ToList();
        return new PaginatedList<UserResponseDto>(staffDtos, staffList.TotalCount, pageNumber, pageSize);
    }

    private string GenerateTemporaryPassword()
    {
        // Generate a random 8-character password
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string GenerateSalt()
    {
        const int saltLength = 32;
        var salt = new byte[saltLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }
        return Convert.ToBase64String(salt);
    }

    private string HashPassword(string password, string salt)
    {
        using (var sha256 = SHA256.Create())
        {
            var saltedPassword = password + salt;
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    private string GetCurrentUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
    }

    public async Task UpdateStaffDepotIdAsync(string userId, string depotId)
    {
        var existingStaff = await _unitOfWork.UserRepository.GetUserByIdAsync(userId);
        if (existingStaff == null || existingStaff.Role != Role.STAFF)
        {
            throw new KeyNotFoundException($"Staff with ID {userId} not found.");
        }
        var depot = await _unitOfWork.DepotRepository.GetByIdAsync(depotId);
        if (depot == null)
        {
            throw new KeyNotFoundException($"Depot with ID {depotId} not found.");
        }
        existingStaff.DepotId = depotId;
        existingStaff.UpdatedAt = DateTime.UtcNow;
        existingStaff.UpdatedBy = GetCurrentUserName();
        await _unitOfWork.UserRepository.UpdateAsync(existingStaff);
        await _unitOfWork.SaveChangesAsync();
    }
}
