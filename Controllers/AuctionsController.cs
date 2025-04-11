using System;
using System.Threading.Tasks;
using auctionbay_backend.DTOs;
using auctionbay_backend.Models;
using auctionbay_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace auctionbay_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuctionsController : ControllerBase
    {
        private readonly IAuctionService _auctionService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuctionsController(IAuctionService auctionService, UserManager<ApplicationUser> userManager)
        {
            _auctionService = auctionService;
            _userManager = userManager;
        }

        // POST: api/Auctions/{id}/bid
        [HttpPost("{id}/bid")]
        [Authorize]
        public async Task<IActionResult> PlaceBid([FromRoute] int id, [FromBody] BidDto dto)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
                return Unauthorized();

            try
            {
                var bid = await _auctionService.PlaceBidAsync(user.Id, id, dto);
                return Ok(bid);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/Auctions
        [HttpGet]
        public async Task<IActionResult> GetActiveAuctions()
        {
            var auctions = await _auctionService.GetActiveAuctionsAsync();
            return Ok(auctions);
        }
    }
}
