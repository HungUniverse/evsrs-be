
using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EVSRS.Repositories.Repository
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context, httpContextAccessor)
        {
        }
        public async Task CreateTransactionAsync(Transaction transaction)
        {
            await InsertAsync(transaction);
        }

        public async Task DeleteTransactionAsync(Transaction transaction)
        {
            await DeleteAsync(transaction);
        }

        public async Task<Transaction?> GetTransactionByBookingIdAsync(string bookingId)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.OrderBookingId == bookingId).FirstOrDefaultAsync();
            return response;
        }

        public async Task<Transaction?> GetTransactionByIdAsync(string id)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.Id == id).FirstOrDefaultAsync();
            return response;
        }

        public async Task<Transaction?> GetTransactionByUserIdAsync(string userId)
        {
            var response = await _dbSet.Where(x => !x.IsDeleted && x.UserId == userId).FirstOrDefaultAsync();
            return response;
        }

        public async Task<Transaction?> GetLatestTransactionByOrderIdAsync(string orderId)
        {
            var response = await _dbSet
                .Where(x => !x.IsDeleted && x.OrderBookingId == orderId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
            return response;
        }

        public async Task<PaginatedList<Transaction>> GetTransactionList()
        {
            var respone = await _dbSet.ToListAsync();
            return PaginatedList<Transaction>.Create(respone, 1, respone.Count);
        }

        public async Task UpdateTransactionAsync(Transaction transaction)
        {
            await UpdateAsync(transaction);
        }
    }
}
