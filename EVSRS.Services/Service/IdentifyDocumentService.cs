using AutoMapper;
using EVSRS.BusinessObjects.DTO.IdentifyDocumentDto;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Interface;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Service
{
    public class IdentifyDocumentService : IIdentifyDocumentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidationService _validationService;

        public IdentifyDocumentService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _validationService = validationService;
        }
        public async Task<IdentifyDocumentResponseDto> CreateIdentifyDocumentAsync(IdentifyDocumentRequestDto identifyDocumentRequestDto)
        {
            await _validationService.ValidateAndThrowAsync(identifyDocumentRequestDto);
            var newIdentifyDocument = _mapper.Map<BusinessObjects.Entity.IdentifyDocument>(identifyDocumentRequestDto);
            await _unitOfWork.IdentifyDocumentRepository.CreateIdentifyDocumentAsync(newIdentifyDocument);
            await _unitOfWork.SaveChangesAsync();
            
            var identifyDocumentDto = _mapper.Map<IdentifyDocumentResponseDto>(newIdentifyDocument);
            return identifyDocumentDto;
        }

        public async Task DeleteIdentifyDocumentAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var existingIdentifyDocument = await _unitOfWork.IdentifyDocumentRepository.GetByIdAsync(id);
            if (existingIdentifyDocument == null)
            {
                throw new KeyNotFoundException($"IdentifyDocument with id {id} not found.");
            }
            await _unitOfWork.IdentifyDocumentRepository.DeleteIdentifyDocumentAsync(existingIdentifyDocument);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PaginatedList<IdentifyDocumentResponseDto>> GetAllIdentifyDocumentAsync(int pageIndex, int pageSize)
        {
            var identifyDocuments = await _unitOfWork.IdentifyDocumentRepository.GetAllIdentifyDocument();
            var identifyDocumentDtos = _mapper.Map<List<IdentifyDocumentResponseDto>>(identifyDocuments.Items);
            var paginatedList = new PaginatedList<IdentifyDocumentResponseDto>(identifyDocumentDtos, identifyDocuments.TotalCount, pageIndex, pageSize);
            return paginatedList;
        }

        public async Task<IdentifyDocumentResponseDto?> GetIdentifyDocumentByIdAsync(string id)
        {
            var identifyDocument = await _unitOfWork.IdentifyDocumentRepository.GetByIdAsync(id);
            if (identifyDocument == null)
            {
                throw new KeyNotFoundException($"IdentifyDocument with id {id} not found.");
            }
            var identifyDocumentDto = _mapper.Map<IdentifyDocumentResponseDto>(identifyDocument);
            return identifyDocumentDto;
        }

        public async Task<IdentifyDocumentResponseDto?> GetIdentifyDocumentByUserIdAsync(string userId)
        {
            var identifyDocument =  await _unitOfWork.IdentifyDocumentRepository.GetByUserIdAsync(userId);
            if (identifyDocument == null)
            {
                throw new KeyNotFoundException($"IdentifyDocument with UserId {userId} not found.");
            }
            var identifyDocumentDto = _mapper.Map<IdentifyDocumentResponseDto>(identifyDocument);
            return identifyDocumentDto;
        }

        public async Task<IdentifyDocumentResponseDto> UpdateIdentifyDocumentAsync(string id, IdentifyDocumentRequestDto identifyDocumentRequestDto)
        {
            var existingIdentifyDocument = await _unitOfWork.IdentifyDocumentRepository.GetByIdAsync(id);
            if (existingIdentifyDocument == null)
            {
                throw new KeyNotFoundException($"IdentifyDocument with id {id} not found.");
            }
            _mapper.Map(identifyDocumentRequestDto, existingIdentifyDocument);
            existingIdentifyDocument.UpdatedAt = DateTime.UtcNow;
            existingIdentifyDocument.UpdatedBy = GetCurrentUserName();
            await _unitOfWork.IdentifyDocumentRepository.UpdateIdentifyDocumentAsync(existingIdentifyDocument);
            await _unitOfWork.SaveChangesAsync();
            
            var identifyDocumentDto = _mapper.Map<IdentifyDocumentResponseDto>(existingIdentifyDocument);
            return identifyDocumentDto;
        }

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }
    }
}
