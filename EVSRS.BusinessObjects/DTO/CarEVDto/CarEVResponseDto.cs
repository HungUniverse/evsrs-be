using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.CarEVDto
{
    public class CarEVResponseDto
    {
        public string Id { get; set; }
        public string ModelId { get; set; } 
        public string DepotId { get; set; } 
        public string OdoMeter { get; set; } 
        public string BatteryHealthPercentage { get; set; }
        
        public string Status { get; set; } 
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } 
        public string UpdatedBy { get; set; }
        public bool isDeleted { get; set; }

    }
}
