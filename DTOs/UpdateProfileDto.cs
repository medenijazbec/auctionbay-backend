using System.ComponentModel.DataAnnotations;

namespace auctionbay_backend.DTOs
{
    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        [EmailAddress] public string? Email { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
