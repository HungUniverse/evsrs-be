using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.DepotDto
{
    public class DepotResponseDto
    {
        public string Id { get; set; }
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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public bool isDeleted { get; set; }
    }
}
