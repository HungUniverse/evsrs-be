using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EVSRS.Repositories.Repository
{
    public class MembershipRepository : GenericRepository<Membership>, IMembershipRepository
    {
        public MembershipRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) 
            : base(context, httpContextAccessor)
        {
        }

        public async Task<Membership?> GetByUserIdAsync(string userId)
        {
            return await _dbSet
                .Include(m => m.MembershipConfig)
                .Where(m => !m.IsDeleted && m.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Membership>> GetMembershipsByConfigIdAsync(string membershipConfigId)
        {
            return await _dbSet
                .Include(m => m.User)
                .Include(m => m.MembershipConfig)
                .Where(m => !m.IsDeleted && m.MembershipConfigId == membershipConfigId)
                .ToListAsync();
        }

        public async Task CreateMembershipAsync(Membership membership)
        {
            await InsertAsync(membership);
        }

        public async Task UpdateMembershipAsync(Membership membership)
        {
            await UpdateAsync(membership);
        }

        public async Task DeleteMembershipAsync(Membership membership)
        {
            await DeleteAsync(membership);
        }
    }
}
