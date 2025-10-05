using System;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;

namespace EVSRS.Repositories.Interface;

public interface ITokenRepository : IGenericRepository<ApplicationUserToken>
{
    Task<ApplicationUserToken?> GetTokenByUserIdAsync(string userId, TokenType tokenType);
    Task<ApplicationUserToken?> GetTokenAsync(string accessToken, TokenType tokenType);
    Task CreateTokenAsync(ApplicationUserToken userToken);
    Task UpdateTokenAsync(ApplicationUserToken userToken);
    Task<bool> DeleteTokenAsync(string userId, string token, TokenType tokenType);
}
