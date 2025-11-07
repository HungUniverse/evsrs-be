using System.ComponentModel.DataAnnotations;

namespace EVSRS.BusinessObjects.DTO.MembershipDto
{
    public class UpdateMembershipConfigDto
    {
        [Range(0, 100, ErrorMessage = "DiscountPercent must be between 0 and 100")]
        public decimal? DiscountPercent { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "RequiredAmount must be greater than or equal to 0")]
        public decimal? RequiredAmount { get; set; }
    }
}
