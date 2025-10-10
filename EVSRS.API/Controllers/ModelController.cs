using EVSRS.BusinessObjects.DTO.ModelDto;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModelController : ControllerBase
    {
        private readonly IModelService _modelService;
        public ModelController(IModelService modelService)
        {
            _modelService = modelService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllModels(int pageNumber = 1, int pageSize = 10)
        {
            var models = await _modelService.GetAllModelsAsync(pageNumber, pageSize);
            return Ok(new ResponseModel<PaginatedList<ModelResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                models,
                "Get models successfully!"
            ));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetModelById(string id)
        {
            var model = await _modelService.GetModelByIdAsync(id);
            if (model == null)
            {
                return NotFound();
            }
            return Ok(new ResponseModel<ModelResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                model,
                "Get model successfully!"
            ));
        }

        [HttpGet("by-name/{name}")]
        public async Task<IActionResult> GetModelByName(string name)
        {
            var model = await _modelService.GetModelByNameAsync(name);
            if (model == null)
            {
                return NotFound();
            }
            return Ok(new ResponseModel<ModelResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                model,
                "Get model successfully!"
            ));
        }

        [HttpPost]
        public async Task<IActionResult> CreateModel([FromBody] ModelRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _modelService.CreateModelAsync(model);
            return StatusCode(StatusCodes.Status201Created, new ResponseModel<string>(
                StatusCodes.Status201Created,
                ApiCodes.CREATED,
                null,
                "Create model successfully!"
            ));
        }

        [HttpPut("{id:Guid}")]
        public async Task<IActionResult> UpdateModel(string id, [FromBody] ModelRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            await _modelService.UpdateModelAsync(id, model);
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Update model successfully!"
            ));

        }

        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> DeleteModel(string id)
        {
            await _modelService.DeleteModelAsync(id);
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Delete model successfully!"
            ));
        }


    }
}
