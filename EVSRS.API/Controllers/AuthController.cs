using EVSRS.BusinessObjects.DTO.AuthDto;
using EVSRS.BusinessObjects.DTO.TokenDto;
using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOTPRequestDto model)
        {
            await _authService.SendRegisterOtpAsync(model);
            return Ok(new ResponseModel<object>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Send OTP successfully. Please check your email!"
            ));
        }
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtpAsync([FromBody] SendOTPRequestDto model)
        {
            await _authService.ResendOtpAsync(model);
            return Ok(new ResponseModel<object>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Resend OTP successfully!"
            ));
        }
        [HttpPost("complete-register")]
        public async Task<IActionResult> CompleteRegister([FromBody] RegisterUserRequestDto model)
        {
            await _authService.CompleteRegisterAsync(model);
            return Ok(new ResponseModel<object>(
                StatusCodes.Status201Created,
                ApiCodes.CREATED,
                null,
                "Register user successfully!"));
        }
        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] LoginRequestDto model)
        {
            var token = await _authService.SignInAsync(model);
            return Ok(new ResponseModel<TokenResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                token,
                "Login successfully!"
            ));
        }
        [HttpPost("login-google")]
        public async Task<IActionResult> LoginGoogle([FromBody] GoogleLoginRequestDto request)
        {
            var tokenResponse =
                await _authService.SignInWithGoogleJwtAsync(request.JwtToken);

            var response = new ResponseModel<TokenResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                tokenResponse,
                "Login successfully!"
            );

            return Ok(response);
        }


        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto model)
        {
            await _authService.LogoutAsync(model);
            return Ok(new ResponseModel<object>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Logout successfully!"
            ));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto model)
        {
            var token = await _authService.RefreshTokenAsync(model);
            return Ok(new ResponseModel<TokenResponseDto>(
                StatusCodes.Status201Created,
                ApiCodes.CREATED,
                token,
                "Refresh token successfully!"
            ));
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto model)
        {
            await _authService.VerifyOtpAsync(model);
            return Ok(new ResponseModel<object>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "OTP verification successful!"
            ));
        }

        [HttpPost("register-at-depot")]
        public async Task<IActionResult> RegisterAtDepot([FromBody] RegisterUserAtDepotRequestDto model)
        {
            var response = await _authService.RegisterUserAtDepotAsync(model);
            return Ok(new ResponseModel<RegisterUserAtDepotResponseDto>(
                StatusCodes.Status201Created,
                ApiCodes.CREATED,
                response,
                "User registered at depot successfully!"
            ));
        }
    }
}
