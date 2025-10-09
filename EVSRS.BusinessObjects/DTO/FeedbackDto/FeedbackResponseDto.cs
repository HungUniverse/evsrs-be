using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.FeedbackDto
{
    public class FeedbackResponseDto
    {
        public string Id { get; set; } = default!;
        public string OrderBookingId { get; set; } = default!;
                
        public string UserId { get; set; } = default!;
      
        public string Rated { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
