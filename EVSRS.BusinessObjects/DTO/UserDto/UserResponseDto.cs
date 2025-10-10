using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.UserDto;

public class UserResponseDto
{
    public string Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FullName { get; set; }
    public Role? Role { get; set; }
    public bool IsVerify { get; set; } 
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
    public bool isDeleted { get; set; }
}