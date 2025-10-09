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
    public class FeedbackRepository : GenericRepository<Feedback>, IFeedbackRepository
    {
        public FeedbackRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }
        public async Task CreateFeedbackAsync(Feedback feedback)
        {
            await InsertAsync(feedback);
        }

        public async Task DeleteFeedbackAsync(Feedback feedback)
        {
            await DeleteAsync(feedback);
        }

        public async Task<PaginatedList<Feedback>> GetFeedbacksAsync()
        {
            var response = await _dbSet.ToListAsync();
            return PaginatedList<Feedback>.Create(response, 1, response.Count);
        }

        public async Task<Feedback?> GetFeedbackByIdAsync(string id)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.Id == id).FirstOrDefaultAsync();
            return response;
        }

        public async Task UpdateFeedbackAsync(Feedback feedback)
        {
            await UpdateAsync(feedback);
        }
    }
}
