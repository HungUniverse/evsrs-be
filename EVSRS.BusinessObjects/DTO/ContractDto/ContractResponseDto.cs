using System;
using EVSRS.BusinessObjects.DTO.OrderBookingDto;
using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.ContractDto;

public class ContractResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string OrderBookingId { get; set; } = string.Empty;
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime SignedDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? FileUrl { get; set; }
    public string? SignatureUrl { get; set; }
    public SignStatus SignStatus { get; set; }
    
    // Navigation properties
    public UserResponseDto? User { get; set; }
    public OrderBookingResponseDto? OrderBooking { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}