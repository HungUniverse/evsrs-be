using System;
using EVSRS.BusinessObjects.Base;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.Entity;

public class ApplicationUserToken : BaseEntity
{
    public string UserId { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public bool? IsRevoked { get; set; }
    public TokenType? TokenType { get; set; }

    // Navigation properties
    public ApplicationUser? User { get; set; }
}
