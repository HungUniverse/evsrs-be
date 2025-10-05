using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.Entity
{
    public class OTP: BaseEntity
    {
        public string? UserId { get; set; }
        public string? Code { get; set; }
        public OTPType OTPType { get; set; }
        public DateTime? ExpireAt { get; set; }

        public ApplicationUser? User { get; set; }
    }
}
