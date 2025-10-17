using EVSRS.BusinessObjects.Base;
using EVSRS.BusinessObjects.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class CarEV: BaseEntity
    {
        public string? ModelId { get; set; }
        public string? DepotId { get; set; }
        public string? LicensePlate { get; set; }
        public string? BatteryHealthPercentage { get; set; }
        public CarEvStatus Status { get; set; }
        public Model? Model { get; set; }
        public Depot? Depot { get; set; }
        public virtual ICollection<OrderBooking> OrderBookings { get; set; } = new List<OrderBooking>();

    }
}
