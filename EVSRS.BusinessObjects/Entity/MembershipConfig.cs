using EVSRS.BusinessObjects.Base;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.Entity
{
    public class MembershipConfig : BaseEntity
    {
        // Level này áp dụng cho hạng nào
        public MembershipLevel Level { get; set; }

        // % giảm giá cho hạng này
        public decimal DiscountPercent { get; set; }

        // Ngưỡng tiền cần đạt để lên hạng này
        public decimal RequiredAmount { get; set; }

        // Navigation - danh sách user có hạng này
        public virtual ICollection<Membership>? Memberships { get; set; }
    }
}
