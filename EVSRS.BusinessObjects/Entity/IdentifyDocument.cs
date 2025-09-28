﻿using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class IdentifyDocument:BaseEntity
    {
        public string? UserId { get; set; }
        public string? Type { get; set; }
        public string? CountryCode { get; set; }
        public string? NumberMasked { get; set; }
        public string? LicenseClass { get; set; }
        public DateTime? ExpireAt { get; set; }
        public string? Status { get; set; }
        public DateTime? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? Note { get; set; }



        public ApplicationUser? User { get; set; }
    }
}
