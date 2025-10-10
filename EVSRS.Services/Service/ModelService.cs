using AutoMapper;
using EVSRS.BusinessObjects.DTO.ModelDto;
using EVSRS.BusinessObjects.Entity;
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
    public class ModelService : IModelService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidationService _validationService;

        public ModelService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _validationService = validationService;
        }
        public async Task CreateModelAsync(ModelRequestDto model)
        {
            await _validationService.ValidateAndThrowAsync(model);
            var newModel = _mapper.Map<Model>(model);
            await _unitOfWork.ModelRepository.CreateModelAsync(newModel);
            await _unitOfWork.SaveChangesAsync();



        }

        public async Task DeleteModelAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var existingModel = await _unitOfWork.ModelRepository.GetModelByIdAsync(id);
            if (existingModel == null)
            {
                throw new KeyNotFoundException($"Model with ID {id} not found.");
            }
            await _unitOfWork.ModelRepository.DeleteModelAsync(existingModel);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PaginatedList<ModelResponseDto>> GetAllModelsAsync(int pageNumber, int pageSize)
        {
            var models = await  _unitOfWork.ModelRepository.GetModelListAsync();
            var modelDtos = models.Items.Select(m => _mapper.Map<ModelResponseDto>(m)).ToList();
            var paginateList = new PaginatedList<ModelResponseDto>(modelDtos, models.TotalCount, pageNumber, pageSize);
            return paginateList;

        }

        public async Task<ModelResponseDto> GetModelByIdAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var existingModel = await _unitOfWork.ModelRepository.GetModelByIdAsync(id);
            if (existingModel == null)
            {
                throw new KeyNotFoundException($"Model with ID {id} not found.");
            }
            var modelDto = _mapper.Map<ModelResponseDto>(existingModel);
            return modelDto;
        }

        public async Task<ModelResponseDto> GetModelByNameAsync(string name)
        {
            await _validationService.ValidateAndThrowAsync(name);
            var existingModel = await _unitOfWork.ModelRepository.GetModelByNameAsync(name);
            if (existingModel == null)
            {
                throw new KeyNotFoundException($"Model with Name {name} not found.");
            }
            var modelDto = _mapper.Map<ModelResponseDto>(existingModel);
            return modelDto;

        }

        public async Task UpdateModelAsync(string id, ModelRequestDto model)
        {
            var existingModel = await _unitOfWork.ModelRepository.GetByIdAsync(id);
            if (existingModel == null)
            {
                throw new KeyNotFoundException($"Model with ID {id} not found.");
            }
            existingModel.UpdatedAt = DateTime.Now;
            existingModel.UpdatedBy = GetCurrentUserName();
            await _unitOfWork.ModelRepository.UpdateModelAsync(existingModel);
            await _unitOfWork.SaveChangesAsync();

        }

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }
    }
}
