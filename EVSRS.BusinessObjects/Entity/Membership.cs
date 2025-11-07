using EVSRS.BusinessObjects.Base;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.Entity
{
    public class Membership : BaseEntity
    {
        // FK tới ApplicationUser.Id (1:1)
        public string UserId { get; set; } = null!;

        // FK tới MembershipConfig
        public string MembershipConfigId { get; set; } = null!;

        // Tổng tiền các OrderBooking đã Complete của user
        public decimal TotalOrderBill { get; set; } = 0m;

        // Navigation
        public virtual ApplicationUser? User { get; set; }
        public virtual MembershipConfig? MembershipConfig { get; set; }
    }
}
