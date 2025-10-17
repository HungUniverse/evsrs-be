using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class Depot: BaseEntity
    {
        public string? Name { get; set; }
        public string? MapId { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? Street { get; set; }
        public string? Lattitude { get; set; }
        public string? Longitude { get; set; }
        public string? OpenTime { get; set; }
        public string? CloseTime { get; set; }

        public virtual ICollection<CarEV> Details { get; set; } = new List<CarEV>();
        public virtual ICollection<OrderBooking> OrderBookings { get; set; } =  new List<OrderBooking>();
        public virtual ICollection<ApplicationUser> ApplicationUsers { get; set; } = new List<ApplicationUser>();
    }
}
