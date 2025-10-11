using System;
using EVSRS.BusinessObjects.DTO.OrderBookingDto;
using EVSRS.BusinessObjects.DTO.UserDto;

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
    public string SignStatus { get; set; } = string.Empty;
    
    // Navigation properties
    public UserResponseDto? User { get; set; }
    public OrderBookingResponseDto? OrderBooking { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}