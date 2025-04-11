namespace auctionbay_backend.DTOs
{
    public class ResetPasswordDto
    {
        //retrieved from the link in the reset email
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        //new password and confirmation
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
