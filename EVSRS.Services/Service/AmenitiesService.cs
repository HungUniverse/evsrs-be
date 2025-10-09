using AutoMapper;
using EVSRS.BusinessObjects.DTO.AmenitiesDto;
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
    public class AmenitiesService: IAmenitiesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidationService _validationService;

        public AmenitiesService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _validationService = validationService;
        }

        public async Task CreateAmenities(AmenitiesRequestDto amenitiesRequestDto)
        {
            await _validationService.ValidateAndThrowAsync(amenitiesRequestDto);
            var newAmenities = _mapper.Map<EVSRS.BusinessObjects.Entity.Amenities>(amenitiesRequestDto);
            await _unitOfWork.AmenitiesRepository.CreateAmenities(newAmenities);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAmenities(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var existingAmenities = await _unitOfWork.AmenitiesRepository.GetAmenitiesById(id);
            if (existingAmenities == null)
            {
                throw new KeyNotFoundException($"Amenities with ID {id} not found.");
            }
            await _unitOfWork.AmenitiesRepository.DeleteAmenities(existingAmenities);
        }

        public async Task<PaginatedList<AmenitiesResponseDto>> GetAllAmenities()
        {
            var amenities = await _unitOfWork.AmenitiesRepository.GetAllAmenities();
            var amenitiesDtos = amenities.Items.Select(cm => _mapper.Map<AmenitiesResponseDto>(cm)).ToList();
            var paginateList = new PaginatedList<AmenitiesResponseDto>(amenitiesDtos, amenities.TotalCount, 1, amenities.TotalCount);
            return paginateList;
        }

        public async Task<AmenitiesResponseDto?> GetAmenitiesById(string id)
        {
            var amenities = await _unitOfWork.AmenitiesRepository.GetAmenitiesById(id);
            if (amenities == null)
            {
                return null;
            }
            var amenitiesDto = _mapper.Map<AmenitiesResponseDto>(amenities);
            return amenitiesDto;
        }

        public async Task<AmenitiesResponseDto?> GetAmenitiesByName(string name)
        {
            await _validationService.ValidateAndThrowAsync(name);
            var amenities = await _unitOfWork.AmenitiesRepository.GetAmenitiesByName(name);
            if (amenities == null)
            {
                return null;
            }
            var amenitiesDto = _mapper.Map<AmenitiesResponseDto>(amenities);
            return amenitiesDto;
        }

        public async Task UpdateAmenities(string id, AmenitiesRequestDto amenitiesRequestDto)
        {
            var existingAmenities = await _unitOfWork.AmenitiesRepository.GetAmenitiesById(id);
            if (existingAmenities == null)
            {
                throw new KeyNotFoundException($"Amenities with ID {id} not found.");
            }
            _mapper.Map(amenitiesRequestDto, existingAmenities);
            existingAmenities.UpdatedBy = GetCurrentUserName();
            existingAmenities.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.AmenitiesRepository.UpdateAmenities(existingAmenities);
            await _unitOfWork.SaveChangesAsync();

        }

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }
    }
}
