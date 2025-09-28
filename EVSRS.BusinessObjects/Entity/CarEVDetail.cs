using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class CarEVDetail: BaseEntity
    {
        public string? ModelEVId { get; set; }
        public string? DepotId { get; set; }
        public string? OdoMeter { get; set; }
        public string? BatteryhealthPercentage { get; set; }
        public string? Status { get; set; }

    }
}
