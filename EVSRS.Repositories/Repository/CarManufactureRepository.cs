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
    public class CarManufactureRepository : GenericRepository<CarManufacture>, ICarManufactureRepository
    {
        public CarManufactureRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }
        public async Task CreateCarManufactureAsync(CarManufacture carManufacture)
        {
            await InsertAsync(carManufacture);

        }  

        public async Task DeleteCarManufactureAsync(CarManufacture carManufacture)
        {
            await DeleteAsync(carManufacture);
        }

        public async Task<CarManufacture?> GetCarManufactureByIdAsync(string id)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.Id == id).FirstOrDefaultAsync();
            return response;
        }

        public async Task<CarManufacture?> GetCarManufactureByNameAsync(string name)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.Name == name).FirstOrDefaultAsync();
            return response;
        }

        public async Task<PaginatedList<CarManufacture>> GetCarManufactureListAsync()
        {
            var respone = await _dbSet.ToListAsync();
            return PaginatedList<CarManufacture>.Create(respone, 1, respone.Count);
        }

        public async Task UpdateCarManufactureAsync(CarManufacture carManufacture)
        {
            // Ensure the entity is tracked by EF
            var trackedEntity = await _dbSet.FirstOrDefaultAsync(x => x.Id == carManufacture.Id);
            if (trackedEntity != null)
            {
                // Update properties manually to ensure EF tracking works correctly
                trackedEntity.Name = carManufacture.Name;
                trackedEntity.Logo = carManufacture.Logo;
                trackedEntity.UpdatedAt = carManufacture.UpdatedAt;
                trackedEntity.UpdatedBy = carManufacture.UpdatedBy;
                
                await UpdateAsync(trackedEntity);
            }
            else
            {
                // Fallback to regular update if entity not found
                await UpdateAsync(carManufacture);
            }
        }
    }
}
