using EVSRS.BusinessObjects.DTO.CarManufactureDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Interface
{
    public interface ICarManufactureService
    {
        Task<PaginatedList<CarManufactureResponseDto>> GetAllCarManufacturesAsync(int pageNumber, int pageSize);
        Task<CarManufactureResponseDto> GetCarManufactureByIdAsync(string id);
        Task<CarManufactureResponseDto> GetCarManufactureByNameAsync(string name);
        Task CreateCarManufactureAsync(CarManufactureRequestDto carManufacture);
        Task<CarManufactureResponseDto> UpdateCarManufactureAsync(string id, CarManufactureRequestDto carManufacture);
        Task DeleteCarManufactureAsync(string id);
    }
}
