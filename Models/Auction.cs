using System;
using System.Collections.Generic;

namespace auctionbay_backend.Models
{
    public class Auction
    {
        public int AuctionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal StartingPrice { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string AuctionState { get; set; } = "Active";  // Active, Closed, Cancelled, etc.
        public string CreatedBy { get; set; } = string.Empty;   //references ApplicationUser.Id
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string MainImageUrl { get; set; } = string.Empty;
        //store the cropped thumbnail URL
        public string ThumbnailUrl { get; set; } = string.Empty;

        //property to store image binary data in the database.
        public byte[]? MainImageData { get; set; }

        //Navigation: an auction can have many bids.
        public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    }
}
