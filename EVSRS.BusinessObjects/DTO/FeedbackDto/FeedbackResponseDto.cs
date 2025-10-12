using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.FeedbackDto
{
    public class FeedbackResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string OrderBookingId { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string Rated { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Image { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool isDeleted { get; set; }
    }
}
