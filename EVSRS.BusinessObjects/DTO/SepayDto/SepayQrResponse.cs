using System;
using EVSRS.BusinessObjects.DTO.OrderBookingDto;

namespace EVSRS.BusinessObjects.DTO.SepayDto;

public class SepayQrResponse
{
    public string QrUrl { get; set; } = string.Empty;
    public OrderBookingResponseDto? OrderBooking { get; set; }
}
