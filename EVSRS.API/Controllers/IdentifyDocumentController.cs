using EVSRS.BusinessObjects.DTO.IdentifyDocumentDto;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IdentifyDocumentController : ControllerBase
    {
        private readonly IIdentifyDocumentService _identifyDocumentService;
        public IdentifyDocumentController(IIdentifyDocumentService identifyDocumentService)
        {
            _identifyDocumentService = identifyDocumentService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetIdentifyDocumentByUserId(string userId)
        {
            var result = await _identifyDocumentService.GetIdentifyDocumentByUserIdAsync(userId);
            if (result == null)
            {
                return NotFound(new { Message = "Identify document not found." });
            }
            return Ok( new ResponseModel<IdentifyDocumentResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Identify document retrieved successfully."
                ));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetIdentifyDocumentById(string id)
        {
            var result = await _identifyDocumentService.GetIdentifyDocumentByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { Message = "Identify document not found." });
            }
            return Ok(new ResponseModel<IdentifyDocumentResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Identify document retrieved successfully."
                ));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllIdentifyDocuments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _identifyDocumentService.GetAllIdentifyDocumentAsync(page, pageSize);
            if (result == null) {
                return NotFound(new { Message = "No identify documents found." });
            }
            return Ok(new ResponseModel<PaginatedList<IdentifyDocumentResponseDto>>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Identify documents retrieved successfully."
                ));
        }

        [HttpPost]
        public async Task<IActionResult> CreateIdentifyDocument([FromBody] IdentifyDocumentRequestDto identifyDocumentRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Invalid data." });
            }
            var result = await _identifyDocumentService.CreateIdentifyDocumentAsync(identifyDocumentRequestDto);
            return CreatedAtAction(nameof(GetIdentifyDocumentById), new { id = result.Id }, new ResponseModel<IdentifyDocumentResponseDto>(
                StatusCodes.Status201Created,
                ApiCodes.SUCCESS,
                result,
                "Identify document created successfully."
                ));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIdentifyDocument(string id, [FromBody] IdentifyDocumentRequestDto identifyDocumentRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Invalid data." });
            }
            var result = await _identifyDocumentService.UpdateIdentifyDocumentAsync(id, identifyDocumentRequestDto);
            if (result == null)
            {
                return NotFound(new { Message = "Identify document not found." });
            }
            return Ok(new ResponseModel<IdentifyDocumentResponseDto>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                result,
                "Identify document updated successfully."
                ));
        }

        [HttpPatch("{id}/status")]
        [Authorize] // Ensure only authorized users can update status
        public async Task<IActionResult> UpdateIdentifyDocumentStatus(string id, [FromBody] UpdateIdentifyDocumentStatusDto updateStatusDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Message = "Invalid data." });
            }

            try
            {
                var result = await _identifyDocumentService.UpdateIdentifyDocumentStatusAsync(id, updateStatusDto);
                return Ok(new ResponseModel<IdentifyDocumentResponseDto>(
                    StatusCodes.Status200OK,
                    ApiCodes.SUCCESS,
                    result,
                    "Identify document status updated successfully."
                ));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { Message = "Identify document not found." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIdentifyDocument(string id)
        {
            await _identifyDocumentService.DeleteIdentifyDocumentAsync(id);
            return Ok(new ResponseModel<string>(
                StatusCodes.Status200OK,
                ApiCodes.SUCCESS,
                null,
                "Identify document deleted successfully."
                ));
        }
    }
}
