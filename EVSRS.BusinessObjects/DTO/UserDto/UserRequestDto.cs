using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVSRS.BusinessObjects.DTO.UserDto.Validation;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.UserDto
{
    [DepotIdRequiredForStaff]
    public class UserRequestDto
    {
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? HashPassword { get; set; }
        public string? Salt { get; set; }
        public string? FullName { get; set; }
        public string? ProfilePicture { get; set; }
        public string? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        
        // DepotId is required only for STAFF role
        public string? DepotId { get; set; }
        
        public Role? Role { get; set; }
        public bool IsVerify { get; set; } = false;
    }
}
