using AutoMapper;
using EVSRS.BusinessObjects.DTO.ContractDto;
using EVSRS.BusinessObjects.DTO.HandoverInspectionDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;

namespace EVSRS.Services.Service;

public class HandoverService : IHandoverService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidationService _validationService;

    public HandoverService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        IValidationService validationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _validationService = validationService;
    }

    public async Task<ContractResponseDto> CreateContractAsync(ContractRequestDto request)
    {
        await _validationService.ValidateAndThrowAsync(request);

        // Check if contract already exists for this order
        var existingContract = await _unitOfWork.ContractRepository.GetContractByOrderIdAsync(request.OrderBookingId);
        _validationService.CheckBadRequest(existingContract != null, "Contract already exists for this order");

        // Validate order booking exists and is in correct status
        var orderBooking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(request.OrderBookingId);
        _validationService.CheckNotFound(orderBooking, "Order booking not found");
        _validationService.CheckBadRequest(
            orderBooking?.Status != OrderBookingStatus.CONFIRMED,
            "Order must be in CONFIRMED status to create contract"
        );

        var contract = _mapper.Map<Contract>(request);
        contract.Id = Guid.NewGuid().ToString();
        contract.SignedDate = DateTime.UtcNow;
        contract.CreatedBy = GetCurrentUserName();
        contract.CreatedAt = DateTime.UtcNow;
        contract.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ContractRepository.InsertAsync(contract);
        await _unitOfWork.SaveChangesAsync();

        var result = await _unitOfWork.ContractRepository.GetContractByOrderIdAsync(request.OrderBookingId);
        return _mapper.Map<ContractResponseDto>(result);
    }

    public async Task<HandoverInspectionResponseDto> CreateHandoverInspectionAsync(HandoverInspectionRequestDto request)
    {
        await _validationService.ValidateAndThrowAsync(request);

        // Check if handover inspection already exists
        var existingInspection = await _unitOfWork.HandoverInspectionRepository
            .GetHandoverInspectionByOrderAndTypeAsync(request.OrderBookingId, request.Type);
        _validationService.CheckBadRequest(existingInspection != null, 
            $"Handover inspection of type {request.Type} already exists for this order");

        // Validate order booking status
        var orderBooking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(request.OrderBookingId);
        _validationService.CheckNotFound(orderBooking, "Order booking not found");

        if (request.Type == "HANDOVER")
        {
            _validationService.CheckBadRequest(
                orderBooking?.Status != OrderBookingStatus.CONFIRMED,
                "Order must be in CONFIRMED status for handover"
            );
        }
        else if (request.Type == "RETURN")
        {
            _validationService.CheckBadRequest(
                orderBooking?.Status != OrderBookingStatus.IN_USE,
                "Order must be in IN_USE status for return"
            );
        }

        var inspection = _mapper.Map<HandoverInspection>(request);
        inspection.Id = Guid.NewGuid().ToString();
        inspection.CreatedBy = GetCurrentUserName();
        inspection.CreatedAt = DateTime.UtcNow;
        inspection.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.HandoverInspectionRepository.InsertAsync(inspection);
        await _unitOfWork.SaveChangesAsync();

        var result = await _unitOfWork.HandoverInspectionRepository.GetByIdAsync(inspection.Id);
        return _mapper.Map<HandoverInspectionResponseDto>(result);
    }

    public async Task<HandoverInspectionResponseDto> UpdateHandoverInspectionAsync(string id, HandoverInspectionRequestDto request)
    {
        await _validationService.ValidateAndThrowAsync(request);

        var inspection = await _unitOfWork.HandoverInspectionRepository.GetByIdAsync(id);
        _validationService.CheckNotFound(inspection, "Handover inspection not found");

        _mapper.Map(request, inspection);
        inspection!.UpdatedBy = GetCurrentUserName();
        inspection.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.HandoverInspectionRepository.UpdateAsync(inspection);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<HandoverInspectionResponseDto>(inspection);
    }

    public async Task<HandoverInspectionResponseDto> GetHandoverInspectionByIdAsync(string id)
    {
        var inspection = await _unitOfWork.HandoverInspectionRepository.GetByIdAsync(id);
        _validationService.CheckNotFound(inspection, "Handover inspection not found");
        return _mapper.Map<HandoverInspectionResponseDto>(inspection);
    }

    public async Task<List<HandoverInspectionResponseDto>> GetHandoverInspectionsByOrderIdAsync(string orderBookingId)
    {
        var inspections = await _unitOfWork.HandoverInspectionRepository.GetHandoverInspectionsByOrderIdAsync(orderBookingId);
        return inspections.Select(i => _mapper.Map<HandoverInspectionResponseDto>(i)).ToList();
    }

    public async Task<List<HandoverInspectionResponseDto>> GetHandoverInspectionsByStaffIdAsync(string staffId)
    {
        var inspections = await _unitOfWork.HandoverInspectionRepository.GetHandoverInspectionsByStaffIdAsync(staffId);
        return inspections.Select(i => _mapper.Map<HandoverInspectionResponseDto>(i)).ToList();
    }

    public async Task<ContractResponseDto> GetContractByOrderIdAsync(string orderBookingId)
    {
        var contract = await _unitOfWork.ContractRepository.GetContractByOrderIdAsync(orderBookingId);
        _validationService.CheckNotFound(contract, "Contract not found");
        return _mapper.Map<ContractResponseDto>(contract);
    }

    public async Task<ContractResponseDto> UpdateContractStatusAsync(string id, string signStatus)
    {
        var contract = await _unitOfWork.ContractRepository.GetByIdAsync(id);
        _validationService.CheckNotFound(contract, "Contract not found");

        contract!.SignStatus = signStatus;
        contract.UpdatedBy = GetCurrentUserName();
        contract.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ContractRepository.UpdateAsync(contract);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ContractResponseDto>(contract);
    }

    public async Task DeleteHandoverInspectionAsync(string id)
    {
        var inspection = await _unitOfWork.HandoverInspectionRepository.GetByIdAsync(id);
        _validationService.CheckNotFound(inspection, "Handover inspection not found");

        await _unitOfWork.HandoverInspectionRepository.DeleteAsync(inspection!);
        await _unitOfWork.SaveChangesAsync();
    }

    private string GetCurrentUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
    }
}