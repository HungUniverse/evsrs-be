using EVSRS.BusinessObjects.DTO.HandoverInspectionDto;
using EVSRS.BusinessObjects.DTO.ReturnSettlementDto;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSRS.API.Constant;

namespace EVSRS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReturnController : ControllerBase
{
    private readonly IReturnService _returnService;

    public ReturnController(IReturnService returnService)
    {
        _returnService = returnService;
    }

    /// <summary>
    /// Create return inspection when customer returns car
    /// </summary>
    [HttpPost("inspection")]
    public async Task<IActionResult> CreateReturnInspection([FromBody] HandoverInspectionRequestDto request)
    {
        var result = await _returnService.CreateReturnInspectionAsync(request);
        return Ok(new ResponseModel<HandoverInspectionResponseDto>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Return inspection created successfully"
        ));
    }

    /// <summary>
    /// Create return settlement with additional fees
    /// </summary>
    [HttpPost("settlement")]
    public async Task<IActionResult> CreateReturnSettlement([FromBody] ReturnSettlementRequestDto request)
    {
        var result = await _returnService.CreateReturnSettlementAsync(request);
        return Ok(new ResponseModel<ReturnSettlementResponseDto>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Return settlement created successfully"
        ));
    }

    /// <summary>
    /// Update return settlement
    /// </summary>
    [HttpPut("settlement/{id}")]
    public async Task<IActionResult> UpdateReturnSettlement(string id, [FromBody] ReturnSettlementRequestDto request)
    {
        var result = await _returnService.UpdateReturnSettlementAsync(id, request);
        return Ok(new ResponseModel<ReturnSettlementResponseDto>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Return settlement updated successfully"
        ));
    }

    /// <summary>
    /// Get return settlement by ID
    /// </summary>
    [HttpGet("settlement/{id}")]
    public async Task<IActionResult> GetReturnSettlementById(string id)
    {
        var result = await _returnService.GetReturnSettlementByIdAsync(id);
        return Ok(new ResponseModel<ReturnSettlementResponseDto>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Return settlement retrieved successfully"
        ));
    }

    /// <summary>
    /// Get return settlement by order booking ID
    /// </summary>
    [HttpGet("settlement/order/{orderBookingId}")]
    public async Task<IActionResult> GetReturnSettlementByOrderId(string orderBookingId)
    {
        var result = await _returnService.GetReturnSettlementByOrderIdAsync(orderBookingId);
        if (result == null)
        {
            return NotFound(new ResponseModel<ReturnSettlementResponseDto>(
                StatusCodes.Status404NotFound,
                ApiCodes.NOT_FOUND,
                null,
                "Return settlement not found"
            ));
        }
        return Ok(new ResponseModel<ReturnSettlementResponseDto>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Return settlement retrieved successfully"
        ));
    }

    /// <summary>
    /// Get return settlements by date range
    /// </summary>
    [HttpGet("settlement")]
    public async Task<IActionResult> GetReturnSettlementsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var result = await _returnService.GetReturnSettlementsByDateRangeAsync(startDate, endDate);
        return Ok(new ResponseModel<List<ReturnSettlementResponseDto>>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Return settlements retrieved successfully"
        ));
    }

    /// <summary>
    /// Get return inspection by order booking ID
    /// </summary>
    [HttpGet("inspection/order/{orderBookingId}")]
    public async Task<IActionResult> GetReturnInspectionByOrderId(string orderBookingId)
    {
        var result = await _returnService.GetReturnInspectionByOrderIdAsync(orderBookingId);
        return Ok(new ResponseModel<HandoverInspectionResponseDto>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Return inspection retrieved successfully"
        ));
    }

    /// <summary>
    /// Delete return settlement
    /// </summary>
    [HttpDelete("settlement/{id}")]
    public async Task<IActionResult> DeleteReturnSettlement(string id)
    {
        await _returnService.DeleteReturnSettlementAsync(id);
        return Ok(new ResponseModel<string>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            null,
            "Return settlement deleted successfully"
        ));
    }
}