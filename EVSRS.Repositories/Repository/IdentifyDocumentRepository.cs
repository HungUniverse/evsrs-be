using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Repositories.Repository
{
    public class IdentifyDocumentRepository : GenericRepository<IdentifyDocument>, IIdentifyDocumentRepository
    {
        public IdentifyDocumentRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }   
        public async Task<bool> CheckExistByUserIdAsync(string userId)
        {
            return await _dbSet.AnyAsync(x => x.UserId == userId && !x.IsDeleted);
        }

        public async Task CreateIdentifyDocumentAsync(IdentifyDocument identifyDocument)
        {
            await InsertAsync(identifyDocument);
        }

        public async Task DeleteIdentifyDocumentAsync(IdentifyDocument identifyDocument)
        {
            await DeleteAsync(identifyDocument);
        }

        public async Task<PaginatedList<IdentifyDocument>> GetAllIdentifyDocument()
        {
            var response = await _dbSet.Where(x => !x.IsDeleted).ToListAsync();
            return PaginatedList<IdentifyDocument>.Create(response, 1, response.Count);
        }

        public async Task<IdentifyDocument?> GetByIdAsync(string id)
        {
            var response = await _dbSet.Where(x => x.Id == id && !x.IsDeleted).FirstOrDefaultAsync();
            return response;
        }

        public async Task<IdentifyDocument?> GetByUserIdAsync(string userId)
        {
            var response = await _dbSet.Where(x => x.UserId == userId && !x.IsDeleted).FirstOrDefaultAsync();
            return response;
        }

        public async Task UpdateIdentifyDocumentAsync(IdentifyDocument identifyDocument)
        {
            await UpdateAsync(identifyDocument);
        }
    }
}
