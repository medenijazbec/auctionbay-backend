using System;

namespace auctionbay_backend.DTOs
{
    public class AuctionCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal StartingPrice { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string MainImageUrl { get; set; } = string.Empty;
    }
}
