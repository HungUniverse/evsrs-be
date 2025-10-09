using AutoMapper;
using EVSRS.BusinessObjects.DTO.CarManufactureDto;
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
    public class CarManufactureService : ICarManufactureService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidationService _validationService;

        public CarManufactureService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _validationService = validationService;
        }

        public async Task CreateCarManufactureAsync(CarManufactureRequestDto carManufacture)
        {
            await _validationService.ValidateAndThrowAsync(carManufacture);
            var newCarManufacture = _mapper.Map<CarManufacture>(carManufacture);
            await _unitOfWork.CarManufactureRepository.CreateCarManufactureAsync(newCarManufacture);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteCarManufactureAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var existingCarManufacture = await _unitOfWork.CarManufactureRepository.GetCarManufactureByIdAsync(id);
            if (existingCarManufacture == null)
            {
                throw new KeyNotFoundException($"Car Manufacture with ID {id} not found.");
            }
            await _unitOfWork.CarManufactureRepository.DeleteCarManufactureAsync(existingCarManufacture);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PaginatedList<CarManufactureResponseDto>> GetAllCarManufacturesAsync(int pageNumber, int pageSize)
        {
            var carManufactures = await _unitOfWork.CarManufactureRepository.GetCarManufactureListAsync();
            var carManufactureDtos = carManufactures.Items.Select(cm => _mapper.Map<CarManufactureResponseDto>(cm)).ToList();
            var paginateList = new PaginatedList<CarManufactureResponseDto>(carManufactureDtos, carManufactures.TotalCount, pageNumber, pageSize);
            return paginateList;
        }

        public async Task<CarManufactureResponseDto> GetCarManufactureByIdAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var existingCarManufacture = await _unitOfWork.CarManufactureRepository.GetCarManufactureByIdAsync(id);
            if (existingCarManufacture == null)
            {
                throw new KeyNotFoundException($"Car Manufacture with ID {id} not found.");
            }
            return _mapper.Map<CarManufactureResponseDto>(existingCarManufacture);
        }
        public async Task<CarManufactureResponseDto> GetCarManufactureByNameAsync(string name)
        {
            await _validationService.ValidateAndThrowAsync(name);
            var existingCarManufacture = await _unitOfWork.CarManufactureRepository.GetCarManufactureByNameAsync(name);
            if (existingCarManufacture == null)
            {
                throw new KeyNotFoundException($"Car Manufacture with Name {name} not found.");
            }
            return _mapper.Map<CarManufactureResponseDto>(existingCarManufacture);
        }

        public async Task<CarManufactureResponseDto> UpdateCarManufactureAsync(string id, CarManufactureRequestDto carManufacture)
        {
            await _validationService.ValidateAndThrowAsync(carManufacture);
            var existingCarManufacture = await _unitOfWork.CarManufactureRepository.GetCarManufactureByIdAsync(id);
            if (existingCarManufacture == null)
            {
                throw new KeyNotFoundException($"Car Manufacture with ID {id} not found.");
            }
            
            // Manual mapping to ensure proper update
            existingCarManufacture.Name = carManufacture.Name;
            existingCarManufacture.Logo = carManufacture.Logo;
            existingCarManufacture.UpdatedBy = GetCurrentUserName();
            existingCarManufacture.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.CarManufactureRepository.UpdateCarManufactureAsync(existingCarManufacture);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<CarManufactureResponseDto>(existingCarManufacture);

        }

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }
    }
}
