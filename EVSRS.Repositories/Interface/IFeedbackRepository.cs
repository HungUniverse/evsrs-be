using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Repositories.Interface
{
    public interface IFeedbackRepository
    {
        Task<PaginatedList<Feedback>> GetFeedbacksAsync();
        Task<Feedback?> GetFeedbackByIdAsync(string id);
        Task CreateFeedbackAsync(Feedback feedback);
        Task UpdateFeedbackAsync(Feedback feedback);
        Task DeleteFeedbackAsync(Feedback feedback);
    }
}
