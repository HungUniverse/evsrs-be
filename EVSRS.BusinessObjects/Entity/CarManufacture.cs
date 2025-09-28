using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class CarManufacture: BaseEntity
    {
        public string? Name { get; set; }
        public string? Logo { get; set; }

        public virtual ICollection<CarEV> CarEVs { get; set; } = new List<CarEV>(); 
    }
}
