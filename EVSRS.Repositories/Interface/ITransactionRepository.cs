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
        Task <List<Transaction>> GetTransactionsByUserIdAsync(string userId);
        Task<List<Transaction>> GetTransactionsByOrderIdAsync(string orderId);
        Task CreateTransactionAsync(Transaction transaction);
        Task UpdateTransactionAsync(Transaction transaction);
        Task DeleteTransactionAsync(Transaction transaction);
        

    }
}
