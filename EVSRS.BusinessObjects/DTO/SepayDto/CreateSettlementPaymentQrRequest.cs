namespace EVSRS.BusinessObjects.DTO.SepayDto;

public class CreateSettlementPaymentQrRequest
{
    public string SettlementId { get; set; } = string.Empty;
    public string SettlementCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string OrderBookingId { get; set; } = string.Empty;
    public string? UserId { get; set; }
}