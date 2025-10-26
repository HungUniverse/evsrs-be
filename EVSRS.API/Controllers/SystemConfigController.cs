using EVSRS.BusinessObjects.DTO.SystemConfigDto;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemConfigController : ControllerBase
    {
        private readonly ISystemConfigService _systemConfigService;

        public SystemConfigController(ISystemConfigService systemConfigService)
        {
            _systemConfigService = systemConfigService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSystemConfig([FromBody] SystemConfigRequestDto model)
        {
            try
            {
                await _systemConfigService.CreateSystemConfig(model);
                return StatusCode(StatusCodes.Status201Created, new ResponseModel<object>(
                    StatusCodes.Status201Created,
                    ApiCodes.CREATED,
                    null,
                    "Created system config successfully!"));
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSystemConfig(string id)
        {
            try
            {
                var existingSystemConfig = await _systemConfigService.GetSystemConfigById(id);
                if (existingSystemConfig == null)
                {
                    return NotFound(new ResponseModel<object>(
                        StatusCodes.Status404NotFound,
                        ApiCodes.NOT_FOUND,
                        null,
                        $"System config with ID {id} not found."));
                }
                await _systemConfigService.DeleteSystemConfig(id);
                return Ok(new ResponseModel<object>(
                    StatusCodes.Status200OK,
                    ApiCodes.SUCCESS,
                    null,
                    "Deleted system config successfully!"));
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

        [HttpGet]
        public async Task<IActionResult> GetAllSystemConfigs()
        {
            var systemConfigs = await _systemConfigService.GetAllSystemConfigs();
            return Ok(new ResponseModel<PaginatedList<SystemConfigResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                systemConfigs));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSystemConfigById(string id)
        {
            var systemConfig = await _systemConfigService.GetSystemConfigById(id);
            if (systemConfig == null)
            {
                return NotFound(new ResponseModel<object>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    $"System config with ID {id} not found."));
            }
            return Ok(new ResponseModel<SystemConfigResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                systemConfig));
        }

        [HttpGet("key/{key}")]
        public async Task<IActionResult> GetSystemConfigByKey(string key)
        {
            var systemConfig = await _systemConfigService.GetSystemConfigByKey(key);
            if (systemConfig == null)
            {
                return NotFound(new ResponseModel<object>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    $"System config with key '{key}' not found."));
            }
            return Ok(new ResponseModel<SystemConfigResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                systemConfig));
        }

        [HttpGet("type/{configType}")]
        public async Task<IActionResult> GetSystemConfigsByType(ConfigType configType)
        {
            var systemConfigs = await _systemConfigService.GetSystemConfigsByType(configType);
            return Ok(new ResponseModel<List<SystemConfigResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                systemConfigs));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSystemConfig(string id, [FromBody] SystemConfigRequestDto model)
        {
            try
            {
                var existingSystemConfig = await _systemConfigService.GetSystemConfigById(id);
                if (existingSystemConfig == null)
                {
                    return NotFound(new ResponseModel<object>(
                        StatusCodes.Status404NotFound,
                        ApiCodes.NOT_FOUND,
                        null,
                        $"System config with ID {id} not found."));
                }
                await _systemConfigService.UpdateSystemConfig(id, model);
                return Ok(new ResponseModel<object>(
                    StatusCodes.Status200OK,
                    ApiCodes.SUCCESS,
                    null,
                    "Updated system config successfully!"));
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