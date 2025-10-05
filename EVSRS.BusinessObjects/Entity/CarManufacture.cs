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

        public virtual ICollection<Model> Models { get; set; } = new List<Model>(); 
    }
}
