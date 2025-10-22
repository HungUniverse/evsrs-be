using EVSRS.BusinessObjects.DTO.CarEVDto;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Interface
{
    public interface ICarEVService
    {
        Task<PaginatedList<CarEVResponseDto>> GetAllCarEVsAsync(int pageNumber, int pageSize);
        Task<CarEVResponseDto> GetCarEVByIdAsync(string id);
        
        Task CreateCarEVAsync(CarEVRequestDto carEV);
        Task<CarEVResponseDto> UpdateCarEVAsync(string id, CarEVRequestDto carEV);
        Task DeleteCarEVAsync(string id);
        Task<List<CarEVResponseDto>> GetAllCarEVsByDepotIdAsync(string depotId);
    }
}
