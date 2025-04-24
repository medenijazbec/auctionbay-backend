using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using auctionbay_backend.Data;
using auctionbay_backend.DTOs;
using auctionbay_backend.Models;
using Microsoft.EntityFrameworkCore;
using auctionbay_backend.Controllers;


namespace auctionbay_backend.Services
{
    public class AuctionService : IAuctionService
    {
        private readonly ApplicationDbContext _dbContext;
        public AuctionService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AuctionResponseDto> CreateAuctionAsync(string userId, AuctionCreateDto dto)
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
            return MapAuctionToResponseDto(auction);
        }

        public async Task<AuctionResponseDto> UpdateAuctionAsync(string userId, int auctionId, AuctionUpdateDto dto)
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
            return MapAuctionToResponseDto(auction);
        }

        public async Task<BidDto> PlaceBidAsync(string userId, int auctionId, BidDto dto)
        {
            var auction = await _dbContext.Auctions.FirstOrDefaultAsync(a => a.AuctionId == auctionId);
            if (auction == null)
            {
                throw new Exception("Auction not found.");
            }

            //Enforce bidding rules: the bid must be at least 1 unit higher than the current highest bid,
            //or at least equal to the starting price if no bids exist.
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
            return new BidDto { Amount = bid.Amount };
        }

        //Updated GetActiveAuctionsAsync method with pagination
        public async Task<IEnumerable<AuctionResponseDto>> GetActiveAuctionsAsync(int page, int pageSize)
        {
            var auctions = await _dbContext.Auctions
                .Include(a => a.Bids)
                .Where(a => a.AuctionState == "Active" && a.EndDateTime > DateTime.UtcNow)
                .OrderBy(a => a.EndDateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return auctions.Select(a => MapAuctionToResponseDto(a));
        }

        public async Task<IEnumerable<AuctionResponseDto>> GetAuctionsByUserAsync(string userId)
        {
            var auctions = await _dbContext.Auctions
                .Include(a => a.Bids)
                .Where(a => a.CreatedBy == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return auctions.Select(a => MapAuctionToResponseDto(a));
        }

        private AuctionResponseDto MapAuctionToResponseDto(Auction auction)
        {
            return new AuctionResponseDto
            {
                AuctionId = auction.AuctionId,
                Title = auction.Title,
                Description = auction.Description,
                StartingPrice = auction.StartingPrice,
                StartDateTime = auction.StartDateTime,
                EndDateTime = auction.EndDateTime,
                AuctionState = auction.AuctionState,
                CreatedBy = auction.CreatedBy,
                CreatedAt = auction.CreatedAt,
                MainImageUrl = auction.MainImageUrl,
                CurrentHighestBid = auction.Bids.Any() ? auction.Bids.Max(b => b.Amount) : auction.StartingPrice,
                TimeLeft = auction.EndDateTime > DateTime.UtcNow ? auction.EndDateTime - DateTime.UtcNow : TimeSpan.Zero,
                Bids = auction.Bids.Select(b => new BidDto { Amount = b.Amount }).ToList()
            };
        }

        //detail mapper that user info:
        private AuctionResponseDto MapAuctionToDetailDto(Auction auction)
        {
            return new AuctionResponseDto
            {
                AuctionId = auction.AuctionId,
                Title = auction.Title,
                Description = auction.Description,
                StartingPrice = auction.StartingPrice,
                StartDateTime = auction.StartDateTime,
                EndDateTime = auction.EndDateTime,
                AuctionState = auction.AuctionState,
                CreatedBy = auction.CreatedBy,
                CreatedAt = auction.CreatedAt,
                MainImageUrl = auction.MainImageUrl,
                CurrentHighestBid = auction.Bids.Any() ? auction.Bids.Max(b => b.Amount) : auction.StartingPrice,
                TimeLeft = auction.EndDateTime > DateTime.UtcNow
                                          ? auction.EndDateTime - DateTime.UtcNow
                                          : TimeSpan.Zero,

                //full bid info
                Bids = auction.Bids
                                          .OrderByDescending(b => b.CreatedDateTime)
                                          .Select(b => new BidDto
                                          {
                                              Amount = b.Amount,
                                              CreatedDateTime = b.CreatedDateTime,
                                              UserName = b.User.FirstName + " " + b.User.LastName,
                                              ProfilePictureUrl = b.User.ProfilePictureUrl
                                          })
                                          .ToList()
            };
        }
        public async Task<AuctionResponseDto?> GetAuctionDetailAsync(int auctionId)
        {
            var a = await _dbContext.Auctions
                                    .Include(x => x.Bids)
                                    .ThenInclude(b => b.User)
                                    .FirstOrDefaultAsync(x => x.AuctionId == auctionId);
            if (a == null) return null;
            return MapAuctionToDetailDto(a);
        }


        //Services/AuctionService.cs   (add GetAuctionAsync)
        public async Task<AuctionResponseDto?> GetAuctionAsync(int auctionId)
        {
            var a = await _dbContext.Auctions
                                    .Include(x => x.Bids)
                                    .FirstOrDefaultAsync(x => x.AuctionId == auctionId);
            return a is null ? null : MapAuctionToResponseDto(a);
        }


    }
}
