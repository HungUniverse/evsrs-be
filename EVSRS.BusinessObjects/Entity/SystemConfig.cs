using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.Entity
{
    public class SystemConfig : BaseEntity
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
        public ConfigType ConfigType { get; set; }
    }
}