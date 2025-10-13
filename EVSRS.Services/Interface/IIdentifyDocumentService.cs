using EVSRS.BusinessObjects.DTO.IdentifyDocumentDto;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Interface
{
    public interface IIdentifyDocumentService
    {
        Task<PaginatedList<IdentifyDocumentResponseDto>> GetAllIdentifyDocumentAsync(int pageIndex, int pageSize);
        Task<IdentifyDocumentResponseDto?> GetIdentifyDocumentByIdAsync(string id);
        Task<IdentifyDocumentResponseDto?> GetIdentifyDocumentByUserIdAsync(string userId);
        Task<IdentifyDocumentResponseDto> CreateIdentifyDocumentAsync(IdentifyDocumentRequestDto identifyDocumentRequestDto);
        Task<IdentifyDocumentResponseDto> UpdateIdentifyDocumentAsync(string id, IdentifyDocumentRequestDto identifyDocumentRequestDto);
        Task<IdentifyDocumentResponseDto> UpdateIdentifyDocumentStatusAsync(string id, UpdateIdentifyDocumentStatusDto updateStatusDto);
        Task DeleteIdentifyDocumentAsync(string id);
    }
}
