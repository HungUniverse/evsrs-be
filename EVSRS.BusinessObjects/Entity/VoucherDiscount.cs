using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class VoucherDiscount: BaseEntity
    {
        public string? VoucherBatchId { get; set; }
        public string? UserId { get; set; }
        public string? Code { get; set; }
        public string? status { get; set; }

        public ApplicationUser? User { get; set; }
        public VoucherBatch? VoucherBatch { get; set; }

    }
}
