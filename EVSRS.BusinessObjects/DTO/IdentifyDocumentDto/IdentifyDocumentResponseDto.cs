using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.IdentifyDocumentDto
{
    public class IdentifyDocumentResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public UserResponseDto User { get; set; }
        public string? FrontImage { get; set; } = string.Empty;
        public string? BackImage { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string NumberMasked { get; set; } = string.Empty;
        public string LicenseClass { get; set; } = string.Empty;
        public DateTime ExpireAt { get; set; }
        public IdentifyDocumentStatus Status { get; set; }
        public string VerifiedBy { get; set; }
        public DateTime VerifiedAt { get; set; }
        public string Note { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public bool isDeleted { get; set; }
    }
}
