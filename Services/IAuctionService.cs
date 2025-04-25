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
        Task<AuctionResponseDto?> GetAuctionAsync(int auctionId);
        Task<AuctionResponseDto?> GetAuctionDetailAsync(int auctionId);

        /*userId is optional – null means “treat as guest”  */
        Task<IEnumerable<AuctionResponseDto>> GetActiveAuctionsAsync(int page, int pageSize, string? userId = null);

        Task<AuctionResponseDto?> GetAuctionDetailAsync(int auctionId, string? userId = null);

        Task DeleteAuctionAsync(string userId, int auctionId);


    }
}
