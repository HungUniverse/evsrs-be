using System;

namespace EVSRS.BusinessObjects.DTO.TokenDto;

public class TokenResponseDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}
