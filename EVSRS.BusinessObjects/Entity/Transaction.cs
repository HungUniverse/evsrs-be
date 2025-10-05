using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class Transaction: BaseEntity
    {
        public string? OrderBookingId { get; set; }
        public string? UserId { get; set; }
        public string? SepayId { get; set; }
        public string? Gateway { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public String? Code { get; set; }
        public string? Content { get; set; }
        public string? TransferType { get; set; }
        public string? TranferAmount { get; set; }
        public string? Accumulated { get; set; }
        public string? SubAccount { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Description { get; set; }

        public ApplicationUser? User { get; set; }
        public OrderBooking? OrderBooking { get; set; } 
    }
}
