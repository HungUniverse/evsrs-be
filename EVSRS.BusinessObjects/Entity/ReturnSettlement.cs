using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class ReturnSettlement : BaseEntity
    {
        public string? OrderBookingId { get; set; }
        public DateTime? CalculateAt { get; set; }
        public string? Subtotal { get; set; }
        public string? Discount { get; set; }
        public string? Total { get; set; }
        public string? Notes { get; set; }

        public OrderBooking? OrderBooking { get; set; }
        public virtual ICollection<SettlementItem> SettlementItems { get; set; } = new List<SettlementItem>();


    }
}
