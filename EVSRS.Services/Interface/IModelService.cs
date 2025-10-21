using EVSRS.BusinessObjects.DTO.ModelDto;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Interface
{
    public interface IModelService
    {
        Task<PaginatedList<ModelResponseDto>> GetAllModelsAsync(int pageNumber, int pageSize);
        Task<ModelResponseDto> GetModelByIdAsync(string id);
        Task<ModelResponseDto> GetModelByNameAsync(string name);
        Task CreateModelAsync(ModelRequestDto model);
        Task UpdateModelAsync(string id, ModelRequestDto model);
        Task DeleteModelAsync(string id);
        Task<int> UpdateElectricityFeeForAllModelsAsync(UpdateElectricityFeeRequestDto request);
        Task<ModelResponseDto> UpdateOverageFeeAsync(string id, UpdateOverageFeeRequestDto request);
    }
}
