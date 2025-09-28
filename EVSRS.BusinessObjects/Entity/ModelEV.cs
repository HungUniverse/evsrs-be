using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class ModelEV: BaseEntity
    {
        public string? ManufacturerCarId { get; set; }
        public string? ModelName { get; set; }
        public string? BatteryCapacityKwh { get; set; }
        public string? RangeKm { get; set; }
        public string? Seats { get; set; }
        public double? Price { get; set; }

    }
}
