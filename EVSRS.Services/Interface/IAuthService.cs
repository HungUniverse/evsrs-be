using System;
using EVSRS.BusinessObjects.DTO.AuthDto;
using EVSRS.BusinessObjects.DTO.TokenDto;
using EVSRS.BusinessObjects.DTO.UserDto;

namespace EVSRS.Services.Interface;

public interface IAuthService
{
    Task SendRegisterOtpAsync(SendOTPRequestDto request);
    Task VerifyOtpAsync(VerifyOtpRequestDto request);
    Task LogoutAsync(LogoutRequestDto model);
    Task<TokenResponseDto> SignInAsync(LoginRequestDto model);
    Task ResendOtpAsync(SendOTPRequestDto model);
    Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto model);
    Task CompleteRegisterAsync(RegisterUserRequestDto model);
    Task<TokenResponseDto> SignInWithGoogleJwtAsync(string jwtToken, string? notificationToken = null);
    Task<RegisterUserAtDepotResponseDto> RegisterUserAtDepotAsync(RegisterUserAtDepotRequestDto request);
}
