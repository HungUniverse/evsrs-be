using System.ComponentModel.DataAnnotations;

namespace EVSRS.BusinessObjects.DTO.ReturnSettlementDto;

public class SettlementItemRequestDto
{
    [Required(ErrorMessage = "Fee incurred is required")]
    public string FeeIncurred { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Description is required")]
    public string Description { get; set; } = string.Empty;
    
    public string? Discount { get; set; }
    
    [Required(ErrorMessage = "Total amount is required")]
    public string Total { get; set; } = string.Empty;
    
    public string? Notes { get; set; }
    
    public string? Image { get; set; }
}