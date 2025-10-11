using System.ComponentModel.DataAnnotations;

namespace EVSRS.BusinessObjects.DTO.HandoverInspectionDto;

public class HandoverInspectionRequestDto
{
    [Required(ErrorMessage = "Order booking ID is required")]
    public string OrderBookingId { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Type is required")]
    public string Type { get; set; } = string.Empty; // "HANDOVER" or "RETURN"
    
    [Required(ErrorMessage = "Battery percent is required")]
    [Range(0, 100, ErrorMessage = "Battery percent must be between 0 and 100")]
    public string BatteryPercent { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Odometer reading is required")]
    public string Odometer { get; set; } = string.Empty;
    
    public string? Images { get; set; } // JSON array of image URLs
    
    public string? Notes { get; set; }
    
    [Required(ErrorMessage = "Staff ID is required")]
    public string StaffId { get; set; } = string.Empty;
}