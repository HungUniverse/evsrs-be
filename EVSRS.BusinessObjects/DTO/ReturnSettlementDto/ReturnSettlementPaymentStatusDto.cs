using System;

namespace EVSRS.BusinessObjects.DTO.ReturnSettlementDto;

public class ReturnSettlementPaymentStatusDto
{
    public string Id { get; set; } = string.Empty;
    public string OrderBookingId { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = "PENDING"; // PENDING, PAID, FAILED
    public string? PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Total { get; set; }
    public bool IsPaymentRequired { get; set; }
    public bool IsPaymentOverdue { get; set; }
    public DateTime? PaymentDueDate { get; set; }
    public string? QrCodeUrl { get; set; } // SePay QR if payment is pending
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Additional order information for context
    public string? OrderCode { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
}