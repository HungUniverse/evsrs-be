using System;
using EVSRS.BusinessObjects.Base;

namespace EVSRS.BusinessObjects.Entity;

public class Contract: BaseEntity
{
    public string UserId { get; set; }
    public string OrderBookingId { get; set; }
    public string ContractNumber { get; set; }
    public DateTime SignedDate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string FileUrl { get; set; }
    public string SignStatus { get; set; }

    // Navigation properties
    public virtual ApplicationUser Users { get; set; }
    public virtual OrderBooking OrderBooking { get; set; }
}
