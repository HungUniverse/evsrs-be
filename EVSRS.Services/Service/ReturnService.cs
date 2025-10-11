using AutoMapper;
using EVSRS.BusinessObjects.DTO.HandoverInspectionDto;
using EVSRS.BusinessObjects.DTO.ReturnSettlementDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;

namespace EVSRS.Services.Service;

public class ReturnService : IReturnService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IValidationService _validationService;

    public ReturnService(
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

    public async Task<HandoverInspectionResponseDto> CreateReturnInspectionAsync(HandoverInspectionRequestDto request)
    {
        await _validationService.ValidateAndThrowAsync(request);

        // Ensure this is a return inspection
        request.Type = "RETURN";

        // Check if return inspection already exists
        var existingInspection = await _unitOfWork.HandoverInspectionRepository
            .GetHandoverInspectionByOrderAndTypeAsync(request.OrderBookingId, "RETURN");
        _validationService.CheckBadRequest(existingInspection != null, "Return inspection already exists for this order");

        // Validate order booking status
        var orderBooking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(request.OrderBookingId);
        _validationService.CheckNotFound(orderBooking, "Order booking not found");
        _validationService.CheckBadRequest(
            orderBooking?.Status != OrderBookingStatus.IN_USE,
            "Order must be in IN_USE status for return inspection"
        );

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

    public async Task<ReturnSettlementResponseDto> CreateReturnSettlementAsync(ReturnSettlementRequestDto request)
    {
        await _validationService.ValidateAndThrowAsync(request);

        // Check if settlement already exists
        var existingSettlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementByOrderIdAsync(request.OrderBookingId);
        _validationService.CheckBadRequest(existingSettlement != null, "Return settlement already exists for this order");

        // Validate order booking status
        var orderBooking = await _unitOfWork.OrderRepository.GetOrderBookingByIdAsync(request.OrderBookingId);
        _validationService.CheckNotFound(orderBooking, "Order booking not found");
        _validationService.CheckBadRequest(
            orderBooking?.Status != OrderBookingStatus.RETURNED,
            "Order must be in RETURNED status to create settlement"
        );

        var settlement = _mapper.Map<ReturnSettlement>(request);
        settlement.Id = Guid.NewGuid().ToString();
        settlement.CalculateAt = DateTime.UtcNow;
        settlement.CreatedBy = GetCurrentUserName();
        settlement.CreatedAt = DateTime.UtcNow;
        settlement.UpdatedAt = DateTime.UtcNow;

        // Create settlement items
        foreach (var itemRequest in request.SettlementItems)
        {
            var item = _mapper.Map<SettlementItem>(itemRequest);
            item.Id = Guid.NewGuid().ToString();
            item.ReturnSettlementId = settlement.Id;
            item.CreatedBy = GetCurrentUserName();
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            settlement.SettlementItems.Add(item);
        }

        await _unitOfWork.ReturnSettlementRepository.InsertAsync(settlement);
        await _unitOfWork.SaveChangesAsync();

        var result = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementWithItemsAsync(settlement.Id);
        return _mapper.Map<ReturnSettlementResponseDto>(result);
    }

    public async Task<ReturnSettlementResponseDto> UpdateReturnSettlementAsync(string id, ReturnSettlementRequestDto request)
    {
        await _validationService.ValidateAndThrowAsync(request);

        var settlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementWithItemsAsync(id);
        _validationService.CheckNotFound(settlement, "Return settlement not found");

        _mapper.Map(request, settlement);
        settlement!.UpdatedBy = GetCurrentUserName();
        settlement.UpdatedAt = DateTime.UtcNow;

        // Update settlement items
        settlement.SettlementItems.Clear();
        foreach (var itemRequest in request.SettlementItems)
        {
            var item = _mapper.Map<SettlementItem>(itemRequest);
            item.Id = Guid.NewGuid().ToString();
            item.ReturnSettlementId = settlement.Id;
            item.CreatedBy = GetCurrentUserName();
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            settlement.SettlementItems.Add(item);
        }

        await _unitOfWork.ReturnSettlementRepository.UpdateAsync(settlement);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ReturnSettlementResponseDto>(settlement);
    }

    public async Task<ReturnSettlementResponseDto> GetReturnSettlementByIdAsync(string id)
    {
        var settlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementWithItemsAsync(id);
        _validationService.CheckNotFound(settlement, "Return settlement not found");
        return _mapper.Map<ReturnSettlementResponseDto>(settlement);
    }

    public async Task<ReturnSettlementResponseDto?> GetReturnSettlementByOrderIdAsync(string orderBookingId)
    {
        var settlement = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementByOrderIdAsync(orderBookingId);
        return settlement != null ? _mapper.Map<ReturnSettlementResponseDto>(settlement) : null;
    }

    public async Task<List<ReturnSettlementResponseDto>> GetReturnSettlementsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var settlements = await _unitOfWork.ReturnSettlementRepository.GetReturnSettlementsByDateRangeAsync(startDate, endDate);
        return settlements.Select(s => _mapper.Map<ReturnSettlementResponseDto>(s)).ToList();
    }

    public async Task<HandoverInspectionResponseDto> GetReturnInspectionByOrderIdAsync(string orderBookingId)
    {
        var inspection = await _unitOfWork.HandoverInspectionRepository
            .GetHandoverInspectionByOrderAndTypeAsync(orderBookingId, "RETURN");
        _validationService.CheckNotFound(inspection, "Return inspection not found");
        return _mapper.Map<HandoverInspectionResponseDto>(inspection);
    }

    public async Task DeleteReturnSettlementAsync(string id)
    {
        var settlement = await _unitOfWork.ReturnSettlementRepository.GetByIdAsync(id);
        _validationService.CheckNotFound(settlement, "Return settlement not found");

        await _unitOfWork.ReturnSettlementRepository.DeleteAsync(settlement!);
        await _unitOfWork.SaveChangesAsync();
    }

    private string GetCurrentUserName()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
    }
}