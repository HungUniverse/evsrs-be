using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Repositories.Interface
{
    public interface IAmenitiesRepository: IGenericRepository<Amenities>
    {
        Task<PaginatedList<Amenities>> GetAllAmenities();
        Task<Amenities?> GetAmenitiesById(string id);
        Task<Amenities?> GetAmenitiesByName(string name);
        Task CreateAmenities(Amenities amenities);
        Task UpdateAmenities(Amenities amenities);
        Task DeleteAmenities(Amenities amenities);

    }
}
