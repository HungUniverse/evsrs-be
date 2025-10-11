using System.ComponentModel.DataAnnotations;

namespace EVSRS.BusinessObjects.DTO.ContractDto;

public class ContractRequestDto
{
    [Required(ErrorMessage = "User ID is required")]
    public string UserId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Order booking ID is required")]
    public string OrderBookingId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Contract number is required")]
    public string ContractNumber { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }
    
    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }
    
    public string? FileUrl { get; set; }
    
    public string SignStatus { get; set; } = "PENDING"; // PENDING, SIGNED, CANCELLED
}