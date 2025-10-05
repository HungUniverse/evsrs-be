using EVSRS.BusinessObjects.Base;
using EVSRS.BusinessObjects.Enum;
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
        public string? UserEmail { get; set; }
        public string? HashPassword { get; set; }
        public string? Salt { get; set; }
        public string? FullName { get; set; }
        public String? ProfilePicture { get; set; }
        public string? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsVerify { get; set; } = false;
        
        public virtual ICollection<OTP>? OTPs { get; set; } = new List<OTP>();
        public Role? Role { get; set; }
        public virtual ICollection<ApplicationUserToken>? UserTokens { get; set; } = new List<ApplicationUserToken>();
        public virtual ICollection<Notification>? Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<Feedback>? Feedbacks { get; set; } = new List<Feedback>();
        public virtual ICollection<IdentifyDocument> IdentifyDocuments { get; set; } = new List<IdentifyDocument>();
        public virtual ICollection<OrderBooking>? OrderBookings { get; set; } = [];


    }
}
