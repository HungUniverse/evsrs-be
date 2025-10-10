using EVSRS.BusinessObjects.DTO.TransactionDto;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.Services.Interface
{
    public interface ITransactionService
    {
        Task<PaginatedList<TransactionResponseDto>> GetAllTransactionsAsync(int pageNumber, int pageSize);
        Task<TransactionResponseDto> GetTransactionByIdAsync(string id);
        Task<TransactionResponseDto> GetTransactionByBookingIdAsync(string bookingId);
        Task<TransactionResponseDto> GetTransactionByUserIdAsync(string userId);
        Task CreateTransactionAsync(TransactionRequestDto transaction);
        Task UpdateTransactionAsync(string id, TransactionRequestDto transactionRequestDto);
        Task DeleteTransactionAsync(string id);
    }
}
