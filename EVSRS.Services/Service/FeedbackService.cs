using AutoMapper;
using EVSRS.BusinessObjects.DTO.FeedbackDto;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Service
{
    public class FeedbackService: IFeedbackService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidationService _validationService;

        public FeedbackService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _validationService = validationService;
        }

        public async Task CreateFeedbackAsync(FeedbackRequestDto feedbackRequestDto)
        {
            await _validationService.ValidateAndThrowAsync(feedbackRequestDto);
            var newFeedback = _mapper.Map<EVSRS.BusinessObjects.Entity.Feedback>(feedbackRequestDto);
            await _unitOfWork.FeedbackRepository.CreateFeedbackAsync(newFeedback);
            await _unitOfWork.SaveChangesAsync();

        }

        public async Task DeleteFeedbackAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var existingFeedback = await _unitOfWork.FeedbackRepository.GetFeedbackByIdAsync(id);
            if (existingFeedback == null)
            {
                throw new KeyNotFoundException($"Feedback with ID {id} not found.");
            }
            await _unitOfWork.FeedbackRepository.DeleteFeedbackAsync(existingFeedback);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PaginatedList<FeedbackResponseDto>> GetAllFeedbacksAsync(int pageNumber, int pageSize)
        {
            var feedbacks = await _unitOfWork.FeedbackRepository.GetFeedbacksAsync();
            var feedbackDtos = feedbacks.Items.Select(cm => _mapper.Map<FeedbackResponseDto>(cm)).ToList();
            var paginateList = new PaginatedList<FeedbackResponseDto>(feedbackDtos, feedbacks.TotalCount, pageNumber, pageSize);
            return paginateList;
        }

        public async Task<FeedbackResponseDto?> GetFeedbackByIdAsync(string id)
        {
            var feedback = await _unitOfWork.FeedbackRepository.GetFeedbackByIdAsync(id);
            if (feedback == null)
            {
                return null;
            }
            var feedbackDto = _mapper.Map<FeedbackResponseDto>(feedback);   
            return feedbackDto;
        }

        public async Task UpdateFeedbackAsync(string id, FeedbackRequestDto feedbackRequestDto)
        {
            var existingFeedback = await _unitOfWork.FeedbackRepository.GetFeedbackByIdAsync(id);
            if (existingFeedback == null)
            {
                throw new KeyNotFoundException($"Feedback with ID {id} not found.");
            }
            await _validationService.ValidateAndThrowAsync(feedbackRequestDto);
            _mapper.Map(feedbackRequestDto, existingFeedback);
            existingFeedback.UpdatedAt = DateTime.UtcNow;
            existingFeedback.UpdatedBy = GetCurrentUserName();
            await _unitOfWork.FeedbackRepository.UpdateFeedbackAsync(existingFeedback);
            await _unitOfWork.SaveChangesAsync();

        }

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }
    }
}
