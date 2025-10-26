using AutoMapper;
using EVSRS.BusinessObjects.DTO.SystemConfigDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
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
    public class SystemConfigService : ISystemConfigService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidationService _validationService;

        public SystemConfigService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _validationService = validationService;
        }

        public async Task CreateSystemConfig(SystemConfigRequestDto systemConfigRequestDto)
        {
            await _validationService.ValidateAndThrowAsync(systemConfigRequestDto);
            
            // Check if key already exists
            var existingConfig = await _unitOfWork.SystemConfigRepository.GetSystemConfigByKeyAsync(systemConfigRequestDto.Key);
            if (existingConfig != null)
            {
                throw new InvalidOperationException($"System config with key '{systemConfigRequestDto.Key}' already exists.");
            }

            var newSystemConfig = _mapper.Map<SystemConfig>(systemConfigRequestDto);
            await _unitOfWork.SystemConfigRepository.CreateSystemConfig(newSystemConfig);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteSystemConfig(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var existingSystemConfig = await _unitOfWork.SystemConfigRepository.GetSystemConfigByIdAsync(id);
            if (existingSystemConfig == null)
            {
                throw new KeyNotFoundException($"System config with ID {id} not found.");
            }
            await _unitOfWork.SystemConfigRepository.DeleteSystemConfig(existingSystemConfig);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PaginatedList<SystemConfigResponseDto>> GetAllSystemConfigs()
        {
            var systemConfigs = await _unitOfWork.SystemConfigRepository.GetSystemConfigAsync();
            var systemConfigsDto = _mapper.Map<List<SystemConfigResponseDto>>(systemConfigs.Items);
            return PaginatedList<SystemConfigResponseDto>.Create(systemConfigsDto, systemConfigs.PageNumber, systemConfigs.TotalCount);
        }

        public async Task<SystemConfigResponseDto?> GetSystemConfigById(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var systemConfig = await _unitOfWork.SystemConfigRepository.GetSystemConfigByIdAsync(id);
            if (systemConfig == null)
            {
                return null;
            }
            return _mapper.Map<SystemConfigResponseDto>(systemConfig);
        }

        public async Task<SystemConfigResponseDto?> GetSystemConfigByKey(string key)
        {
            await _validationService.ValidateAndThrowAsync(key);
            var systemConfig = await _unitOfWork.SystemConfigRepository.GetSystemConfigByKeyAsync(key);
            if (systemConfig == null)
            {
                return null;
            }
            return _mapper.Map<SystemConfigResponseDto>(systemConfig);
        }

        public async Task<List<SystemConfigResponseDto>> GetSystemConfigsByType(ConfigType configType)
        {
            var systemConfigs = await _unitOfWork.SystemConfigRepository.GetSystemConfigsByTypeAsync(configType);
            return _mapper.Map<List<SystemConfigResponseDto>>(systemConfigs);
        }

        public async Task UpdateSystemConfig(string id, SystemConfigRequestDto systemConfigRequestDto)
        {
            await _validationService.ValidateAndThrowAsync(systemConfigRequestDto);
            var existingSystemConfig = await _unitOfWork.SystemConfigRepository.GetSystemConfigByIdAsync(id);
            if (existingSystemConfig == null)
            {
                throw new KeyNotFoundException($"System config with ID {id} not found.");
            }

            // Check if key already exists for another config
            var existingConfigWithKey = await _unitOfWork.SystemConfigRepository.GetSystemConfigByKeyAsync(systemConfigRequestDto.Key);
            if (existingConfigWithKey != null && existingConfigWithKey.Id != id)
            {
                throw new InvalidOperationException($"System config with key '{systemConfigRequestDto.Key}' already exists.");
            }

            _mapper.Map(systemConfigRequestDto, existingSystemConfig);
            await _unitOfWork.SystemConfigRepository.UpdateSystemConfig(existingSystemConfig);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}