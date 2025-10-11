using EVSRS.BusinessObjects.DTO.ReturnSettlementDto;
using EVSRS.BusinessObjects.DTO.HandoverInspectionDto;

namespace EVSRS.Services.Interface;

public interface IReturnService
{
    Task<HandoverInspectionResponseDto> CreateReturnInspectionAsync(HandoverInspectionRequestDto request);
    Task<ReturnSettlementResponseDto> CreateReturnSettlementAsync(ReturnSettlementRequestDto request);
    Task<ReturnSettlementResponseDto> UpdateReturnSettlementAsync(string id, ReturnSettlementRequestDto request);
    Task<ReturnSettlementResponseDto> GetReturnSettlementByIdAsync(string id);
    Task<ReturnSettlementResponseDto?> GetReturnSettlementByOrderIdAsync(string orderBookingId);
    Task<List<ReturnSettlementResponseDto>> GetReturnSettlementsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<HandoverInspectionResponseDto> GetReturnInspectionByOrderIdAsync(string orderBookingId);
    Task DeleteReturnSettlementAsync(string id);
}