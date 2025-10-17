using EVSRS.BusinessObjects.DTO.DepotDto;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.UserDto;

public class UserResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DateOfBirth { get; set; }
    public string? FullName { get; set; }
    public string? DepotId { get; set; } // For staff members
    public DepotResponseDto? Depot { get; set; } // Navigation property
    public Role? Role { get; set; }
    public bool IsVerify { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
    public bool isDeleted { get; set; }
}