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
    public class CarEVRepository : GenericRepository<CarEV> , ICarEVRepository
    {
        public CarEVRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }
        public async Task CreateCarEVAsync(CarEV carEV)
        {
            await InsertAsync(carEV);


        }

        public async Task DeleteCarEVAsync(CarEV carEV)
        {
            await DeleteAsync(carEV);
        }

        

        public async Task<CarEV?> GetCarEVByIdAsync(string id)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.Id == id).FirstOrDefaultAsync();
            return response;
        }

        public async Task<PaginatedList<CarEV>> GetCarEVList()
        {
            var response = await _dbSet.Include(c => c.Model).Include(c => c.Depot).Where(x => !x.IsDeleted).ToListAsync();
            return PaginatedList<CarEV>.Create(response, 1, response.Count);
        }

        public async Task<List<CarEV>> GetAllCarEVsByDepotIdAsync(string depotId)
        {
            return await _context.CarEVs
                .Where(c => c.DepotId == depotId && !c.IsDeleted)
                .ToListAsync();
        }

        public async Task<PaginatedList<CarEV>> GetCarEVsByDepotIdAsync(string depotId, int pageNumber, int pageSize)
        {
            var query = _context.CarEVs
                .Include(c => c.Model)
                    .ThenInclude(m => m.CarManufacture)
                .Include(c => c.Model)
                    .ThenInclude(m => m.Amenities)
                .Include(c => c.Depot)
                .Where(c => c.DepotId == depotId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt);

            return await PaginatedList<CarEV>.CreateAsync(query, pageNumber, pageSize);
        }

        

        public async Task UpdateCarEVAsync(CarEV carEV)
        {
            await UpdateAsync(carEV);
        }

        
    }
}
