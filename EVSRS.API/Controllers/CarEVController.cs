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
            return Ok(carEV);
        }
        [HttpPost]
        public async Task<IActionResult> CreateCarEV([FromBody] CarEVRequestDto carEV)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _carEVService.CreateCarEVAsync(carEV);
            return StatusCode(StatusCodes.Status201Created, new ResponseModel<string>(StatusCodes.Status201Created, ApiCodes.CREATED, null, "Created CarEV successfully"));

        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCarEV(string id, [FromBody] CarEVRequestDto carEV)
        {
            var updatedCarEV = await _carEVService.UpdateCarEVAsync(id, carEV);
            if (updatedCarEV == null)
            {
                return NotFound();
            }
            return Ok(updatedCarEV);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCarEV(string id)
        {
            await _carEVService.DeleteCarEVAsync(id);
            return NoContent();
        }
    }
}
