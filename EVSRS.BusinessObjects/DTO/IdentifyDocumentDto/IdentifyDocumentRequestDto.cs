using EVSRS.BusinessObjects.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.IdentifyDocumentDto
{
    public class IdentifyDocumentRequestDto
    {
        public string UserId { get; set; } = string.Empty;
        public string? FrontImage { get; set; } = string.Empty;
        public string? BackImage { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string NumberMasked { get; set; } = string.Empty;
        public string LicenseClass { get; set; } = string.Empty;
        public DateTime? ExpireAt { get; set; }
        public IdentifyDocumentStatus? Status { get; set; } = IdentifyDocumentStatus.PENDING;
        public string? VerifiedBy { get; set; } = null;
        public DateTime? VerifiedAt { get; set; } = null;
        public string Note { get; set; } = string.Empty;
    }
}
