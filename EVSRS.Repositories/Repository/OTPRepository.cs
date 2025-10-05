using EVSRS.BusinessObjects.DBContext;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EVSRS.Repositories.Repository;

public class OTPRepository : GenericRepository<OTP>, IOTPRepository
{
    public OTPRepository(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : base(context,
        httpContextAccessor)
    {
    }

    public async Task<OTP?> GetOTPAsync(string userId, string otp, OTPType? otpType)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(x => x.UserId == userId);
        }

        if (!string.IsNullOrEmpty(otp))
        {
            query = query.Where(x => x.Code == otp);
        }

        if (otpType != null)
        {
            query = query.Where(x => x.OTPType == otpType);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task CreateOTPAsync(OTP otp)
    {
        await InsertAsync(otp);
    }

    public async Task DeleteOTPAsync(OTP otp)
    {
        await DeleteAsync(otp);
    }
}