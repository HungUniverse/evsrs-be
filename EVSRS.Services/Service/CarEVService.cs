using AutoMapper;
using EVSRS.BusinessObjects.DTO.CarEVDto;
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
    /// Service quản lý xe điện (CarEV): CRUD, trạng thái, và các truy vấn liên quan xe.
    /// </summary>
    public class CarEVService : ICarEVService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidationService _validationService;

        public CarEVService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _validationService = validationService;
        }
        public async Task CreateCarEVAsync(CarEVRequestDto carEV)
        {
            await _validationService.ValidateAndThrowAsync(carEV);
            var newCarEV = _mapper.Map<CarEV>(carEV);
            await _unitOfWork.CarEVRepository.CreateCarEVAsync(newCarEV);
            await _unitOfWork.SaveChangesAsync();

        }

        public async Task DeleteCarEVAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var existingCarEV = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(id);
            if (existingCarEV == null)
            {
                throw new KeyNotFoundException($"Car EV with ID {id} not found.");
            }
            await _unitOfWork.CarEVRepository.DeleteCarEVAsync(existingCarEV);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PaginatedList<CarEVResponseDto>> GetAllCarEVsAsync(int pageNumber, int pageSize)
        {
            var carEVs = await _unitOfWork.CarEVRepository.GetCarEVList();
            var carEVDtos = carEVs.Items.Select(cm => _mapper.Map<CarEVResponseDto>(cm)).ToList();
            var paginateList = new PaginatedList<CarEVResponseDto>(carEVDtos, carEVs.TotalCount, pageNumber, pageSize);
            return paginateList;

        }

        

        public async Task<CarEVResponseDto> GetCarEVByIdAsync(string id)
        {
            var carEV = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(id);
            if (carEV == null)
            {
                throw new KeyNotFoundException($"Car EV with ID {id} not found.");
            }
            await _validationService.ValidateAndThrowAsync(id);
            var carEVDto = _mapper.Map<CarEVResponseDto>(carEV);
            return carEVDto;

        }

       

        public async Task<CarEVResponseDto> UpdateCarEVAsync(string id, CarEVRequestDto carEV)
        {
            var existingCarEV = await _unitOfWork.CarEVRepository.GetCarEVByIdAsync(id);
            if (existingCarEV == null)
            {
                throw new KeyNotFoundException($"Car EV with ID {id} not found.");
            }
            await _validationService.ValidateAndThrowAsync(carEV);
            _mapper.Map(carEV, existingCarEV);
            existingCarEV.UpdatedBy = GetCurrentUserName();
            existingCarEV.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CarEVRepository.UpdateCarEVAsync(existingCarEV);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CarEVResponseDto>(existingCarEV);
        }

        public async Task<List<CarEVResponseDto>> GetAllCarEVsByDepotIdAsync(string depotId)
        {
            var carEVs = await _unitOfWork.CarEVRepository.GetAllCarEVsByDepotIdAsync(depotId);
            return carEVs.Select(c => _mapper.Map<CarEVResponseDto>(c)).ToList();
        }

        public async Task<PaginatedList<CarEVResponseDto>> GetCarEVsByDepotIdAsync(string depotId, int pageNumber, int pageSize)
        {
            var paginatedCarEVs = await _unitOfWork.CarEVRepository.GetCarEVsByDepotIdAsync(depotId, pageNumber, pageSize);
            
            var carEVDtos = paginatedCarEVs.Items.Select(c => _mapper.Map<CarEVResponseDto>(c)).ToList();
            
            return new PaginatedList<CarEVResponseDto>(
                carEVDtos, 
                paginatedCarEVs.TotalCount, 
                paginatedCarEVs.PageNumber, 
                paginatedCarEVs.PageSize
            );
        }

       

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }
    }
}
