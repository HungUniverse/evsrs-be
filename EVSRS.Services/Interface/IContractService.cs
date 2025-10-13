using EVSRS.BusinessObjects.DTO.ContractDto;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Interface
{
    public interface IContractService
    {
        Task<PaginatedList<ContractResponseDto>> GetAllContractsAsync(int pageNumber, int pageSize);
        Task<ContractResponseDto> GetContractByIdAsync(string id);
        Task<ContractResponseDto> GetContractByOrderIdAsync(string orderBookingId);
        Task<ContractResponseDto> GetContractByNumberAsync(string contractNumber);
        Task CreateContractAsync(ContractRequestDto request);
        Task UpdateContractAsync(string id, ContractRequestDto request);
        Task DeleteContractAsync(string id);
    }
}
