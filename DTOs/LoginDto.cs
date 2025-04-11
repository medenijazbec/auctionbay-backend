namespace auctionbay_backend.DTOs
{
    public class LoginDto
    {
        // Matching your login page: "Email", "Password"
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
