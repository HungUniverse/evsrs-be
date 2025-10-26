using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.Repositories.Repository;

public class SystemConfigRepository: GenericRepository<SystemConfig>, ISystemConfigRepository
{
    public SystemConfigRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
    {
    }

    public async Task<PaginatedList<SystemConfig>> GetSystemConfigAsync()
    {
        var response = await _dbSet.Where(x => !x.IsDeleted).ToListAsync();
        return PaginatedList<SystemConfig>.Create(response, 1, response.Count);
    }

    public async Task<SystemConfig?> GetSystemConfigByIdAsync(string id)
    {
        var response = await _dbSet.Where(x => !x.IsDeleted && x.Id == id).FirstOrDefaultAsync();
        return response;
    }

    public async Task<SystemConfig?> GetSystemConfigByKeyAsync(string key)
    {
        var response = await _dbSet.Where(x => !x.IsDeleted && x.Key == key).FirstOrDefaultAsync();
        return response;
    }

    public async Task<List<SystemConfig>> GetSystemConfigsByTypeAsync(ConfigType configType)
    {
        var response = await _dbSet.Where(x => !x.IsDeleted && x.ConfigType == configType).ToListAsync();
        return response;
    }

    public async Task CreateSystemConfig(SystemConfig model)
    {
        await InsertAsync(model);
    }

    public async Task UpdateSystemConfig(SystemConfig model)
    {
        await UpdateAsync(model);
    }

    public async Task DeleteSystemConfig(SystemConfig model)
    {
        await DeleteAsync(model);
    }
}