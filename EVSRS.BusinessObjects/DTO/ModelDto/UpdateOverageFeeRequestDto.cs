using System.ComponentModel.DataAnnotations;

namespace EVSRS.BusinessObjects.DTO.ModelDto
{
    public class UpdateOverageFeeRequestDto
    {
        [Required(ErrorMessage = "OverageFee is required")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "OverageFee must be a valid decimal number")]
        public string OverageFee { get; set; } = string.Empty;
    }
}