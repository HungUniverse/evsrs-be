namespace EVSRS.BusinessObjects.DTO.MembershipDto
{
    public class MembershipResponseDto
    {
        public string Id { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public MembershipConfigResponseDto Config { get; set; } = null!;
        public decimal TotalOrderBill { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
