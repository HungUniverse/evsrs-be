using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class ApplicationUser: BaseEntity   
    {
       
    
        public string? UserName { get; set; }
        public string? HashPassword { get; set; }
        public string? Salt { get; set; }
        public string? FullName { get; set; }
        public String? ProfilePicture { get; set; }
        public string? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public bool isVerify { get; set; } = false;
        
        public virtual ICollection<OTP>? OTPs { get; set; } = new List<OTP>();
        public ApplicationRole? Role { get; set; }

    }
}
