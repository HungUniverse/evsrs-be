using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVSRS.BusinessObjects.Enum;

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
        public OrderBookingStatus? Status { get; set; }
        public string? SubTotal { get; set; }
        public string? Discount { get; set; }
        public string? DepositAmount { get; set; }
        public string ? TotalAmount { get; set; }
        public string? RemainingAmount { get; set; }
        public PaymentStatus? PaymentMethod { get; set; }
        public PaymentType? PaymentType { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public OrderType? Type { get; set; }
        public string? ShippingFee { get; set; }
        public string? RemainingBalance { get; set; }
        public string? Code { get; set; }
        public string? Note { get; set; }
        public string? RefundAmount { get; set; }

        public ApplicationUser? User { get; set; }
        public virtual ICollection<Feedback>? Feedbacks { get; set; } =  [];
        public virtual ICollection<Transaction>? Transactions { get; set; } = [];
        public virtual ICollection<HandoverInspection> HandoverInspections { get; set; } = new List<HandoverInspection>();
        public virtual ICollection<Contract>? Contracts { get; set; } = [];
        public ReturnSettlement? ReturnSettlement { get; set; }
        public CarEV? CarEvs { get; set; }
        public Depot? Depot { get; set; }
    }
}
