using EVSRS.BusinessObjects.DTO.HandoverInspectionDto;
using EVSRS.BusinessObjects.DTO.ContractDto;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.Services.Interface;

public interface IHandoverService
{
    Task<ContractResponseDto> CreateContractAsync(ContractRequestDto request);
    Task<HandoverInspectionResponseDto> CreateHandoverInspectionAsync(HandoverInspectionRequestDto request);
    Task<HandoverInspectionResponseDto> UpdateHandoverInspectionAsync(string id, HandoverInspectionRequestDto request);
    Task<HandoverInspectionResponseDto> GetHandoverInspectionByIdAsync(string id);
    Task<List<HandoverInspectionResponseDto>> GetHandoverInspectionsByOrderIdAsync(string orderBookingId);
    Task<List<HandoverInspectionResponseDto>> GetHandoverInspectionsByStaffIdAsync(string staffId);
    Task<ContractResponseDto> GetContractByOrderIdAsync(string orderBookingId);
    Task<ContractResponseDto> UpdateContractStatusAsync(string id, SignStatus signStatus);
    Task DeleteHandoverInspectionAsync(string id);
}