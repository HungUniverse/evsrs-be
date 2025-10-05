using System;

namespace EVSRS.BusinessObjects.DTO.AuthDto;

public class LogoutRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
