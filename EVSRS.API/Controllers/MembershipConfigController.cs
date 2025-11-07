using EVSRS.BusinessObjects.DTO.MembershipDto;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MembershipConfigController : ControllerBase
    {
        private readonly IMembershipConfigService _membershipConfigService;

        public MembershipConfigController(IMembershipConfigService membershipConfigService)
        {
            _membershipConfigService = membershipConfigService;
        }

        /// <summary>
        /// Get all membership configs (ADMIN only)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllMembershipConfigs()
        {
            var configs = await _membershipConfigService.GetAllMembershipConfigsAsync();
            return Ok(new ResponseModel<List<MembershipConfigResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                configs,
                "Retrieved all membership configs successfully!"));
        }

        /// <summary>
        /// Get membership config by ID (ADMIN only)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMembershipConfigById(string id)
        {
            var config = await _membershipConfigService.GetMembershipConfigByIdAsync(id);
            if (config == null)
            {
                return NotFound(new ResponseModel<object>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    $"Membership config with ID {id} not found."));
            }

            return Ok(new ResponseModel<MembershipConfigResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                config,
                "Retrieved membership config successfully!"));
        }

        /// <summary>
        /// Get membership config by level (ADMIN only)
        /// </summary>
        [HttpGet("level/{level}")]
        public async Task<IActionResult> GetMembershipConfigByLevel(MembershipLevel level)
        {
            var config = await _membershipConfigService.GetMembershipConfigByLevelAsync(level);
            if (config == null)
            {
                return NotFound(new ResponseModel<object>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    $"Membership config for level '{level}' not found."));
            }

            return Ok(new ResponseModel<MembershipConfigResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                config,
                "Retrieved membership config successfully!"));
        }

        /// <summary>
        /// Create new membership config (ADMIN only)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateMembershipConfig([FromBody] CreateMembershipConfigDto dto)
        {
            try
            {
                var result = await _membershipConfigService.CreateMembershipConfigAsync(dto);
                return StatusCode(StatusCodes.Status201Created, new ResponseModel<MembershipConfigResponseDto>(
                    StatusCodes.Status201Created,
                    ApiCodes.CREATED,
                    result,
                    "Created membership config successfully!"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ResponseModel<object>(
                    StatusCodes.Status400BadRequest,
                    ApiCodes.BAD_REQUEST,
                    null,
                    ex.Message));
            }
        }

        /// <summary>
        /// Update membership config - Admin can update DiscountPercent and RequiredAmount (ADMIN only)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMembershipConfig(string id, [FromBody] UpdateMembershipConfigDto dto)
        {
            try
            {
                var result = await _membershipConfigService.UpdateMembershipConfigAsync(id, dto);
                return Ok(new ResponseModel<MembershipConfigResponseDto>(
                    StatusCodes.Status200OK,
                    ApiCodes.SUCCESS,
                    result,
                    "Updated membership config successfully!"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ResponseModel<object>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    ex.Message));
            }
        }

        /// <summary>
        /// Delete membership config (ADMIN only) - Cannot delete None level
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMembershipConfig(string id)
        {
            try
            {
                await _membershipConfigService.DeleteMembershipConfigAsync(id);
                return Ok(new ResponseModel<object>(
                    StatusCodes.Status200OK,
                    ApiCodes.SUCCESS,
                    null,
                    "Deleted membership config successfully!"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ResponseModel<object>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    ex.Message));
            }
            catch (InvalidOperationException ex)
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
