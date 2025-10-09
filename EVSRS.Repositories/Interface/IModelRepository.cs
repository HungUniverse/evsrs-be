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
    public interface IModelRepository: IGenericRepository<Model>
    {
        Task<PaginatedList<Model>> GetModelListAsync();
        Task<Model?> GetModelByIdAsync(string id);
        Task<Model?> GetModelByNameAsync(string name);
        Task CreateModelAsync(Model model);
        Task UpdateModelAsync(Model model);
        Task DeleteModelAsync(Model model);
    }
}
