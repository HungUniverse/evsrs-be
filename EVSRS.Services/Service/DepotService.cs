using AutoMapper;
using EVSRS.BusinessObjects.DTO.DepotDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Service
{
    /// <summary>
    /// Service quản lý depot: thông tin depot, cấu hình, và các API liên quan depot.
    /// </summary>
    public class DepotService : IDepotService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidationService _validationService;

        public DepotService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _validationService = validationService;
        }

        public async Task<DepotResponseDto> CreateDepotAsync(DepotRequestDto depot)
        {
            await _validationService.ValidateAndThrowAsync(depot);
            var newDepot = _mapper.Map<Depot>(depot);
            await _unitOfWork.DepotRepository.CreateDepotAsync(newDepot);
            await _unitOfWork.SaveChangesAsync();
            
            var depotDto = _mapper.Map<DepotResponseDto>(newDepot);
            return depotDto;
        }

        public async Task DeleteDepotAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var existingDepot = await _unitOfWork.DepotRepository.GetDepotById(id);
            if (existingDepot == null)
            {
                throw new KeyNotFoundException($"Depot with ID {id} not found.");
            }
            await _unitOfWork.DepotRepository.DeleteDepotAsync(existingDepot);
            await _unitOfWork.SaveChangesAsync();


        }

        public async Task<PaginatedList<DepotResponseDto>> GetAllDepotAsync(int page, int pageSize)
        {
            var depots = await _unitOfWork.DepotRepository.GetAllDepot();
            var depotDtos = depots.Items.Select(d => _mapper.Map<DepotResponseDto>(d)).ToList();
            var paginateList = new PaginatedList<DepotResponseDto>(depotDtos, depots.TotalCount, page, pageSize);
            return paginateList;
        }

        public async Task<DepotResponseDto?> GetDepotByIdAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var depot = await _unitOfWork.DepotRepository.GetDepotById(id);
            if (depot == null)
            {
                return null;
            }
            var depotDto = _mapper.Map<DepotResponseDto>(depot);
            return depotDto;
        }

        public async Task<DepotResponseDto?> GetDepotByMapId(string mapId)
        {
            await _validationService.ValidateAndThrowAsync(mapId);
            var depot = await _unitOfWork.DepotRepository.GetDepotByMapId(mapId);
            if (depot == null)
            {
                return null;
            }
            var depotDto = _mapper.Map<DepotResponseDto>(depot);
            return depotDto;
        }

        public async Task<DepotResponseDto?> GetDepotByNameAsync(string name)
        {
            await _validationService.ValidateAndThrowAsync(name);
            var depot = await _unitOfWork.DepotRepository.GetDepotByName(name);
            if (depot == null)
            {
                return null;
            }
            var depotDto = _mapper.Map<DepotResponseDto>(depot);
            return depotDto;
        }

        public async Task<PaginatedList<DepotResponseDto>> GetDepotsByLocationAsync(string? province, string? district, int page, int pageSize)
        {
            var depots = await _unitOfWork.DepotRepository.GetDepotsByLocationAsync(province, district, page, pageSize);
            var depotDtos = depots.Items.Select(d => _mapper.Map<DepotResponseDto>(d)).ToList();
            var paginatedList = new PaginatedList<DepotResponseDto>(depotDtos, depots.TotalCount, page, pageSize);
            return paginatedList;
        }

        public async Task<DepotResponseDto> UpdateDepotAsync(String id, DepotRequestDto depot)
        {
            var existingDepot = await _unitOfWork.DepotRepository.GetDepotById(id);
            if (existingDepot == null)
            {
                throw new KeyNotFoundException($"Depot with ID {id} not found.");
            }
            existingDepot.UpdatedAt = DateTime.UtcNow;
            existingDepot.UpdatedBy = GetCurrentUserName();
            _mapper.Map(depot, existingDepot);
            await _unitOfWork.DepotRepository.UpdateDepotAsync(existingDepot);
            await _unitOfWork.SaveChangesAsync();
            
            var depotDto = _mapper.Map<DepotResponseDto>(existingDepot);
            return depotDto;
        }

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }
    }
}
