using EVSRS.BusinessObjects.DTO.TransactionDto;
using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Helper;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var users = await _userService.GetAllUserAsync(page, pageSize);
            return Ok(new ResponseModel<PaginatedList<UserResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                users,
                "Get transactions successfully!"
            ));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            return Ok(new ResponseModel<UserResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                user,
                "Get user successfully!"
            ));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var existingUser = await _userService.GetUserByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound(new ResponseModel<string>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    $"User with ID {id} not found."
                ));
            }
            await _userService.DeleteUserAsync(id);
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "User deleted successfully."
            ));

        }

        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var user = await _userService.GetUserByEmailAsync(email);
            return Ok(new ResponseModel<UserResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                user,
                "Get user successfully!"
            ));
        }
        [HttpGet("username/{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var user = await _userService.GetUserByUsernameAsync(username);
            return Ok(new ResponseModel<UserResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                user,
                "Get user successfully!"
            ));
        }

        [HttpGet("phone/{phone}")]
        public async Task<IActionResult> GetUserByPhone(string phone)
        {
            var user = await _userService.GetUserByPhoneAsync(phone);
            return Ok(new ResponseModel<UserResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                user,
                "Get user successfully!"
            ));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UserRequestDto updateUserRequestDto)
        {
            
            await _userService.UpdateUserAsync(id, updateUserRequestDto);
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "User updated successfully."
            ));
        }

        [HttpPost("staff")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequestDto createStaffRequestDto)
        {
            var staff = await _userService.CreateStaffAsync(createStaffRequestDto);
            return Ok(new ResponseModel<UserResponseDto>(
                StatusCodes.Status201Created,
                ApiCodes.SUCCESS,
                staff,
                "Staff created successfully!"
            ));
        }

        [HttpGet("depot/{depotId}/staff")]
        public async Task<IActionResult> GetStaffByDepotId(string depotId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var staffList = await _userService.GetStaffByDepotIdAsync(depotId, page, pageSize);
            return Ok(new ResponseModel<PaginatedList<UserResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                staffList,
                "Get staff by depot successfully!"
            ));
        }
    }
}
