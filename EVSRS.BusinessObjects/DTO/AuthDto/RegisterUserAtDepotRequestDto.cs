using System;

namespace EVSRS.BusinessObjects.DTO.AuthDto;

public class RegisterUserAtDepotRequestDto
{
    public string UserEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? FrontImage { get; set; }
    public string? BackImage { get; set; }
    public string? CountryCode { get; set; }
    public string? NumberMasked { get; set; }
    public string? LicenseClass { get; set; }
    public DateTime? ExpireAt { get; set; }
}