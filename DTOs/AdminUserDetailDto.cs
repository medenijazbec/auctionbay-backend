using System.Collections.Generic;
using auctionbay_backend.DTOs;

namespace auctionbay_backend.DTOs.Admin
{
    public class AdminUserDetailDto : AdminUserListDto
    {
        // all the auctions this user has created
        public List<AuctionResponseDto> Auctions { get; set; } = new();
    }
}
