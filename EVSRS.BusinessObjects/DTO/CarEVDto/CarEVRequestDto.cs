using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.CarEVDto
{
    public class CarEVRequestDto
    {
        public string ModelId { get; set; } = string.Empty;
        public string DepotId { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string BatteryHealthPercentage { get; set; } = string.Empty;
        public CarEvStatus Status { get; set; } = CarEvStatus.UNAVAILABLE;
    }
}
