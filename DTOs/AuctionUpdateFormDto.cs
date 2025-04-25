using System;
using Microsoft.AspNetCore.Http;

namespace auctionbay_backend.DTOs
{
    public class AuctionUpdateFormDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal StartingPrice { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string ExistingImageUrl { get; set; } = string.Empty;   // sent by the UI
        public IFormFile? Image { get; set; }
    }
}
