using System;
using System.ComponentModel.DataAnnotations;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.OrderBookingDto
{
    public class OrderBookingRequestDto
    {
        /// <summary>
        /// ID của xe cụ thể (nếu khách chọn xe cụ thể).
        /// Bỏ trống nếu dùng ModelId để hệ thống tự chọn xe available.
        /// </summary>
        public string? CarEVDetailId { get; set; }
        
        /// <summary>
        /// ID của model xe (nếu khách chỉ chọn mẫu xe, không chọn xe cụ thể).
        /// Hệ thống sẽ tự động tìm xe available của model này tại depot.
        /// </summary>
        public string? ModelId { get; set; }
        
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