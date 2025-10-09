using EVSRS.BusinessObjects.DTO.AmenitiesDto;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmenitiesController : ControllerBase
    {
        private readonly IAmenitiesService _amenitiesService;
        public AmenitiesController(IAmenitiesService amenitiesService)
        {
            _amenitiesService = amenitiesService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAmenities([FromBody] AmenitiesRequestDto model)
        {
            await _amenitiesService.CreateAmenities(model);
            return Ok(new ResponseModel<object>(
                StatusCodes.Status201Created,
                ApiCodes.CREATED,
                null,
                "Create amenities successfully!"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAmenities(string id)
        {
            await _amenitiesService.DeleteAmenities(id);
            return Ok(new ResponseModel<object>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Delete amenities successfully!"));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAmenities()
        {
            var amenities = await _amenitiesService.GetAllAmenities();
            return Ok(new ResponseModel<PaginatedList<AmenitiesResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                amenities));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAmenitiesById(string id)
        {
            var amenities = await _amenitiesService.GetAmenitiesById(id);
            if (amenities == null)
            {
                return NotFound(new ResponseModel<object>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    $"Amenities with ID {id} not found."));
            }
            return Ok(new ResponseModel<AmenitiesResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                amenities));
        }

        [HttpGet("name")]
        public async Task<IActionResult> GetAmenitiesByName([FromQuery] string name)
        {
            var amenities = await _amenitiesService.GetAmenitiesByName(name);
            if (amenities == null)
            {
                return NotFound(new ResponseModel<object>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    $"Amenities with name {name} not found."));
            }
            return Ok(new ResponseModel<AmenitiesResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                amenities));
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAmenities(string id, [FromBody] AmenitiesRequestDto model)
        {
            await _amenitiesService.UpdateAmenities(id, model);
            return Ok(new ResponseModel<object>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Update amenities successfully!"));
        }

       

    }
}
