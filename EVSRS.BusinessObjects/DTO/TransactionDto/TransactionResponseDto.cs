using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.DTO.TransactionDto
{
    public class TransactionResponseDto
    {
        public string Id { get; set; }
        public string? OrderBookingId { get; set; }
        public string? UserId { get; set; }
        public string? SepayId { get; set; }
        public string? Gateway { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? Code { get; set; }
        public string? Content { get; set; }
        public string? TransferType { get; set; }
        public string? TranferAmount { get; set; }
        public string? Accumulated { get; set; }
        public string? SubAccount { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public bool isDeleted { get; set; }
    }
}
