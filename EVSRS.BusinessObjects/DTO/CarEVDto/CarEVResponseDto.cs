using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVSRS.BusinessObjects.DTO.DepotDto;
using EVSRS.BusinessObjects.DTO.ModelDto;

namespace EVSRS.BusinessObjects.DTO.CarEVDto
    {
    public class CarEVResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public ModelResponseDto Model { get; set; } 
        public DepotResponseDto Depot { get; set; } 
        public string OdoMeter { get; set; } = string.Empty;
        public string BatteryHealthPercentage { get; set; } = string.Empty;
        
        public string Status { get; set; } 
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } 
        public string UpdatedBy { get; set; }
        public bool isDeleted { get; set; }

    }
}
