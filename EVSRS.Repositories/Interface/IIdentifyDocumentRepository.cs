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
    public interface IIdentifyDocumentRepository: IGenericRepository<IdentifyDocument>
    {
        Task<IdentifyDocument?> GetByUserIdAsync(string userId);
        Task<PaginatedList<IdentifyDocument>> GetAllIdentifyDocument();
        Task<IdentifyDocument?> GetByIdAsync(string id);
        Task<bool> CheckExistByUserIdAsync(string userId);
        Task CreateIdentifyDocumentAsync(IdentifyDocument identifyDocument);
        Task UpdateIdentifyDocumentAsync(IdentifyDocument identifyDocument);
        Task DeleteIdentifyDocumentAsync(IdentifyDocument identifyDocument);
       
    }
}
