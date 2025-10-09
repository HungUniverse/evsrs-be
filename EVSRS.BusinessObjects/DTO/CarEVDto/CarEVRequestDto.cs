using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.CarEVDto
{
    public class CarEVRequestDto
    {
        public string ModelId { get; set; } = string.Empty;
        public string DepotId { get; set; } = string.Empty;
        public string OdoMeter { get; set; } = string.Empty;
        public string BatteryHealthPercentage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
