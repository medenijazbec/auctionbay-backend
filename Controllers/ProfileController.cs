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



        //GET  api/Profile/me
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);
            if (user == null)
                return NotFound(new { error = "User not found." });

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.ProfilePictureUrl
            });
        }


         //PUT  api/Profile/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);
            if (user == null)
                return Unauthorized();

            /* unique‑e‑mail validation */
            if (!string.Equals(user.Email, dto.Email, System.StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _userManager.FindByEmailAsync(dto.Email);
                if (exists != null)
                    return Conflict(new { error = "E‑mail address is already taken." });
            }

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;
            user.UserName = dto.Email;          // keep Identity happy
            if (dto.ProfilePictureUrl != null)
                user.ProfilePictureUrl = dto.ProfilePictureUrl;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.ProfilePictureUrl
            });
        }


        //PUT  api/Profile/update-password
        [HttpPut("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);
            if (user == null)
                return Unauthorized();

            if (dto.NewPassword != dto.ConfirmNewPassword)
                return BadRequest(new { error = "Password mismatch." });

            var res = await _userManager.ChangePasswordAsync(user,
                                                             dto.CurrentPassword,
                                                             dto.NewPassword);
            if (!res.Succeeded)
                return BadRequest(res.Errors);

            return Ok(new { Message = "Password updated successfully." });
        }


         //GET  api/Profile/auctions   (owner’s auctions)
        [HttpGet("auctions")]
        public async Task<IActionResult> GetMyAuctions()
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);
            if (user == null)
                return Unauthorized();

            var auctions = await _auctionService.GetAuctionsByUserAsync(user.Id);
            return Ok(auctions);
        }


         //POST api/Profile/auction     (create)
        [HttpPost("auction")]
        public async Task<IActionResult> CreateAuction([FromBody] AuctionCreateDto dto)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);
            if (user == null)
                return Unauthorized();

            var auction = await _auctionService.CreateAuctionAsync(user.Id, dto);
            return Ok(auction);
        }


        //PUT  api/Profile/auction/{id}  (update)
        [HttpPut("auction/{id}")]
        public async Task<IActionResult> UpdateAuction(int id,
                                                       [FromBody] AuctionUpdateDto dto)
        {
            var user = await _userManager.FindByNameAsync(User.Identity!.Name);
            if (user == null)
                return Unauthorized();

            try
            {
                var auction = await _auctionService.UpdateAuctionAsync(user.Id, id, dto);
                return Ok(auction);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
