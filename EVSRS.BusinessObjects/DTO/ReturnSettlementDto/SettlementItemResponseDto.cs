using System;

namespace EVSRS.BusinessObjects.DTO.ReturnSettlementDto;

public class SettlementItemResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string ReturnSettlementId { get; set; } = string.Empty;
    public string FeeIncurred { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Discount { get; set; }
    public string? Total { get; set; }
    public string? Notes { get; set; }
    public string? Image { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}