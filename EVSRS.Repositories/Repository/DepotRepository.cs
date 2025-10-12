using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Repositories.Repository
{
    public class DepotRepository : GenericRepository<Depot>, IDepotRepository
    {
        public DepotRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }

        public async Task CreateDepotAsync(Depot depot)
        {
            await InsertAsync(depot);
        }

        public async Task DeleteDepotAsync(Depot depot)
        {
            await DeleteAsync(depot);
        }

        public async Task<PaginatedList<Depot>> GetAllDepot()
        {
            var response = await _dbSet.Where(x => !x.IsDeleted).ToListAsync();
            return PaginatedList<Depot>.Create(response, 1, response.Count);
        }

        public async Task<Depot?> GetDepotById(string id)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.Id == id).FirstOrDefaultAsync();
            return response;
        }

        public async Task<Depot?> GetDepotByMapId(string mapId)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.MapId == mapId).FirstOrDefaultAsync();
            return response;
            
        }

        public async Task<Depot?> GetDepotByName(string name)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.Name == name).FirstOrDefaultAsync();
            return response;
        }

        public async Task<PaginatedList<Depot>> GetDepotsByLocationAsync(string? province, string? district, int page, int pageSize)
        {
            var query = _dbSet.Where(x => !x.IsDeleted);

            if (!string.IsNullOrEmpty(province))
            {
                query = query.Where(x => x.Province != null && x.Province.Contains(province));
            }

            if (!string.IsNullOrEmpty(district))
            {
                query = query.Where(x => x.District != null && x.District.Contains(district));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedList<Depot>(items, totalCount, page, pageSize);
        }

        public async Task UpdateDepotAsync(Depot depot)
        {
            await UpdateAsync(depot);
        }
    }
}
