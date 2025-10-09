using EVSRS.BusinessObjects.DTO.AmenitiesDto;
using EVSRS.BusinessObjects.DTO.FeedbackDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Interface;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;
        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateFeedback([FromBody] FeedbackRequestDto model)
        {
            await _feedbackService.CreateFeedbackAsync(model);
            return Ok(new ResponseModel<object>(
                StatusCodes.Status201Created,
                ApiCodes.CREATED,
                null,
                "Create feedback successfully!"));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFeedbacks(int pageNumber = 1, int pageSize = 10)
        {
            var feedbacks = await _feedbackService.GetAllFeedbacksAsync(pageNumber, pageSize);
            return Ok(new ResponseModel<PaginatedList<FeedbackResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                feedbacks));
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFeedbackById(string id)
        {
            var feedback = await _feedbackService.GetFeedbackByIdAsync(id);
            if (feedback == null)
            {
                return NotFound(new ResponseModel<object>(
                    StatusCodes.Status404NotFound,
                    ApiCodes.NOT_FOUND,
                    null,
                    $"Feedback with ID {id} not found."));
            }
            return Ok(new ResponseModel<FeedbackResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                feedback));
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeedback(string id)
        {
            await _feedbackService.DeleteFeedbackAsync(id);
            return Ok(new ResponseModel<object>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Delete feedback successfully!"));
        }


    }
}
