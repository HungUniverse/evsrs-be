using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class OTP: BaseEntity
    {
        public string? UserId { get; set; }
        public string? Code { get; set; }
        public string? OTPType { get; set; }
        public DateTime? ExpireAt { get; set; }

        public ApplicationUser? User { get; set; }
    }
}
