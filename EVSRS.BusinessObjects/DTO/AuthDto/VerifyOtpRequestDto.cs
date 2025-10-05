using System;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.AuthDto;

public class VerifyOtpRequestDto
{
    public string Email { get; set; }
    public string Code { get; set; }
    public OTPType OTPType { get; set; }
}
