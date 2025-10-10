using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.DepotDto
{
    public class DepotRequestDto
    {
        public string Name { get; set; }
        public string MapId { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string Street { get; set; }
        public string Lattitude { get; set; }
        public string Longitude { get; set; }
        public string OpenTime { get; set; }
        public string CloseTime { get; set; }
    }
}
