using System.Collections.Generic;
using System.Threading.Tasks;
using auctionbay_backend.DTOs;

namespace auctionbay_backend.Services
{
    public interface IAuctionService
    {
        Task<AuctionResponseDto> CreateAuctionAsync(string userId, AuctionCreateDto dto);
        Task<AuctionResponseDto> UpdateAuctionAsync(string userId, int auctionId, AuctionUpdateDto dto);
        Task<BidDto> PlaceBidAsync(string userId, int auctionId, BidDto dto);

        //Updated method signature for paginated GET
        Task<IEnumerable<AuctionResponseDto>> GetActiveAuctionsAsync(int page, int pageSize);

        Task<IEnumerable<AuctionResponseDto>> GetAuctionsByUserAsync(string userId);
    }
}
