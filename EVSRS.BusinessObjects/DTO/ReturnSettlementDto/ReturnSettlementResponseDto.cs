using System;

namespace EVSRS.BusinessObjects.DTO.ReturnSettlementDto;

public class ReturnSettlementResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string OrderBookingId { get; set; } = string.Empty;
    public DateTime? CalculateAt { get; set; }
    public string? Subtotal { get; set; }
    public string? Discount { get; set; }
    public string? Total { get; set; }
    public string? Notes { get; set; }
    public string? PaymentStatus { get; set; } = "PENDING";
    public string? PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
    
    public List<SettlementItemResponseDto> SettlementItems { get; set; } = new List<SettlementItemResponseDto>();
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}