using System;
using System.Collections.Generic;

namespace auctionbay_backend.DTOs
{
    public class AuctionResponseDto
    {
        public int AuctionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal StartingPrice { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string AuctionState { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string MainImageUrl { get; set; } = string.Empty;
        public decimal CurrentHighestBid { get; set; }
        public TimeSpan TimeLeft { get; set; }
        public List<BidDto> Bids { get; set; } = new List<BidDto>();
    }
}
