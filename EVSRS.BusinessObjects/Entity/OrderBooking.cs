using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class OrderBooking: BaseEntity
    {
        public string? UserId { get; set; }
        public string? DepotId { get; set; }
        public string? CarEVDetailId { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public DateTime? CheckOutedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string? status { get; set; }
        public string? SubTotal { get; set; }
        public string? Discount { get; set; }
        public string? DepositAmount { get; set; }
        public string ? TotalAmount { get; set; }
        public string? RemainingAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentType { get; set; }
        public string? PaymentStatus { get; set; }
        public string? Note { get; set; }
    }
}
