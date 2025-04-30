using System;
using System.ComponentModel.DataAnnotations;

namespace auctionbay_backend.DTOs.Admin
{
    public class AdminAuctionUpdateDto
    {
        [Required] public string Title { get; set; } = null!;
        [Required] public string Description { get; set; } = null!;
        [Required] public decimal StartingPrice { get; set; }
        [Required] public DateTime StartDateTime { get; set; }
        [Required] public DateTime EndDateTime { get; set; }
        public string? MainImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
    }
}
