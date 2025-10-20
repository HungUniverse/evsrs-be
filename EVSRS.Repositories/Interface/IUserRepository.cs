using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Infrastructure;

namespace EVSRS.Repositories.Interface;

public interface IUserRepository : IGenericRepository<ApplicationUser>
{
    Task<PaginatedList<ApplicationUser>> GetUsersAsync();
    Task<List<ApplicationUser>> GetAllUserAsync();
    Task<ApplicationUser?> GetUserByIdAsync(string id);
    Task<ApplicationUser?> GetUserByUsernameAsync(string username);
    Task<ApplicationUser?> GetUserByEmailAsync(string email);
    Task<ApplicationUser?> GetByEmailPhoneAsync(string email, string phoneNumber);
    Task<ApplicationUser?> GetByPhoneAsync(string phoneNumber);
    Task<PaginatedList<ApplicationUser>> GetStaffByDepotIdAsync(string depotId, int pageNumber, int pageSize);
    Task CreateUserAsync(ApplicationUser user);
    Task UpdateUserAsync(ApplicationUser user);
    Task UpdateStaffDepotId(string userId, string depotId);
    Task DeleteUserAsync(ApplicationUser user);

}