using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EVSRS.Repositories.Repository;

public class UserRepository : GenericRepository<ApplicationUser>, IUserRepository
{
    public UserRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context,
        httpContextAccessor)
    {
    }

    public async Task<PaginatedList<ApplicationUser>> GetUsersAsync()
    {
        var response = await _dbSet.Where(x => !x.IsDeleted).ToListAsync();
        var paginatedList = PaginatedList<ApplicationUser>.Create(response, 1, response.Count);
        return paginatedList;
    }

    public async Task<List<ApplicationUser>> GetAllUserAsync()
    {
        var respone = await _dbSet.ToListAsync();
        return respone;
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string id)
    {
        var response = await _dbSet.Where(x => !x.IsDeleted && x.Id == id).FirstOrDefaultAsync();
        return response;
    }

    public async Task<ApplicationUser?> GetUserByUsernameAsync(string username)
    {
        var response = await _dbSet.Where(x => !x.IsDeleted && x.UserName == username)
            .FirstOrDefaultAsync();
        return response;
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        var response = await _dbSet.Where(x => !x.IsDeleted && x.UserEmail == email)
            .FirstOrDefaultAsync();
        return response;
    }

    public async Task<ApplicationUser?> GetByEmailPhoneAsync(string email, string phoneNumber)
    {
        var response = await _dbSet
            .Where(x => !x.IsDeleted && x.UserEmail == email && x.PhoneNumber == phoneNumber)
            .FirstOrDefaultAsync();
        return response;
    }

    public async Task<ApplicationUser?> GetByPhoneAsync(string phoneNumber)
    {
        var response = await _dbSet
            .Where(x => !x.IsDeleted && x.PhoneNumber == phoneNumber)
            .FirstOrDefaultAsync();
        return response;
    }


    public async Task CreateUserAsync(ApplicationUser user)
    {
        await InsertAsync(user);
    }

    public async Task UpdateUserAsync(ApplicationUser user)
    {
        await UpdateAsync(user);
    }

    public async Task<PaginatedList<ApplicationUser>> GetStaffByDepotIdAsync(string depotId, int pageNumber, int pageSize)
    {
        var query = _context.Users
            .Include(u => u.Depot)
            .Where(u => u.DepotId == depotId && u.Role == Role.STAFF && !u.IsDeleted)
            .OrderBy(u => u.FullName);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedList<ApplicationUser>(items, totalCount, pageNumber, pageSize);
    }

    public async Task DeleteUserAsync(ApplicationUser user)
    {
        await DeleteAsync(user);
    }
}