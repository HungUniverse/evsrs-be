using System;
using EVSRS.BusinessObjects.DTO.CarEVDto;
using EVSRS.BusinessObjects.DTO.DepotDto;
using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.OrderBookingDto;

public class OrderBookingResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public UserResponseDto? User { get; set; }
    public string DepotId { get; set; } = string.Empty;
    public DepotResponseDto? Depot { get; set; }
    public string CarEVDetailId { get; set; } = string.Empty;
    public CarEVResponseDto? CarEvs { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public DateTime? CheckOutedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public OrderBookingStatus Status { get; set; }
    public string? Code { get; set; } 
    public string? SubTotal { get; set; }
    public string? Discount { get; set; }
    public string? DepositAmount { get; set; }
    public string? TotalAmount { get; set; }
    public string? RemainingAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentType PaymentType { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public string? Note { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
