using EVSRS.BusinessObjects.DTO.AmenitiesDto;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Interface
{
    public interface IAmenitiesService
    {
        Task<PaginatedList<AmenitiesResponseDto>> GetAllAmenities();
        Task<AmenitiesResponseDto?> GetAmenitiesById(string id);
        Task<AmenitiesResponseDto?> GetAmenitiesByName(string name);
        Task CreateAmenities(AmenitiesRequestDto amenitiesRequestDto);
        Task UpdateAmenities(string id, AmenitiesRequestDto amenitiesRequestDto);
        Task DeleteAmenities(string id);
    }
}
