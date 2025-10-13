using EVSRS.BusinessObjects.DTO.ContractDto;
using EVSRS.BusinessObjects.DTO.HandoverInspectionDto;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Helper;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EVSRS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class HandoverController : ControllerBase
{
    private readonly IHandoverService _handoverService;

    public HandoverController(IHandoverService handoverService)
    {
        _handoverService = handoverService;
    }

    /// <summary>
    /// Create a contract for confirmed order booking
    /// </summary>
    [HttpPost("contract")]
    public async Task<IActionResult> CreateContract([FromBody] ContractRequestDto request)
    {
        var result = await _handoverService.CreateContractAsync(request);
        return Ok(new ResponseModel<ContractResponseDto>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Contract created successfully"
        ));
    }

    /// <summary>
    /// Create handover inspection when giving car to customer
    /// </summary>
    [HttpPost("inspection")]
    public async Task<IActionResult> CreateHandoverInspection([FromBody] HandoverInspectionRequestDto request)
    {
        request.Type = "HANDOVER"; // Ensure this is a handover inspection
        var result = await _handoverService.CreateHandoverInspectionAsync(request);
        return Ok(new ResponseModel<HandoverInspectionResponseDto>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Handover inspection created successfully"
        ));
    }

    /// <summary>
    /// Update handover inspection
    /// </summary>
    [HttpPut("inspection/{id}")]
    public async Task<IActionResult> UpdateHandoverInspection(string id, [FromBody] HandoverInspectionRequestDto request)
    {
        var result = await _handoverService.UpdateHandoverInspectionAsync(id, request);
        return Ok(new ResponseModel<HandoverInspectionResponseDto>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Handover inspection updated successfully"
        ));
    }

    /// <summary>
    /// Get handover inspection by ID
    /// </summary>
    [HttpGet("inspection/{id}")]
    public async Task<IActionResult> GetHandoverInspectionById(string id)
    {
        var result = await _handoverService.GetHandoverInspectionByIdAsync(id);
        return Ok(new ResponseModel<HandoverInspectionResponseDto>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Handover inspection retrieved successfully"
        ));
    }

    /// <summary>
    /// Get all handover inspections for an order
    /// </summary>
    [HttpGet("inspection/order/{orderBookingId}")]
    public async Task<IActionResult> GetHandoverInspectionsByOrderId(string orderBookingId)
    {
        var result = await _handoverService.GetHandoverInspectionsByOrderIdAsync(orderBookingId);
        return Ok(new ResponseModel<List<HandoverInspectionResponseDto>>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Handover inspections retrieved successfully"
        ));
    }

    /// <summary>
    /// Get all handover inspections by staff ID
    /// </summary>
    [HttpGet("inspection/staff/{staffId}")]
    public async Task<IActionResult> GetHandoverInspectionsByStaffId(string staffId)
    {
        var result = await _handoverService.GetHandoverInspectionsByStaffIdAsync(staffId);
        return Ok(new ResponseModel<List<HandoverInspectionResponseDto>>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Handover inspections retrieved successfully"
        ));
    }

    /// <summary>
    /// Get contract by order booking ID
    /// </summary>
    [HttpGet("contract/order/{orderBookingId}")]
    public async Task<IActionResult> GetContractByOrderId(string orderBookingId)
    {
        var result = await _handoverService.GetContractByOrderIdAsync(orderBookingId);
        return Ok(new ResponseModel<ContractResponseDto>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Contract retrieved successfully"
        ));
    }

    /// <summary>
    /// Update contract sign status
    /// </summary>
    [HttpPatch("contract/{id}/status")]
    public async Task<IActionResult> UpdateContractStatus(string id, [FromBody] UpdateContractStatusRequest request)
    {
        var result = await _handoverService.UpdateContractStatusAsync(id, request.SignStatus);
        return Ok(new ResponseModel<ContractResponseDto>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            result,
            "Contract status updated successfully"
        ));
    }

    /// <summary>
    /// Delete handover inspection
    /// </summary>
    [HttpDelete("inspection/{id}")]
    public async Task<IActionResult> DeleteHandoverInspection(string id)
    {
        await _handoverService.DeleteHandoverInspectionAsync(id);
        return Ok(new ResponseModel<string>(
            StatusCodes.Status200OK,
            ApiCodes.SUCCESS,
            null,
            "Handover inspection deleted successfully"
        ));
    }
}

public class UpdateContractStatusRequest
{
    public SignStatus SignStatus { get; set; } = SignStatus.PENDING;
}