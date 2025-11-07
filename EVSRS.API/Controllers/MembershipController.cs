using EVSRS.BusinessObjects.DTO.MembershipDto;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MembershipController : ControllerBase
    {
        private readonly IMembershipService _membershipService;
        private readonly IMembershipConfigService _membershipConfigService;

        public MembershipController(
            IMembershipService membershipService,
            IMembershipConfigService membershipConfigService)
        {
            _membershipService = membershipService;
            _membershipConfigService = membershipConfigService;
        }

        /// <summary>
        /// Get current user's membership information
        /// </summary>
        [HttpGet("my-membership")]
        [Authorize]
        public async Task<IActionResult> GetMyMembership()
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new ResponseModel<object>(
                        StatusCodes.Status401Unauthorized,
                        ApiCodes.UNAUTHENTICATED,
                        null,
                        "User ID not found in token"));
                }

                var membership = await _membershipService.GetMembershipByUserIdAsync(userId);
                if (membership == null)
                {
                    // Tạo membership mới cho user
                    await _membershipService.CreateInitialMembershipForUserAsync(userId);
                    membership = await _membershipService.GetMembershipByUserIdAsync(userId);
                }

                return Ok(new ResponseModel<MembershipResponseDto>(
                    StatusCodes.Status200OK,
                    ApiCodes.SUCCESS,
                    membership,
                    "Retrieved membership successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseModel<object>(
                    StatusCodes.Status400BadRequest,
                    ApiCodes.BAD_REQUEST,
                    null,
                    ex.Message));
            }
        }

        /// <summary>
        /// Get membership info by user ID (Admin/Staff only)
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> GetMembershipByUserId(string userId)
        {
            try
            {
                var membership = await _membershipService.GetMembershipByUserIdAsync(userId);
                if (membership == null)
                {
                    return NotFound(new ResponseModel<object>(
                        StatusCodes.Status404NotFound,
                        ApiCodes.NOT_FOUND,
                        null,
                        "Membership not found for this user"));
                }

                return Ok(new ResponseModel<MembershipResponseDto>(
                    StatusCodes.Status200OK,
                    ApiCodes.SUCCESS,
                    membership,
                    "Retrieved membership successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseModel<object>(
                    StatusCodes.Status400BadRequest,
                    ApiCodes.BAD_REQUEST,
                    null,
                    ex.Message));
            }
        }

        /// <summary>
        /// Get all membership level configs (Public - for display on website)
        /// </summary>
        [HttpGet("levels")]
        public async Task<IActionResult> GetAllLevels()
        {
            try
            {
                var configs = await _membershipConfigService.GetAllMembershipConfigsAsync();
                return Ok(new ResponseModel<List<MembershipConfigResponseDto>>(
                    StatusCodes.Status200OK,
                    ApiCodes.SUCCESS,
                    configs,
                    "Retrieved all membership levels successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseModel<object>(
                    StatusCodes.Status400BadRequest,
                    ApiCodes.BAD_REQUEST,
                    null,
                    ex.Message));
            }
        }

        /// <summary>
        /// Get specific membership level config (Public)
        /// </summary>
        [HttpGet("levels/{id}")]
        public async Task<IActionResult> GetLevelById(string id)
        {
            try
            {
                var config = await _membershipConfigService.GetMembershipConfigByIdAsync(id);
                if (config == null)
                {
                    return NotFound(new ResponseModel<object>(
                        StatusCodes.Status404NotFound,
                        ApiCodes.NOT_FOUND,
                        null,
                        "Membership level config not found"));
                }

                return Ok(new ResponseModel<MembershipConfigResponseDto>(
                    StatusCodes.Status200OK,
                    ApiCodes.SUCCESS,
                    config,
                    "Retrieved membership level config successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseModel<object>(
                    StatusCodes.Status400BadRequest,
                    ApiCodes.BAD_REQUEST,
                    null,
                    ex.Message));
            }
        }

        /// <summary>
        /// Update membership level config (Admin only)
        /// </summary>
        [HttpPut("levels/{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateLevelConfig(string id, [FromBody] UpdateMembershipConfigDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ResponseModel<object>(
                        StatusCodes.Status400BadRequest,
                        ApiCodes.BAD_REQUEST,
                        ModelState,
                        "Invalid request data"));
                }

                var updatedConfig = await _membershipConfigService.UpdateMembershipConfigAsync(id, dto);
                return Ok(new ResponseModel<MembershipConfigResponseDto>(
                    StatusCodes.Status200OK,
                    ApiCodes.SUCCESS,
                    updatedConfig,
                    "Updated membership level config successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ResponseModel<object>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ResponseModel<object>(
                    StatusCodes.Status400BadRequest,
                    ApiCodes.BAD_REQUEST,
                    null,
                    ex.Message));
            }
        }
    }
}
