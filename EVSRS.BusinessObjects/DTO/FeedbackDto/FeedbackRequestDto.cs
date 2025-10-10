using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.FeedbackDto
{
    public class FeedbackRequestDto
    {
        public string OrderBookingId { get; set; }    // bắt buộc
        public string? Rated { get; set; }                           // 1..5
        public string? Description { get; set; }
        public string? Images { get; set; }
    }
}
