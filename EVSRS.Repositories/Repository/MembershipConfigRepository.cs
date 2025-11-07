using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EVSRS.Repositories.Repository
{
    public class MembershipConfigRepository : GenericRepository<MembershipConfig>, IMembershipConfigRepository
    {
        public MembershipConfigRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) 
            : base(context, httpContextAccessor)
        {
        }

        public async Task<List<MembershipConfig>> GetAllMembershipConfigsAsync()
        {
            return await _dbSet
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.RequiredAmount)
                .ToListAsync();
        }

        public async Task<MembershipConfig?> GetMembershipConfigByIdAsync(string id)
        {
            return await _dbSet
                .Where(x => !x.IsDeleted && x.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<MembershipConfig?> GetMembershipConfigByLevelAsync(MembershipLevel level)
        {
            return await _dbSet
                .Where(x => !x.IsDeleted && x.Level == level)
                .FirstOrDefaultAsync();
        }

        public async Task<MembershipConfig?> GetMembershipConfigForAmountAsync(decimal totalAmount)
        {
            // Lấy config cao nhất mà user đủ điều kiện (RequiredAmount <= totalAmount)
            return await _dbSet
                .Where(x => !x.IsDeleted && x.RequiredAmount <= totalAmount)
                .OrderByDescending(x => x.RequiredAmount)
                .FirstOrDefaultAsync();
        }

        public async Task CreateMembershipConfigAsync(MembershipConfig config)
        {
            await InsertAsync(config);
        }

        public async Task UpdateMembershipConfigAsync(MembershipConfig config)
        {
            await UpdateAsync(config);
        }

        public async Task DeleteMembershipConfigAsync(MembershipConfig config)
        {
            await DeleteAsync(config);
        }

        public async Task<bool> IsLevelExistsAsync(MembershipLevel level)
        {
            return await _dbSet
                .AnyAsync(x => !x.IsDeleted && x.Level == level);
        }
    }
}
