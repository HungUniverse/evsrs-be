using AutoMapper;
using EVSRS.BusinessObjects.DTO.TransactionDto;
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
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidationService _validationService;

        public TransactionService(IUnitOfWork unitOfWork, IMapper mapper, IHttpContextAccessor httpContextAccessor, IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _validationService = validationService;
        }

        public async Task CreateTransactionAsync(TransactionRequestDto transaction)
        {
            await _validationService.ValidateAndThrowAsync(transaction);
            var transactionEntity = _mapper.Map<Transaction>(transaction);
            await _unitOfWork.TransactionRepository.CreateTransactionAsync(transactionEntity);
            await _unitOfWork.SaveChangesAsync();


        }

        public async Task DeleteTransactionAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var transaction = await _unitOfWork.TransactionRepository.GetTransactionByIdAsync(id);
            if (transaction == null)
            {
                throw new KeyNotFoundException($"Transaction with id {id} not found.");
            }
            await _unitOfWork.TransactionRepository.DeleteTransactionAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

        }

        public async Task<PaginatedList<TransactionResponseDto>> GetAllTransactionsAsync(int pageNumber, int pageSize)
        {
            var transactions = await _unitOfWork.TransactionRepository.GetTransactionList();
            var transactionDtos = transactions.Items.Select(t => _mapper.Map<TransactionResponseDto>(t)).ToList();
            var paginatedList = PaginatedList<TransactionResponseDto>.Create(transactionDtos, pageNumber, pageSize);
            return paginatedList;


        }

      

        public async Task<TransactionResponseDto> GetTransactionByIdAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            var transaction = await _unitOfWork.TransactionRepository.GetTransactionByIdAsync(id);
            if (transaction == null)
            {
                throw new KeyNotFoundException($"Transaction with id {id} not found.");
            }
            var transactionDto = _mapper.Map<TransactionResponseDto>(transaction);
            return transactionDto;
        }

        public async Task<List<TransactionResponseDto>> GetTransactionsByOrderIdAsync(string orderId)
        {
            var transactions = await _unitOfWork.TransactionRepository.GetTransactionsByOrderIdAsync(orderId);
            var transactionDtos = transactions.Select(t => _mapper.Map<TransactionResponseDto>(t)).ToList();
            return transactionDtos;
        }

        public async Task<List<TransactionResponseDto>> GetTransactionsByUserIdAsync(string userId)
        {
            var transactions = await _unitOfWork.TransactionRepository.GetTransactionsByUserIdAsync(userId);
            var transactionDtos = transactions.Select(t => _mapper.Map<TransactionResponseDto>(t)).ToList();
            return transactionDtos;
        }

        public async Task UpdateTransactionAsync(string id, TransactionRequestDto transactionRequestDto)
        {
            var existingTransaction = await _unitOfWork.TransactionRepository.GetTransactionByIdAsync(id);
            if (existingTransaction == null)
            {
                throw new KeyNotFoundException($"Transaction with id {id} not found.");
            }
            await _validationService.ValidateAndThrowAsync(transactionRequestDto);
            existingTransaction.UpdatedBy = GetCurrentUserName();
            existingTransaction.UpdatedAt = DateTime.UtcNow;
            _mapper.Map(transactionRequestDto, existingTransaction);
            await _unitOfWork.TransactionRepository.UpdateTransactionAsync(existingTransaction);
            await _unitOfWork.SaveChangesAsync();


        }
        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }
    }
}
