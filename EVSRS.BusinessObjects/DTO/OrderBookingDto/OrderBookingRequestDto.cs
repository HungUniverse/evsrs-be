using System;
using System.ComponentModel.DataAnnotations;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.OrderBookingDto
{
    public class OrderBookingRequestDto
    {
        [Required]
        public string CarEVDetailId { get; set; } = string.Empty;
        
        [Required]
        public string DepotId { get; set; } = string.Empty;
        
        [Required]
        public DateTime StartAt { get; set; }
        
        [Required]
        public DateTime EndAt { get; set; }
        
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BANKING;
        
        public PaymentType PaymentType { get; set; } = PaymentType.DEPOSIT;
        
        public string? Note { get; set; }

        // For offline booking at depot
        public bool IsOfflineBooking { get; set; } = false;
        
        // Customer info for offline booking
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerAddress { get; set; }
    }
}