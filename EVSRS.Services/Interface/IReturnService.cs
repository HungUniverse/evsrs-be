using EVSRS.BusinessObjects.DTO.ReturnSettlementDto;
using EVSRS.BusinessObjects.DTO.HandoverInspectionDto;
using EVSRS.BusinessObjects.DTO.OrderBookingDto;

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
    
    // New methods for complete return flow
    Task<OrderBookingResponseDto> CompleteReturnProcessAsync(CompleteReturnRequestDto request);
    Task<ReturnSettlementResponseDto> ProcessReturnSettlementPaymentAsync(ReturnSettlementPaymentRequestDto request);
    Task<string> GenerateSepayQrForReturnSettlementAsync(string returnSettlementId);
    Task<ReturnSettlementPaymentStatusDto> GetReturnSettlementPaymentStatusAsync(string returnSettlementId);
}