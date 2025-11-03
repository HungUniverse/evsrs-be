using System;
using System.ComponentModel.DataAnnotations;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.OrderBookingDto
{
    public class OrderBookingOfflineRequestDto
    {
        [Required]
        public string CarEVDetailId { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public DateTime StartAt { get; set; }
        
        [Required]
        public DateTime EndAt { get; set; }
        
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BANKING;
                
        public string? Note { get; set; }

    }
}