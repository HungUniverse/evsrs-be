using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EVSRS.Repositories.Interface
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<PaginatedList<Transaction>> GetTransactionList();
        Task<Transaction?> GetTransactionByIdAsync(string id);
        Task<Transaction?> GetTransactionByBookingIdAsync(string bookingId);
        Task<Transaction?> GetTransactionByUserIdAsync(string userId);
        Task CreateTransactionAsync(Transaction transaction);
        Task UpdateTransactionAsync(Transaction transaction);
        Task DeleteTransactionAsync(Transaction transaction);
        Task<Transaction?> GetLatestTransactionByOrderIdAsync(string orderId);

    }
}
