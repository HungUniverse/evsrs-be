using EVSRS.BusinessObjects.DTO.DepotDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Interface
{
    public interface IDepotService
    {
        Task<PaginatedList<DepotResponseDto>> GetAllDepotAsync(int page, int pageSize);
        Task<DepotResponseDto> GetDepotByIdAsync(string id);
        Task<DepotResponseDto> GetDepotByNameAsync(string name);
        Task<DepotResponseDto> GetDepotByMapId(string mapId);
        Task CreateDepotAsync(DepotRequestDto depot);
        Task UpdateDepotAsync(String id, Depot depot);
        Task DeleteDepotAsync(string id);


        


    }
}
