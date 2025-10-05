using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;

namespace EVSRS.Repositories.Interface;

public interface IOTPRepository : IGenericRepository<OTP>
{
    Task<OTP?> GetOTPAsync(string userId, string otp, OTPType? otpType);
    Task CreateOTPAsync(OTP otp);
    Task DeleteOTPAsync(OTP otp);
}