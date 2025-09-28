using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class InspectionDamageItem: BaseEntity
    {
        public string? HandoverInspectionId { get; set; }
        public string? AreaCar { get; set; }
        public string? Severity { get; set; }
        public string? Description { get; set; }
        public string? Images { get; set; }

    }
}
