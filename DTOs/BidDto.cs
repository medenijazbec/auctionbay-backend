namespace auctionbay_backend.DTOs
{
    public class BidDto
    {
        public decimal Amount { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string UserName { get; set; } = string.Empty;           
        public string? ProfilePictureUrl { get; set; }                 
    }
}
