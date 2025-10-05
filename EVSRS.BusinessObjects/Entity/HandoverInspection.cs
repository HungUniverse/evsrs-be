using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class HandoverInspection: BaseEntity
    {
        public string? OrderBookingId { get; set; }
        public string? Type { get; set; }
        public string? BatteryPercent { get; set; }
        public string? Odometer { get; set; }
        public string? Images { get; set; }
        public string? Notes { get; set; }
        public string? StaffId { get; set; }

        public OrderBooking? OrderBooking { get; set; }
        // public virtual ICollection<InspectionDamageItem> InspectionDamageItems { get; set; } = new List<InspectionDamageItem>();
    }
}
