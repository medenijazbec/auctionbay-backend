using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using auctionbay_backend.Data;
using auctionbay_backend.DTOs;
using auctionbay_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace auctionbay_backend.Services
{
    public class AuctionService : IAuctionService
    {
        private readonly ApplicationDbContext _dbContext;
        public AuctionService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Create a new auction by the specified user.
        public async Task<Auction> CreateAuctionAsync(string userId, AuctionCreateDto dto)
        {
            var auction = new Auction
            {
                Title = dto.Title,
                Description = dto.Description,
                StartingPrice = dto.StartingPrice,
                StartDateTime = dto.StartDateTime,
                EndDateTime = dto.EndDateTime,
                MainImageUrl = dto.MainImageUrl,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                AuctionState = "Active"
            };

            _dbContext.Auctions.Add(auction);
            await _dbContext.SaveChangesAsync();
            return auction;
        }

        // Update an auction – only if the auction was created by the given user.
        public async Task<Auction> UpdateAuctionAsync(string userId, int auctionId, AuctionUpdateDto dto)
        {
            var auction = await _dbContext.Auctions.FirstOrDefaultAsync(a => a.AuctionId == auctionId);
            if (auction == null)
            {
                throw new Exception("Auction not found.");
            }

            if (auction.CreatedBy != userId)
            {
                throw new Exception("You are not authorized to update this auction.");
            }

            auction.Title = dto.Title;
            auction.Description = dto.Description;
            auction.StartingPrice = dto.StartingPrice;
            auction.StartDateTime = dto.StartDateTime;
            auction.EndDateTime = dto.EndDateTime;
            auction.MainImageUrl = dto.MainImageUrl;
            auction.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return auction;
        }

        // Place a bid on an auction.
        public async Task<Bid> PlaceBidAsync(string userId, int auctionId, BidDto dto)
        {
            var auction = await _dbContext.Auctions.FirstOrDefaultAsync(a => a.AuctionId == auctionId);
            if (auction == null)
            {
                throw new Exception("Auction not found.");
            }

            // Optionally: enforce bidding rules (e.g., bid must be higher than current highest bid).
            var highestBid = await _dbContext.Bids
                .Where(b => b.AuctionId == auctionId)
                .OrderByDescending(b => b.Amount)
                .FirstOrDefaultAsync();

            decimal minimumBid = highestBid != null ? highestBid.Amount + 1 : auction.StartingPrice;
            if (dto.Amount < minimumBid)
            {
                throw new Exception($"Bid amount must be at least {minimumBid}.");
            }

            var bid = new Bid
            {
                AuctionId = auctionId,
                UserId = userId,
                Amount = dto.Amount,
                CreatedDateTime = DateTime.UtcNow
            };

            _dbContext.Bids.Add(bid);
            await _dbContext.SaveChangesAsync();
            return bid;
        }

        // Get active auctions ordered by EndDateTime ascending.
        public async Task<IEnumerable<Auction>> GetActiveAuctionsAsync()
        {
            return await _dbContext.Auctions
                .Where(a => a.AuctionState == "Active" && a.EndDateTime > DateTime.UtcNow)
                .OrderBy(a => a.EndDateTime)
                .ToListAsync();
        }
    }
}
