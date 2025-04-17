// DTOs/AuctionCreateFormDto.cs
using System;
using Microsoft.AspNetCore.Http;

namespace auctionbay_backend.DTOs
{
    public class AuctionCreateFormDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal StartingPrice { get; set; }
        public DateTime EndDateTime { get; set; }
        public IFormFile? Image { get; set; }
    }
}
