using System;
namespace EVSRS.BusinessObjects.DTO.AuthDto;
public class RegisterUserAtDepotResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}