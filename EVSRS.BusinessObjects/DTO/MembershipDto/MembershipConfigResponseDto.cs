using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.MembershipDto
{
    public class MembershipConfigResponseDto
    {
        public string Id { get; set; } = null!;
        public MembershipLevel Level { get; set; }
        public string LevelName { get; set; } = null!;
        public decimal DiscountPercent { get; set; }
        public decimal RequiredAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
