using EVSRS.BusinessObjects.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.SystemConfigDto
{
    public class SystemConfigResponseDto
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public ConfigType ConfigType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}