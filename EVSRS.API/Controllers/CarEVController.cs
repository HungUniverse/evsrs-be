using EVSRS.BusinessObjects.DTO.CarEVDto;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarEVController : ControllerBase
    {
        private readonly ICarEVService _carEVService;
        public CarEVController(ICarEVService carEVService)
        {
            _carEVService = carEVService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllCarEVs(int pageNumber = 1, int pageSize = 10)
        {
            var carEVs = await _carEVService.GetAllCarEVsAsync(pageNumber, pageSize);
            return Ok(carEVs);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCarEVById(string id)
        {
            var carEV = await _carEVService.GetCarEVByIdAsync(id);
            if (carEV == null)
            {
                return NotFound();
            }
            return StatusCode(StatusCodes.Status200OK, new ResponseModel<CarEVResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                carEV,
                "Get CarEV by ID successfully!"
            ));
        }

        

        [HttpPost]
        public async Task<IActionResult> CreateCarEV([FromBody] CarEVRequestDto carEV)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
          
            await _carEVService.CreateCarEVAsync(carEV);
            return StatusCode(StatusCodes.Status201Created, new ResponseModel<object>(StatusCodes.Status201Created, ApiCodes.CREATED, null, "Created CarEV successfully"));

        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCarEV(string id, [FromBody] CarEVRequestDto carEV)
        {
            var existingCarEV = await _carEVService.GetCarEVByIdAsync(id);
            if (existingCarEV == null)
            {
                return NotFound(new ResponseModel<string>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    $"CarEV with ID {id} not found."
                ));
            }
            await _carEVService.UpdateCarEVAsync(id, carEV);
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "CarEV updated successfully."
            ));
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCarEV(string id)
        {
            var existingCarEV = await _carEVService.GetCarEVByIdAsync(id);
            if (existingCarEV == null)
            {
                return NotFound(new ResponseModel<string>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    $"CarEV with ID {id} not found."
                ));
            }
            await _carEVService.DeleteCarEVAsync(id);
            return Ok(new ResponseModel<object>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                existingCarEV,
                "CarEV deleted successfully."
            ));
        }

        [HttpGet("depot/{depotId}")]
        public async Task<IActionResult> GetCarEVsByDepotId(string depotId)
        {
            var result = await _carEVService.GetAllCarEVsByDepotIdAsync(depotId);
            return Ok(new ResponseModel<List<CarEVResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Get CarEVs by depot successfully!"
            ));
        }

        [HttpGet("depot/{depotId}/paginated")]
        public async Task<IActionResult> GetCarEVsByDepotIdPaginated(string depotId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _carEVService.GetCarEVsByDepotIdAsync(depotId, pageNumber, pageSize);
            return Ok(new ResponseModel<PaginatedList<CarEVResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Get CarEVs by depot with pagination successfully!"
            ));
        }

      
    }
}
