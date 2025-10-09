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
    public class ModelRepository : GenericRepository<Model>, IModelRepository
    {
        public ModelRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }
        public async Task CreateModelAsync(Model model)
        {
            await InsertAsync(model);
        }

        public async Task DeleteModelAsync(Model model)
        {
            await DeleteAsync(model);
        }

        public async Task<Model?> GetModelByIdAsync(string id)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.Id == id).FirstOrDefaultAsync();
            return response;
        }

        public async Task<Model?> GetModelByNameAsync(string name)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.ModelName == name).FirstOrDefaultAsync();
            return response;
        }

        public async Task<PaginatedList<Model>> GetModelListAsync()
        {
            var respone = await _dbSet.ToListAsync();
            return PaginatedList<Model>.Create(respone, 1, respone.Count);
        }

        public async Task UpdateModelAsync(Model model)
        {
            // Ensure the entity is tracked by EF
            var trackedEntity = await _dbSet.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (trackedEntity != null)
            {
                // Update properties manually to ensure EF tracking works correctly
                trackedEntity.ModelName = model.ModelName;
                
                trackedEntity.LimiteDailyKm = model.LimiteDailyKm;
                trackedEntity.RangeKm = model.RangeKm;
                trackedEntity.Seats = model.Seats;
                trackedEntity.Price = model.Price;
                trackedEntity.BatteryCapacityKwh = model.BatteryCapacityKwh;
                trackedEntity.UpdatedAt = model.UpdatedAt;
                trackedEntity.UpdatedBy = model.UpdatedBy;
                
                await UpdateAsync(trackedEntity);
            }
            else
            {
                // Fallback to regular update if entity not found
                await UpdateAsync(model);
            }
        }

       
    }
}
