using System.Security.Claims;
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
    [Authorize]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuctionService _auctionService;

        public ProfileController(UserManager<ApplicationUser> userManager,
                                 IAuctionService auctionService)
        {
            _userManager = userManager;
            _auctionService = auctionService;
        }

        /* ─────────────────── helpers ─────────────────── */
        private async Task<ApplicationUser?> CurrentUserAsync()
        {
            // 1) first try the custom "id" claim we issued when logging‑in
            var id = User.FindFirstValue("id");

            // 2) fall back to the standard NameIdentifier if you ever add it
            id ??= User.FindFirstValue(ClaimTypes.NameIdentifier);

            return id is null ? null : await _userManager.FindByIdAsync(id);
        }

        //  GET api/Profile/me
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.ProfilePictureUrl
            });
        }


        //  PUT api/Profile/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            /* avoid e‑mail duplicates */
            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _userManager.FindByEmailAsync(dto.Email);
                if (exists is not null) return Conflict(new { error = "E‑mail already taken." });
            }

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;
            user.UserName = dto.Email;                 // keep Identity happy

            if (!string.IsNullOrWhiteSpace(dto.ProfilePictureUrl))
                user.ProfilePictureUrl = dto.ProfilePictureUrl;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.ProfilePictureUrl
            });
        }


        //  PUT api/Profile/update-password
        [HttpPut("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            if (dto.NewPassword != dto.ConfirmNewPassword)
                return BadRequest(new { error = "Password mismatch." });

            var res = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!res.Succeeded) return BadRequest(res.Errors);

            return Ok(new { message = "Password updated." });
        }


        //  GET api/Profile/auctions
        [HttpGet("auctions")]
        public async Task<IActionResult> GetMyAuctions()
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            var auctions = await _auctionService.GetAuctionsByUserAsync(user.Id);
            return Ok(auctions);
        }


        //  POST api/Profile/auction
        [HttpPost("auction")]
        public async Task<IActionResult> CreateAuction([FromBody] AuctionCreateDto dto)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            var auction = await _auctionService.CreateAuctionAsync(user.Id, dto);
            return Ok(auction);
        }



        //  PUT api/Profile/auction/{id}
        [HttpPut("auction/{id}")]
        public async Task<IActionResult> UpdateAuction(int id, [FromBody] AuctionUpdateDto dto)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            try
            {
                var auction = await _auctionService.UpdateAuctionAsync(user.Id, id, dto);
                return Ok(auction);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
