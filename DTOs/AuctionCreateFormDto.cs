// DTOs/AuctionCreateFormDto.cs
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace auctionbay_backend.DTOs
{
    public class AuctionCreateFormDto
    {
        [Required, StringLength(100, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(2000, MinimumLength = 20)]
        public string Description { get; set; } = string.Empty;

        [Required, Range(0.01, 1_000_000)]
        public decimal StartingPrice { get; set; }

        [Required]
        [FutureDate(ErrorMessage = "End date must be in the future.")]
        public DateTime EndDateTime { get; set; }

        public IFormFile? Image { get; set; }
        public IFormFile? Thumbnail { get; set; }
    }
}
