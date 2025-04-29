using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using auctionbay_backend.Data;
using auctionbay_backend.DTOs;
using auctionbay_backend.Models;
using Microsoft.EntityFrameworkCore;
using auctionbay_backend.Controllers;
using auctionbay_backend.Services;
using Google.Protobuf.WellKnownTypes;
using static Google.Protobuf.WellKnownTypes.Field.Types;

namespace auctionbay_backend.Services
{
    public class AuctionService : IAuctionService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly INotificationService _notif;
        public AuctionService(ApplicationDbContext dbContext, INotificationService notificationService)
        {
            _dbContext = dbContext;
            _notif = notificationService;
        }
        private static string CalculateState(
            Auction a, string? userId)
        {
            if (a.EndDateTime <= DateTime.UtcNow)
                return "done";

            if (string.IsNullOrEmpty(userId))
                return "inProgress";

            var bidsByUser = a.Bids.Where(b => b.UserId == userId);
            if (!bidsByUser.Any()) return "inProgress";

            var highest = a.Bids.OrderByDescending(b => b.Amount)
                                .FirstOrDefault();
            return highest?.UserId == userId ? "winning" : "outbid";
        }

        private static AuctionResponseDto ToDto(
            Auction a, string? userId)
        {
            var state = CalculateState(a, userId);
            return new AuctionResponseDto
            {
                AuctionId = a.AuctionId,
                Title = a.Title,
                Description = a.Description,
                StartingPrice = a.StartingPrice,
                StartDateTime = a.StartDateTime,
                EndDateTime = a.EndDateTime,
                AuctionState = state,
                CreatedBy = a.CreatedBy,
                CreatedAt = a.CreatedAt,
                MainImageUrl = a.MainImageUrl,
                ThumbnailUrl = a.ThumbnailUrl,
                CurrentHighestBid = a.Bids.Any()
                                    ? a.Bids.Max(b => b.Amount)
                                    : a.StartingPrice,
                TimeLeft = a.EndDateTime > DateTime.UtcNow
                                    ? a.EndDateTime - DateTime.UtcNow
                                    : TimeSpan.Zero,
                /* light list keeps bids only for owner cards */
                Bids = a.Bids.Select(b => new BidDto
                { Amount = b.Amount }).ToList()
            };
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
                ThumbnailUrl = dto.ThumbnailUrl,
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


            //  — notify the previous top bidder that they've been outbid
            if (highestBid != null && highestBid.UserId != userId)
            {
            await _notif.CreateAsync(new Notification
            {
                UserId = highestBid.UserId,
                AuctionId = auctionId,
                Kind = "outbid",
                Title = auction.Title,
                Timestamp = DateTime.UtcNow
                });
            }



            return new BidDto { Amount = bid.Amount };
        }
        //could also hook here into auction-completion logic and call _notif.CreateAsync
        //willdo if first impl doesnt wrk






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
                ThumbnailUrl = auction.ThumbnailUrl,
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

        public async Task<AuctionResponseDto?>GetAuctionDetailAsync(int id, string? userId = null)
        {
            var a = await _dbContext.Auctions
                             .Include(x => x.Bids)
                             .ThenInclude(b => b.User)
                             .FirstOrDefaultAsync(x => x.AuctionId == id);

            if (a is null) return null;

            /* richer DTO with bidder names + pics */
            var dto = ToDto(a, userId);
            dto.Bids = a.Bids.OrderByDescending(b => b.CreatedDateTime)
                             .Select(b => new BidDto
                             {
                                 Amount = b.Amount,
                                 CreatedDateTime = b.CreatedDateTime,
                                 UserName = b.User.FirstName + " " +
                                                     b.User.LastName,
                                 ProfilePictureUrl = b.User.ProfilePictureUrl
                             }).ToList();
            return dto;
        }

        public async Task<IEnumerable<AuctionResponseDto>>
          GetActiveAuctionsAsync(int page, int pageSize, string? userId = null)
        {
            // 24 hours ago cutoff for “done” cards
            var cutoff = DateTime.UtcNow.AddHours(-24);

            var list = await _dbContext.Auctions
                .Include(a => a.Bids)
                .Where(a =>
                    // still open:
                    (a.AuctionState == "Active" && a.EndDateTime > DateTime.UtcNow)
                    ||
                    // OR closed within last 24 h AND user has a bid on it
                    (userId != null
                     && a.EndDateTime <= DateTime.UtcNow
                     && a.EndDateTime > cutoff
                     && a.Bids.Any(b => b.UserId == userId)))
                .OrderBy(a => a.EndDateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return list.Select(a => ToDto(a, userId));
        }

        public async Task DeleteAuctionAsync(string userId, int auctionId)
        {
            var auction = await _dbContext.Auctions
                                          .FirstOrDefaultAsync(a => a.AuctionId == auctionId);
            if (auction is null)
                throw new Exception("Auction not found.");
            if (auction.CreatedBy != userId)
                throw new Exception("You are not authorised to delete this auction.");

            _dbContext.Auctions.Remove(auction);
            await _dbContext.SaveChangesAsync();
        }
        public async Task<IEnumerable<AuctionResponseDto>> GetAuctionsBiddingAsync(string userId)
        {
            var list = await _dbContext.Auctions
                .Include(a => a.Bids)
                .Where(a =>
                       a.CreatedBy != userId &&                    // not my own auction
                       a.Bids.Any(b => b.UserId == userId))        // I have at least one bid
                .OrderBy(a => a.EndDateTime)
                .ToListAsync();

            return list.Select(a => ToDto(a, userId));
        }

        public async Task<IEnumerable<AuctionResponseDto>> GetAuctionsWonAsync(string userId)
        {
            var list = await _dbContext.Auctions
                .Include(a => a.Bids)
                .Where(a =>
                       a.EndDateTime <= DateTime.UtcNow &&         // finished
                       a.Bids.Any() &&                             // there was at least one bid
                       a.Bids.OrderByDescending(b => b.Amount)
                             .First().UserId == userId)            // my bid is the highest
                .OrderByDescending(a => a.EndDateTime)
                .ToListAsync();

            return list.Select(a => ToDto(a, userId));
        }


    }
}
