using System;

namespace auctionbay_backend.Models
{
    public class Bid
    {
        public int BidId { get; set; }
        public int AuctionId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime CreatedDateTime { get; set; } = DateTime.UtcNow;

        //navigation properties 
        public Auction Auction { get; set; }
        public ApplicationUser User { get; set; }
    }
}
