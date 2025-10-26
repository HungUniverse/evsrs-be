using EVSRS.BusinessObjects.DTO.SystemConfigDto;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Interface
{
    public interface ISystemConfigService
    {
        Task<PaginatedList<SystemConfigResponseDto>> GetAllSystemConfigs();
        Task<SystemConfigResponseDto?> GetSystemConfigById(string id);
        Task<SystemConfigResponseDto?> GetSystemConfigByKey(string key);
        Task<List<SystemConfigResponseDto>> GetSystemConfigsByType(ConfigType configType);
        Task CreateSystemConfig(SystemConfigRequestDto systemConfigRequestDto);
        Task UpdateSystemConfig(string id, SystemConfigRequestDto systemConfigRequestDto);
        Task DeleteSystemConfig(string id);
    }
}