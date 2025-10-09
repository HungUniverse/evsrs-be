using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.ModelDto
{
    public class ModelRequestDto
    {
        public string ModelName { get; set; } = string.Empty;
        public string LimiteDailyKm { get; set; } = string.Empty;
        public string RangeKm { get; set; } = string.Empty;
        public string? Seats { get; set; }
        public double? Price { get; set; }
        public string BatteryCapacityKwh { get; set; } = string.Empty;
    }
}
