using AutoMapper;
using EVSRS.BusinessObjects.DTO.ContractDto;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;
using EVSRS.BusinessObjects.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Service
{
    /// <summary>
    /// Service quản lý hợp đồng (contract): tạo, lưu trữ mẫu, và truy vấn hợp đồng liên quan đến booking.
    /// </summary>
    public class ContractService : IContractService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidationService _validationService;

        public ContractService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _validationService = validationService;
        }

        public async Task CreateContractAsync(ContractRequestDto request)
        {
            await _validationService.ValidateAndThrowAsync(request);
            var contract = _mapper.Map<Contract>(request);
            await _unitOfWork.ContractRepository.InsertAsync(contract);
            await _unitOfWork.SaveChangesAsync();

        }

        public async Task DeleteContractAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var existingContract = await _unitOfWork.ContractRepository.GetByIdAsync(id);
            if (existingContract == null)
            {
                throw new KeyNotFoundException("Contract not found");
            }
            await _unitOfWork.ContractRepository.DeleteAsync(existingContract);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PaginatedList<ContractResponseDto>> GetAllContractsAsync(int pageNumber, int pageSize)
        {
            var contracts = await _unitOfWork.ContractRepository.GetAllContractsAsync();
            var contractDtos = contracts.Items.Select(c => _mapper.Map<ContractResponseDto>(c)).ToList();
            return PaginatedList<ContractResponseDto>.Create(contractDtos, pageNumber, pageSize);
        }

        public async Task<ContractResponseDto> GetContractByIdAsync(string id)
        {
            var contract = await _unitOfWork.ContractRepository.GetByIdAsync(id);
            if (contract == null)
            {
                throw new KeyNotFoundException("Contract not found");
            }
            var contractDto = _mapper.Map<ContractResponseDto>(contract);
            return contractDto;
        }

        public async Task<ContractResponseDto> GetContractByNumberAsync(string contractNumber)
        {
            var contract = await _unitOfWork.ContractRepository.GetContractByNumberAsync(contractNumber);
            if (contract == null)
            {
                throw new KeyNotFoundException("Contract not found");
            }
            var contractDto = _mapper.Map<ContractResponseDto>(contract);
            return contractDto;
        }

        public async Task<ContractResponseDto> GetContractByOrderIdAsync(string orderBookingId)
        {
            var contract = await _unitOfWork.ContractRepository.GetContractByOrderIdAsync(orderBookingId);
            if (contract == null)
            {
                throw new KeyNotFoundException("Contract not found");
            }
            var contractDto = _mapper.Map<ContractResponseDto>(contract);
            return contractDto;
        }

        public async Task UpdateContractAsync(string id, ContractRequestDto request)
        {
            var existingContract = await _unitOfWork.ContractRepository.GetByIdAsync(id);
            if (existingContract == null)
            {
                throw new KeyNotFoundException("Contract not found");
            }
            await _validationService.ValidateAndThrowAsync(request);
            existingContract.UpdatedAt = DateTime.UtcNow;
            existingContract.UpdatedBy = GetCurrentUserName();
            _mapper.Map(request, existingContract);
            await _unitOfWork.ContractRepository.UpdateAsync(existingContract);
            await _unitOfWork.SaveChangesAsync();
        }

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }
    }
}
