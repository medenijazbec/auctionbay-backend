using System.Collections.Generic;
using System.Threading.Tasks;
using auctionbay_backend.DTOs;
using auctionbay_backend.Models;

namespace auctionbay_backend.Services
{
    public interface IAuctionService
    {
        Task<Auction> CreateAuctionAsync(string userId, AuctionCreateDto dto);
        Task<Auction> UpdateAuctionAsync(string userId, int auctionId, AuctionUpdateDto dto);
        Task<Bid> PlaceBidAsync(string userId, int auctionId, BidDto dto);
        Task<IEnumerable<Auction>> GetActiveAuctionsAsync();
    }
}
