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
    public interface IDepotRepository: IGenericRepository<Depot>
    {
        Task <PaginatedList<Depot>> GetAllDepot();
        Task <Depot> GetDepotById(string id);
        Task <Depot> GetDepotByName(string name);
        Task <Depot> GetDepotByMapId(string mapId);
        Task CreateDepotAsync(Depot depot);
        Task DeleteDepotAsync(Depot depot);
        Task UpdateDepotAsync(Depot depot);
    }
}
