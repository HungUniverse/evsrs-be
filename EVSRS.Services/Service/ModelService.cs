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
            
           
            
            // Manual mapping to ensure proper create
            var newModel = new Model
            {
                ModelName = model.ModelName,
              
                LimiteDailyKm = model.LimiteDailyKm,
                RangeKm = model.RangeKm,
                Seats = model.Seats,
                Price = model.Price,
                BatteryCapacityKwh = model.BatteryCapacityKwh
            };
            
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

        public async Task<ModelResponseDto> UpdateModelAsync(string id, ModelRequestDto model)
        {
            await _validationService.ValidateAndThrowAsync(model);
            var existingModel = await _unitOfWork.ModelRepository.GetModelByIdAsync(id);
            if (existingModel == null)
            {
                throw new KeyNotFoundException($"Model with ID {id} not found.");
            }
            
            // Manual mapping to ensure proper update
            existingModel.ModelName = model.ModelName;
            
            existingModel.LimiteDailyKm = model.LimiteDailyKm;
            existingModel.RangeKm = model.RangeKm;
            existingModel.Seats = model.Seats;
            existingModel.Price = model.Price;
            existingModel.BatteryCapacityKwh = model.BatteryCapacityKwh;
            existingModel.UpdatedBy = GetCurrentUserName();
            existingModel.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.ModelRepository.UpdateModelAsync(existingModel);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<ModelResponseDto>(existingModel);

        }

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }
    }
}
