using System.ComponentModel.DataAnnotations;

namespace EVSRS.BusinessObjects.DTO.ReturnSettlementDto;

public class CompleteReturnRequestDto
{
    [Required(ErrorMessage = "Order booking ID is required")]
    public string OrderBookingId { get; set; } = string.Empty;
    
    public string? FinalNotes { get; set; }
    
    public string? CompletedByStaffId { get; set; }
}