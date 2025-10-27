using System.ComponentModel.DataAnnotations;

namespace EVSRS.BusinessObjects.DTO.ReturnSettlementDto;

public class ReturnSettlementPaymentRequestDto
{
    [Required(ErrorMessage = "Return settlement ID is required")]
    public string ReturnSettlementId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Payment method is required")]
    public string PaymentMethod { get; set; } = string.Empty; // "CASH", "SEPAY", "CARD"
    
    public string? Notes { get; set; }
}