using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class Model: BaseEntity
    {
        public string? ManufacturerCarId { get; set; }
        public string? AmenitiesId { get; set; }
        public string? ModelName { get; set; }
        public string? BatteryCapacityKwh { get; set; }
        public string? RangeKm { get; set; }
        public string? LimiteDailyKm { get; set; }
        public string? Seats { get; set; }
        public string? Image { get; set; }
        public double? Price { get; set; }
        public int? Sale { get; set; }

        

        public CarManufacture? CarManufacture { get; set; }
        public Amenities? Amenities { get; set; }
        public virtual ICollection<CarEV> CarEVDetails { get; set; } = new List<CarEV>();
    }
}
