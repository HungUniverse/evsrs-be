using System;

namespace EVSRS.BusinessObjects.DTO.AuthDto;

public class GoogleLoginRequestDto
{
    public string JwtToken { get; set; } = null!;
}
