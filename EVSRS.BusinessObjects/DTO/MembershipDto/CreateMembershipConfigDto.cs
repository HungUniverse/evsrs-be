using System.ComponentModel.DataAnnotations;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.MembershipDto
{
    public class CreateMembershipConfigDto
    {
        [Required(ErrorMessage = "Level is required")]
        public MembershipLevel Level { get; set; }

        [Required(ErrorMessage = "DiscountPercent is required")]
        [Range(0, 100, ErrorMessage = "DiscountPercent must be between 0 and 100")]
        public decimal DiscountPercent { get; set; }

        [Required(ErrorMessage = "RequiredAmount is required")]
        [Range(0, double.MaxValue, ErrorMessage = "RequiredAmount must be greater than or equal to 0")]
        public decimal RequiredAmount { get; set; }
    }
}
