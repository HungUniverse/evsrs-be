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
    public interface ICarEVRepository: IGenericRepository<CarEV>
    {
        Task<PaginatedList<CarEV>> GetCarEVList();
        Task<CarEV?> GetCarEVByIdAsync(string id);
        Task CreateCarEVAsync(CarEV carEV);
        Task UpdateCarEVAsync(CarEV carEV);
        Task DeleteCarEVAsync(CarEV carEV);

    }
}
