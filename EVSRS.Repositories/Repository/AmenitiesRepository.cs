using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Repositories.Repository
{
    public class AmenitiesRepository : GenericRepository<Amenities>, IAmenitiesRepository
    {
        public AmenitiesRepository(EVSRS.BusinessObjects.DBContext.ApplicationDbContext context, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }
        public async Task CreateAmenities(Amenities amenities)
        {
            await InsertAsync(amenities);

        }

        public async Task DeleteAmenities(Amenities amenities)
        {
            await DeleteAsync(amenities);
        }

        public async Task<PaginatedList<Amenities>> GetAllAmenities()
        {
            var response = await _dbSet.ToListAsync();
            return PaginatedList<Amenities>.Create(response, 1, response.Count);
        }

        public async Task<Amenities?> GetAmenitiesById(string id)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.Id == id).FirstOrDefaultAsync();
            return response;
        }

        public async Task<Amenities?> GetAmenitiesByName(string name)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.Name == name).FirstOrDefaultAsync();
            return response;
        }

        public async Task UpdateAmenities(Amenities amenities)
        {
            await UpdateAsync(amenities);
        }
    }
}
