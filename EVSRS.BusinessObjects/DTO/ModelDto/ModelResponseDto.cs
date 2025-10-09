using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.ModelDto
{
    public class ModelResponseDto
    {
        public string Id { get; set; }
        public string ModelName { get; set; }
        public string BatteryCapacityKwh { get; set; }
        public string RangeKm { get; set; }
        public string LimiteDailyKm { get; set; }
        public string ManufacturerCarId { get; set; }
        public string AmenitiesId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
