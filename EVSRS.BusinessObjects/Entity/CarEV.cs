using EVSRS.BusinessObjects.Base;
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
        public string? OdoMeter { get; set; }
        public string? BatteryhealthPercentage { get; set; }
        public string? Status { get; set; }

        public Model? Model { get; set; }
        public Depot? Depot { get; set; }
        public virtual ICollection<OrderBooking> OrderBookings { get; set; } = new List<OrderBooking>();


    }
}
