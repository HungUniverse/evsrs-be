using EVSRS.BusinessObjects.DTO.DepotDto;
using EVSRS.BusinessObjects.DTO.ModelDto;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepotController : ControllerBase
    {
        private readonly IDepotService _depotService;
        public DepotController(IDepotService depotService)
        {
            _depotService = depotService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllDepots([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var depots = await _depotService.GetAllDepotAsync(page, pageSize);
            return Ok(new ResponseModel<PaginatedList<DepotResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                depots,
                "Get depots successfully!"
            ));
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDepotById(string id)
        {
            var depot = await _depotService.GetDepotByIdAsync(id);
            if (depot == null)
            {
                return NotFound();
            }
            return Ok(new ResponseModel<DepotResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                depot,
                "Get depot successfully!"
            ));
        }

        [HttpGet("by-name/{name}")]
        public async Task<IActionResult> GetDepotByName(string name)
        {
            var depot = await _depotService.GetDepotByNameAsync(name);
            if (depot == null)
            {
                return NotFound();
            }
            return Ok(new ResponseModel<DepotResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                depot,
                "Get depot successfully!"
            ));
        }

        [HttpGet("by-map/{mapId}")]
        public async Task<IActionResult> GetDepotByMapId(string mapId)
        {
            var depot = await _depotService.GetDepotByMapId(mapId);
            if (depot == null)
            {
                return NotFound();
            }
            return Ok(new ResponseModel<DepotResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                depot,
                "Get depot successfully!"
            ));
        }

        [HttpGet("search")]
        public async Task<IActionResult> GetDepotsByLocation([FromQuery] string? province, [FromQuery] string? district, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var depots = await _depotService.GetDepotsByLocationAsync(province, district, page, pageSize);
            return Ok(new ResponseModel<PaginatedList<DepotResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                depots,
                "Search depots by location successfully!"
            ));
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepot([FromBody] DepotRequestDto depotCreateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _depotService.CreateDepotAsync(depotCreateDto);
            return StatusCode(StatusCodes.Status201Created, new ResponseModel<DepotResponseDto>(
                StatusCodes.Status201Created, 
                ApiCodes.CREATED, 
                result, 
                "Created depot successfully"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepot(string id)
        {
            await _depotService.DeleteDepotAsync(id);
            return Ok(new ResponseModel<string>(
               StatusCodes.Status200OK,
               ApiCodes.SUCCESS,
               null,
               "Delete depot successfully!"
            ));

        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDepot(string id, [FromBody] DepotRequestDto depotUpdateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var updatedDepot = await _depotService.UpdateDepotAsync(id, depotUpdateDto);
            if (updatedDepot == null)
            {
                return NotFound();
            }
            return Ok(new ResponseModel<DepotResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                updatedDepot,
                "Update depot successfully!"
            ));
        }
    }
}
