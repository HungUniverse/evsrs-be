using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.MembershipDto
{
    public class MembershipResponseDto
    {
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public MembershipLevel Level { get; set; }
        public string LevelName { get; set; } = null!;
        public decimal DiscountPercent { get; set; }
        public decimal RequiredAmount { get; set; }
        public decimal TotalOrderBill { get; set; }
        public decimal? ProgressToNextLevel { get; set; }
        public decimal? AmountToNextLevel { get; set; }
        public string? NextLevelName { get; set; }
        public string MembershipConfigId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
