using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.UserDto
{
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
        public bool IsVerify { get; set; } = false;
    }
}
