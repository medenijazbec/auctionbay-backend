// Controllers/AuctionsController.cs
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using auctionbay_backend.DTOs;
using auctionbay_backend.Models;
using auctionbay_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace auctionbay_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuctionsController : ControllerBase
    {
        private readonly IAuctionService _svc;
        private readonly UserManager<ApplicationUser> _um;
        private readonly IWebHostEnvironment _env;

        public AuctionsController(
            IAuctionService svc,
            UserManager<ApplicationUser> um,
            IWebHostEnvironment env)
        {
            _svc = svc;
            _um = um;
            _env = env;
        }

        /*HELPERS*/
        private Task<ApplicationUser?> CurrentUserAsync()
        {
            var id = User.FindFirstValue("id") ??
                     User.FindFirstValue(ClaimTypes.NameIdentifier);
            return id is null ? Task.FromResult<ApplicationUser?>(null)
                              : _um.FindByIdAsync(id);
        }

        /*CREATE AUCTION*/
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] AuctionCreateFormDto form)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            /* 1. store image (optional) */
            var imgUrl = string.Empty;
            if (form.Image is { Length: > 0 })
            {
                var folder = Path.Combine(_env.WebRootPath, "images");
                Directory.CreateDirectory(folder);

                var name = $"{Guid.NewGuid():N}{Path.GetExtension(form.Image.FileName)}";
                await using var fs = System.IO.File.Create(Path.Combine(folder, name));
                await form.Image.CopyToAsync(fs);
                imgUrl = $"/images/{name}";
            }

            /* 2. forward to service */
            var dto = new AuctionCreateDto
            {
                Title = form.Title,
                Description = form.Description,
                StartingPrice = form.StartingPrice,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = form.EndDateTime,
                MainImageUrl = imgUrl
            };

            var created = await _svc.CreateAuctionAsync(user.Id, dto);

            return CreatedAtAction(nameof(GetById), new { id = created.AuctionId }, created);
        }

        /*GET ONE (for CreatedAt)*/
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var a = await _svc.GetAuctionAsync(id);
            return a is null ? NotFound() : Ok(a);
        }

        /*PLACE BID (unchanged)*/
        [HttpPost("{id:int}/bid")]
        [Authorize]
        public async Task<IActionResult> Bid(int id, [FromBody] BidDto dto)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            var res = await _svc.PlaceBidAsync(user.Id, id, dto);
            return Ok(res);
        }

        /*LIST ACTIVE */
        [HttpGet]
        public async Task<IActionResult> Active([FromQuery] int page = 1,
                                                [FromQuery] int pageSize = 9)
            => Ok(await _svc.GetActiveAuctionsAsync(page, pageSize));
    }
}
