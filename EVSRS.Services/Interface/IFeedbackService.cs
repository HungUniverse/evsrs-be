using EVSRS.BusinessObjects.DTO.FeedbackDto;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Interface
{
    public interface IFeedbackService
    {
        Task<PaginatedList<FeedbackResponseDto>> GetAllFeedbacksAsync(int pageNumber, int pageSize);
        Task<FeedbackResponseDto?> GetFeedbackByIdAsync(string id);
        Task CreateFeedbackAsync(FeedbackRequestDto feedbackRequestDto);
        Task UpdateFeedbackAsync(string id, FeedbackRequestDto feedbackRequestDto);
        Task DeleteFeedbackAsync(string id);
    }
}
