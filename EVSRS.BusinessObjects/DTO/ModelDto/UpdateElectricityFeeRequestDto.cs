using System.ComponentModel.DataAnnotations;

namespace EVSRS.BusinessObjects.DTO.ModelDto
{
    public class UpdateElectricityFeeRequestDto
    {
        [Required(ErrorMessage = "ElectricityFee is required")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "ElectricityFee must be a valid decimal number")]
        public string ElectricityFee { get; set; } = string.Empty;
    }
}