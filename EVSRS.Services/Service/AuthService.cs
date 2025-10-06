using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutoMapper;
using EVSRS.BusinessObjects.DTO.AuthDto;
using EVSRS.BusinessObjects.DTO.TokenDto;
using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Helper;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EVSRS.Services.Service;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IEmailSenderSevice _emailSenderService;
    private readonly IMapper _mapper;
    private readonly IValidationService _validationService;
    public AuthService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IValidationService validationService, IEmailSenderSevice emailSenderService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _validationService = validationService;
        _emailSenderService = emailSenderService;
        _mapper = mapper;
    }
    public async Task CompleteRegisterAsync(RegisterUserRequestDto model)
    {
        await _validationService.ValidateAndThrowAsync(model);
        var user = await _unitOfWork.UserRepository.GetByEmailPhoneAsync(model.Email, model.PhoneNumber);

        if (user == null)
            throw new ErrorException(StatusCodes.Status400BadRequest,
                ApiCodes.BAD_REQUEST, "Invalid email or phone number. Please check your information and try again.");
        if (user.IsVerify == false)
            throw new ErrorException(StatusCodes.Status400BadRequest,
                ApiCodes.BAD_REQUEST, "User is not verified! Please verify your account first.");

        var salt = HashHelper.GenerateSalt();
        var hashPassword = HashHelper.HashPassword(model.Password, salt);
        var emailParts = model.Email.Split(new[] { '@' }, 2);
        user.HashPassword = hashPassword;
        user.Salt = salt;
        user.FullName = emailParts[0];
        user.UserName = emailParts[0];
        user.Role = Role.USER;

        await _unitOfWork.UserRepository.UpdateUserAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task LogoutAsync(LogoutRequestDto model)
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue("userId");
        if (string.IsNullOrWhiteSpace(userId))
            throw new ErrorException(StatusCodes.Status401Unauthorized, "Unauthorized access!");

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto model)
    {
        var userToken = await _unitOfWork.TokenRepository.GetTokenAsync(model.RefreshToken, TokenType.REFRESH_TOKEN);

        if (userToken == null)
            throw new ErrorException(StatusCodes.Status401Unauthorized, ApiCodes.UNAUTHORIZED,
                "Invalid refresh token!");

        if (userToken.ExpiredAt < DateTime.UtcNow)
            throw new ErrorException(StatusCodes.Status401Unauthorized, ApiCodes.TOKEN_EXPIRED,
                "Refresh token has expired!");

        var user = await _unitOfWork.UserRepository.GetByIdAsync(userToken.UserId);
        if (user == null || user.IsDeleted == true)
            throw new ErrorException(StatusCodes.Status404NotFound,
                ApiCodes.NOT_FOUND, "User not found or account has been deleted.");

        string roleName = user.Role.ToString() ?? string.Empty;

        var token = GenerateTokens(user, roleName);
        await HandleTokenAsync(user.Id, model.RefreshToken, TokenType.REFRESH_TOKEN);
        return new TokenResponseDto()
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken
        };
    }
    public static string GenerateOtpCode(int length = 6)
    {
        var random = new Random();
        int min = (int)Math.Pow(10, length - 1);
        int max = (int)Math.Pow(10, length) - 1;
        var code = random.Next(min, max + 1);
        return code.ToString();
    }
    public async Task SendOtpEmailAsync(string email, string otpCode)
    {
        string subject = "Your OTP Code - Eco Rent System";
        string preheader = "Use this OTP code to complete your registration. This code is valid for 5 minutes.";

        string content = $@"
    <!DOCTYPE html>
    <html lang=""en"">
    <head>
        <meta charset=""UTF-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
        <title>Your OTP Code</title>
        <style>
            body {{ font-family: Arial, Helvetica, sans-serif; background: #f7f7f7; margin:0; padding:0; }}
            .container {{ max-width: 480px; margin: 40px auto; background: #fff; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.05); padding: 32px 24px; }}
            .brand {{ font-size: 24px; font-weight: bold; color: #2266cc; margin-bottom: 20px; text-align:center; }}
            .title {{ font-size: 20px; font-weight: bold; margin-bottom: 8px; color: #333; text-align:center; }}
            .otp {{ font-size: 28px; font-weight: bold; color: #e91e63; letter-spacing: 6px; margin: 24px 0; text-align:center; }}
            .message {{ font-size: 16px; color: #333; margin-bottom: 18px; text-align:center; }}
            .footer {{ margin-top: 36px; font-size: 13px; color: #888; text-align:center; }}
        </style>
    </head>
    <body>
        <span style=""display:none!important;"">{preheader}</span>
        <div class=""container"">
            <div class=""brand"">Eco Rent System</div>
            <div class=""title"">Your One-Time Password (OTP)</div>
            <div class=""message"">Hello,<br/>Use the code below to complete your registration.<br/>This code will expire in <b>5 minutes</b>.</div>
            <div class=""otp"">{otpCode}</div>
            <div class=""footer"">
                If you did not request this, please ignore this email.<br>
                &copy; {DateTime.Now.Year} Eco Rent System. All rights reserved.
            </div>
        </div>
    </body>
    </html>";

        await _emailSenderService.SendEmailAsync(email, subject, content);
    }
    public async Task ResendOtpAsync(SendOTPRequestDto model)
    {
        var user = await _unitOfWork.UserRepository.GetByEmailPhoneAsync(model.Email, model.PhoneNumber);
        if (user == null)
            throw new ErrorException(StatusCodes.Status404NotFound,
                ApiCodes.NOT_FOUND, "User not found!");
        if (user.IsVerify)
            throw new ErrorException(StatusCodes.Status400BadRequest,
                ApiCodes.BAD_REQUEST, "User is already verified!");

        string otpCode = GenerateOtpCode();
        var otp = new OTP
        {
            UserId = user.Id,
            Code = otpCode,
            ExpireAt = DateTime.UtcNow.AddMinutes(1),
            OTPType = OTPType.REGISTER
        };
        await _unitOfWork.OTPRepository.CreateOTPAsync(otp);
        await _unitOfWork.SaveChangesAsync();
        await SendOtpEmailAsync(model.Email, otpCode);
    }

    public async Task SendRegisterOtpAsync(SendOTPRequestDto request)
    {
        await _validationService.ValidateAndThrowAsync(request);
        var isExist = await _unitOfWork.UserRepository.GetByEmailPhoneAsync(request.Email, request.PhoneNumber);
        if (isExist == null)
        {
            var user = _mapper.Map<ApplicationUser>(request);
            user.UserEmail = request.Email;
            user.PhoneNumber = request.PhoneNumber;
            user.IsVerify = false;
            await _unitOfWork.UserRepository.CreateUserAsync(user);
            await _unitOfWork.SaveChangesAsync();

            string otpCode = GenerateOtpCode();
            string otpHash = HashHelper.HashOtp(otpCode);
            var otp = new OTP
            {
                UserId = user.Id,
                Code = otpHash,
                ExpireAt = DateTime.UtcNow.AddMinutes(5),
                OTPType = OTPType.REGISTER
            };
            await _unitOfWork.OTPRepository.CreateOTPAsync(otp);
            await _unitOfWork.SaveChangesAsync();

            await SendOtpEmailAsync(request.Email, otpCode);
        }
        else
        {
            if ((isExist.PhoneNumber == request.PhoneNumber || isExist.UserEmail == request.Email) && isExist.IsVerify)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest,
                    ApiCodes.BAD_REQUEST, "Email or phone number has already been registered!");
            }

            var isEmailUsed = await _unitOfWork.UserRepository.GetUserByEmailAsync(request.Email);
            if (isEmailUsed != null && isEmailUsed.IsVerify)
            {
                throw new ErrorException(StatusCodes.Status400BadRequest,
                    ApiCodes.BAD_REQUEST, "Email has already been registered by another user!");
            }

            var oldOtps = await _unitOfWork.OTPRepository.GetOTPAsync(isExist.Id, string.Empty, OTPType.REGISTER);
            if (oldOtps != null)
            {
                await _unitOfWork.OTPRepository.DeleteOTPAsync(oldOtps);
                await _unitOfWork.SaveChangesAsync();
            }

            string otpCode = GenerateOtpCode();
            string otpHash = HashHelper.HashOtp(otpCode);
            var otp = new OTP
            {
                UserId = isExist.Id,
                Code = otpHash,
                ExpireAt = DateTime.UtcNow.AddMinutes(5),
                OTPType = OTPType.REGISTER
            };
            await _unitOfWork.OTPRepository.CreateOTPAsync(otp);
            await _unitOfWork.SaveChangesAsync();

            await SendOtpEmailAsync(request.Email, otpCode);
        }
    }

    public async Task<TokenResponseDto> SignInAsync(LoginRequestDto model)
    {
        ApplicationUser? user = null;

        string loginKey = model.Identifier?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(loginKey))
            throw new ErrorException(StatusCodes.Status400BadRequest,
                ApiCodes.BAD_REQUEST,
                "You must enter an email or username!");

        if (loginKey.Contains("@"))
        {
            string email = loginKey.ToLower();
            user = await _unitOfWork.UserRepository.GetUserByEmailAsync(email);
        }
        else
        {
            user = await _unitOfWork.UserRepository.GetUserByUsernameAsync(loginKey);
        }

        if (user == null)
            throw new ErrorException(StatusCodes.Status401Unauthorized,
                ApiCodes.UNAUTHORIZED,
                $"No account found with the provided information: {loginKey}");

        if (user.IsDeleted)
            throw new ErrorException(StatusCodes.Status403Forbidden,
                ApiCodes.FORBIDDEN, "Your account has been locked or deleted.");
        if (!user.IsVerify)
            throw new ErrorException(StatusCodes.Status401Unauthorized, ApiCodes.UNAUTHORIZED,
                "The account has not been verified via email or phone.");

        if (string.IsNullOrEmpty(user.HashPassword) || string.IsNullOrEmpty(user.Salt) ||
            !VerifyPassword(model.Password, user.HashPassword, user.Salt))
            throw new ErrorException(StatusCodes.Status401Unauthorized,
                ApiCodes.UNAUTHORIZED, "Incorrect password!");

        var role = user.Role;

        if (role == null)
        {
            throw new ErrorException(StatusCodes.Status404NotFound,
                ApiCodes.NOT_FOUND,
                $"No role found for this account! RoleId: {user.Role}");
        }
;
        var token = GenerateTokens(user, user.Role.ToString() ?? string.Empty);

        await HandleTokenAsync(user.Id, token.RefreshToken, TokenType.REFRESH_TOKEN);


        return new TokenResponseDto
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
        };
    }

    private string PadBase64(string base64)
    {
        return base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=').Replace('-', '+').Replace('_', '/');
    }
    public GoogleJwtPayload DecodePayload(string jwtToken)
    {
        var parts = jwtToken.Split('.');
        if (parts.Length != 3)
            throw new ArgumentException("Invalid JWT token");

        var payload = parts[1];
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(payload)));
        var result = JsonSerializer.Deserialize<GoogleJwtPayload>(json);
        return result ?? throw new ArgumentException("Invalid JWT payload");
    }
    public async Task<TokenResponseDto> SignInWithGoogleJwtAsync(string jwtToken, string? notificationToken = null)
    {
        var payload = DecodePayload(jwtToken);

        var user = await _unitOfWork.UserRepository.GetByIdAsync(payload.sub);

        var userByEmail = await _unitOfWork.UserRepository.GetUserByEmailAsync(payload.email.ToLower());
        if (userByEmail != null && userByEmail.IsVerify && userByEmail.CreatedBy != "GoogleOAuth")
        {
            throw new ErrorException(StatusCodes.Status409Conflict, ApiCodes.CONFLICT,
                "Email has been aldready registered!");
        }

        if (user == null)
        {
            // Tạo user mới
            user = new ApplicationUser
            {
                Id = payload.sub,
                UserName = payload.name,
                FullName = payload.name,
                UserEmail = payload.email,
                HashPassword = null,
                Salt = null,
                ProfilePicture = payload.picture,
                DateOfBirth = null,
                PhoneNumber = null,
                IsVerify = payload.email_verified,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "GoogleOAuth"
            };

            var defaultRole = Role.USER;
            user.Role = defaultRole;

            await _unitOfWork.UserRepository.InsertWithoutAuditAsync(user);
        }
        else
        {
            if (user.ProfilePicture != payload.picture)
            {
                user.ProfilePicture = payload.picture;
                user.UpdatedBy = "GoogleOAuth";
                user.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.UserRepository.UpdateWithoutAuditAsync(user);
            }
        }

        await _unitOfWork.SaveChangesAsync();

    

        var userRole = user.Role.ToString();

        if (userRole == null)
            throw new ErrorException(StatusCodes.Status404NotFound,
                ApiCodes.NOT_FOUND,
                $"No role found for this account! RoleId: {user.Role}");

        string? role = userRole.ToString();

        var token = GenerateTokens(user, role);

        await HandleTokenAsync(user.Id, token.RefreshToken, TokenType.REFRESH_TOKEN);

        return new TokenResponseDto
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken
        };
    }

    public async Task VerifyOtpAsync(VerifyOtpRequestDto request)
    {
        var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(request.Email);

        if (user == null)
            throw new ErrorException(StatusCodes.Status404NotFound,
                ApiCodes.NOT_FOUND, "User not found with the provided email.");

        string otpInputHash = HashHelper.HashOtp(request.Code);

        var otp = await _unitOfWork.OTPRepository.GetOTPAsync(user.Id, otpInputHash, OTPType.REGISTER);
        if (otp == null)
            throw new ErrorException(StatusCodes.Status400BadRequest,
                ApiCodes.BAD_REQUEST, "Invalid or expired OTP code.");

        user.IsVerify = true;
        await _unitOfWork.UserRepository.UpdateUserAsync(user);
        await _unitOfWork.OTPRepository.DeleteOTPAsync(otp);
        await _unitOfWork.SaveChangesAsync();
    }
    private TokenResponseDto GenerateTokens(ApplicationUser user, string role)
    {
        DateTime now = DateTime.UtcNow;
        var accessTokenExpirationMinutes = int.Parse(_configuration["JWT:AccessTokenExpirationMinutes"] ?? "15");
        var refreshTokenExpirationDays = int.Parse(_configuration["JWT:RefreshTokenExpirationDays"] ?? "7");

        var claims = new List<Claim>
        {
            new Claim("userId", user.Id.ToString()),
            new Claim("username", user.UserName ?? string.Empty),
            new Claim("name", user.FullName ?? string.Empty),
            new Claim("email", user.UserEmail ?? string.Empty),
            new Claim("role", role),
        };

        var keyString = _configuration["JWT:SecretKey"] ?? throw new InvalidOperationException("JWT:SecretKey not found in configuration");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        var accessToken = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"],
            audience: _configuration["JWT:Audience"],
            claims: claims,
            expires: now.AddMinutes(accessTokenExpirationMinutes),
            signingCredentials: creds
        );

        var accessTokenString = new JwtSecurityTokenHandler().WriteToken(accessToken);

        var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new TokenResponseDto()
        {
            AccessToken = accessTokenString,
            RefreshToken = refreshTokenString,
        };
    }
    private bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);
        var hashBytes = new Rfc2898DeriveBytes(password, saltBytes, 20000, HashAlgorithmName.SHA256).GetBytes(32);
        return Convert.ToBase64String(hashBytes) == storedHash;
    }
    private async Task HandleTokenAsync(string userId, string token, TokenType tokenType)
    {
        var existingToken = await _unitOfWork.TokenRepository.GetTokenByUserIdAsync(userId, tokenType);
        if (existingToken != null)
        {
            await _unitOfWork.TokenRepository.DeleteAsync(existingToken);
            await _unitOfWork.SaveChangesAsync();
        }

        DateTime? expiredAt = null;
        if (tokenType == TokenType.REFRESH_TOKEN)
        {
            expiredAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["JWT:RefreshTokenExpirationDays"] ?? "7"));
        }

        var tokenEntity = new ApplicationUserToken
        {
            UserId = userId,
            Token = token,
            ExpiredAt = expiredAt,
            IsRevoked = false,
            TokenType = tokenType,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.TokenRepository.CreateTokenAsync(tokenEntity);
        await _unitOfWork.SaveChangesAsync();
    }
}
