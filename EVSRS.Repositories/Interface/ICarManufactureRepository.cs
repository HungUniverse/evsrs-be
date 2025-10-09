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
    public interface ICarManufactureRepository: IGenericRepository<CarManufacture>
    {
        Task<PaginatedList<CarManufacture>> GetCarManufactureListAsync();
        Task<CarManufacture?> GetCarManufactureByIdAsync(string id);
        Task<CarManufacture?> GetCarManufactureByNameAsync(string name);
        Task CreateCarManufactureAsync(CarManufacture carManufacture);
        Task UpdateCarManufactureAsync(CarManufacture carManufacture);
        Task DeleteCarManufactureAsync(CarManufacture carManufacture);
    }
}
