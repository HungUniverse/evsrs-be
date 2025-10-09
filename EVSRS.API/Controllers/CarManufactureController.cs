using EVSRS.BusinessObjects.DTO.CarManufactureDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarManufactureController : ControllerBase
    {
        private readonly ICarManufactureService _carManufactureService;
        public CarManufactureController(ICarManufactureService carManufactureService)
        {
            _carManufactureService = carManufactureService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCarManufactures(int pageNumber = 1, int pageSize = 10)
        {
            var carManufactures = await _carManufactureService.GetAllCarManufacturesAsync(pageNumber, pageSize);
            return Ok(new ResponseModel<PaginatedList<CarManufactureResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                carManufactures,
                "Get car manufacture successfully!"
            ));
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetCarManufactureById(string id)
        {
            var carManufacture = await _carManufactureService.GetCarManufactureByIdAsync(id);
            if (carManufacture == null)
            {
                return NotFound();
            }
            return Ok(new ResponseModel<CarManufactureResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                carManufacture,
                "Get car manufacture successfully!"
            ));
        }

        [HttpGet("by-name/{name}")]
        public async Task<IActionResult> GetCarManufactureByName(string name)
        {
            var carManufacture = await _carManufactureService.GetCarManufactureByNameAsync(name);
            if (carManufacture == null)
            {
                return NotFound();
            }
            return Ok(new ResponseModel<CarManufactureResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                carManufacture,
                "Get car manufacture successfully!"
            ));
        }


        [HttpPost]
        public async Task<IActionResult> CreateCarManufacture([FromBody] CarManufactureRequestDto carManufacture)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _carManufactureService.CreateCarManufactureAsync(carManufacture);
            return StatusCode(StatusCodes.Status201Created, new ResponseModel<string>(StatusCodes.Status201Created, ApiCodes.CREATED, null, "Created CarManufacture successfully"));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCarManufacture(string id, [FromBody] CarManufactureRequestDto carManufacture)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var updatedCarManufacture = await _carManufactureService.UpdateCarManufactureAsync(id, carManufacture);
            if (updatedCarManufacture == null)
            {
                return NotFound();
            }
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Update car manufacture successfully!"
            ));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCarManufacture(string id)
        {
            var carManufacture = await _carManufactureService.GetCarManufactureByIdAsync(id);
            if (carManufacture == null)
            {
                return NotFound();
            }
            await _carManufactureService.DeleteCarManufactureAsync(id);
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Deleted car manufacture successfully!"
            ));
        }

    }
}
