namespace auctionbay_backend.DTOs
{
    public class RegisterDto
    {
        //matching registration page: "Name", "Surname", "Email", "Password", "Repeat password"
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
