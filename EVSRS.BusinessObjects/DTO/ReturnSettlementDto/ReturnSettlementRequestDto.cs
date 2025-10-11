using System.ComponentModel.DataAnnotations;

namespace EVSRS.BusinessObjects.DTO.ReturnSettlementDto;

public class ReturnSettlementRequestDto
{
    [Required(ErrorMessage = "Order booking ID is required")]
    public string OrderBookingId { get; set; } = string.Empty;
    
    public string? Subtotal { get; set; }
    
    public string? Discount { get; set; }
    
    [Required(ErrorMessage = "Total amount is required")]
    public string Total { get; set; } = string.Empty;
    
    public string? Notes { get; set; }
    
    public List<SettlementItemRequestDto> SettlementItems { get; set; } = new List<SettlementItemRequestDto>();
}