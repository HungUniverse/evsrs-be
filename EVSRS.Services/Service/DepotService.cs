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

        public async Task CreateDepotAsync(DepotRequestDto depot)
        {
            await _validationService.ValidateAndThrowAsync(depot);
            var newDepot = _mapper.Map<Depot>(depot);
            await _unitOfWork.DepotRepository.CreateDepotAsync(newDepot);
            await _unitOfWork.SaveChangesAsync();
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

        public async Task<DepotResponseDto> GetDepotByIdAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var depot = await _unitOfWork.DepotRepository.GetDepotById(id);
            if (depot == null)
            {
                throw new KeyNotFoundException($"Depot with ID {id} not found.");
            }
            var depotDto = _mapper.Map<DepotResponseDto>(depot);
            return depotDto;


        }

        public async Task<DepotResponseDto> GetDepotByMapId(string mapId)
        {
            await _validationService.ValidateAndThrowAsync(mapId);
            var depot = await _unitOfWork.DepotRepository.GetDepotByMapId(mapId);
            if (depot == null)
            {
                throw new KeyNotFoundException($"Depot with Map ID {mapId} not found.");
            }
            var depotDto = _mapper.Map<DepotResponseDto>(depot);
            return depotDto;
        }

        public async Task<DepotResponseDto> GetDepotByNameAsync(string name)
        {
            await _validationService.ValidateAndThrowAsync(name);
            var depot = await _unitOfWork.DepotRepository.GetDepotByName(name);
            if (depot == null)
            {
                throw new KeyNotFoundException($"Depot with Name {name} not found.");
            }
            var depotDto = _mapper.Map<DepotResponseDto>(depot);
            return depotDto;
        }

        public async Task UpdateDepotAsync(Depot depot)
        {
            var existingDepot = await _unitOfWork.DepotRepository.GetDepotById(depot.Id);
            if (existingDepot == null)
            {
                throw new KeyNotFoundException($"Depot with ID {depot.Id} not found.");
            }
            existingDepot.UpdatedAt = DateTime.UtcNow;
            existingDepot.UpdatedBy = GetCurrentUserName();
            
            await _unitOfWork.DepotRepository.UpdateDepotAsync(depot);
            await _unitOfWork.SaveChangesAsync();
        }

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }
    }
}
