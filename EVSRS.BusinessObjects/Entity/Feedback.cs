using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class Feedback: BaseEntity
    {
        public string? UserId { get; set; }
        public string? OrderBookingId { get; set; }
        public string? Images { get; set; }
        public string? Description { get; set; }
        public string? Rated { get; set; }

        public ApplicationUser? User { get; set; }
        public OrderBooking? OrderBooking { get; set; }

    }
}
