using System;

namespace EVSRS.BusinessObjects.DTO.HandoverInspectionDto;

public class HandoverInspectionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string OrderBookingId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string BatteryPercent { get; set; } = string.Empty;
    public string Odometer { get; set; } = string.Empty;
    public string? Images { get; set; }
    public string? Notes { get; set; }
    public decimal? ReturnLateFee { get; set; } = 0m;
    public string StaffId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}