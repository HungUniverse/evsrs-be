using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;

namespace EVSRS.Repositories.Interface;

public interface ISystemConfigRepository : IGenericRepository<SystemConfig>
{
    Task<PaginatedList<SystemConfig>> GetSystemConfigAsync();
    Task<SystemConfig?> GetSystemConfigByIdAsync(string id);
    Task<SystemConfig?> GetSystemConfigByKeyAsync(string key);
    Task<List<SystemConfig>> GetSystemConfigsByTypeAsync(ConfigType configType);
    Task CreateSystemConfig(SystemConfig model);
    Task UpdateSystemConfig(SystemConfig model);
    Task DeleteSystemConfig(SystemConfig model);
}