using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class SettlementItem : BaseEntity
    {
        public string? ReturnSettlementId { get; set; }
        public string? FeeIncurred { get; set; }
        public string? Description { get; set; }
        public string? Discount { get; set; }
        public string? Total { get; set; }
        public string? Notes { get; set; }

        public ReturnSettlement? ReturnSettlement { get; set; }
    }
}
