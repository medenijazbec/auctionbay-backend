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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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

        /* ───────── helpers ───────── */
        private Task<ApplicationUser?> CurrentUserAsync()
        {
            var id = User.FindFirstValue("id") ??
                     User.FindFirstValue(ClaimTypes.NameIdentifier);
            return id is null
                ? Task.FromResult<ApplicationUser?>(null)
                : _um.FindByIdAsync(id);
        }


        /* ───────────────────────────────────────────────── */
        /*  LIST ( card DTOs )                              */
        /*  GET /api/Auctions?page=&pageSize=               */
        /* ───────────────────────────────────────────────── */
        [HttpGet]
        public async Task<IActionResult> Active(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 9)
        {
            var user = await CurrentUserAsync();
            var rows = await _svc.GetActiveAuctionsAsync(
                           page, pageSize, user?.Id);
            return Ok(rows);
        }

        /* ───────────────────────────────────────────────── */
        /*  DETAIL ( full DTO )                             */
        /* ───────────────────────────────────────────────── */
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var user = await CurrentUserAsync();
            var dto = await _svc.GetAuctionDetailAsync(id, user?.Id);
            return dto is null ? NotFound() : Ok(dto);
        }



        /* ───────── CREATE AUCTION ───────── */
        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] AuctionCreateFormDto form)
        {



                        var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();


               const long MAX_BYTES = 10 * 1024 * 1024;
            var allowed = new[] { "image/jpeg", "image/png", "image/gif" };

            var imagesFolder = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(imagesFolder);

            /* 1. store image (optional) */
            var imgUrl = string.Empty;
            string thumbnailUrl = "";
            if (form.Image is { Length: > 0 })
            {
                //size/type checks ---
                if (form.Image.Length > MAX_BYTES || !allowed.Contains(form.Image.ContentType))
                return BadRequest(new { error = "Invalid image upload." });
                //end checks ---

                                var folder = Path.Combine(_env.WebRootPath, "images");
                Directory.CreateDirectory(folder);

                var name = $"{Guid.NewGuid():N}{Path.GetExtension(form.Image.FileName)}";
                await using var fs = System.IO.File.Create(Path.Combine(folder, name));
                await form.Image.CopyToAsync(fs);
                imgUrl = $"/images/{name}";
            }

            // handle the thumbnail upload the same way
                if (form.Thumbnail is { Length: > 0 })
                {
                var thumbName = $"{Guid.NewGuid():N}{Path.GetExtension(form.Thumbnail.FileName)}";
                await using var ts = System.IO.File.Create(Path.Combine(imagesFolder, thumbName));
                await form.Thumbnail.CopyToAsync(ts);
                thumbnailUrl = $"/images/{thumbName}";
                }

            /* 2. forward to service */
            var dto = new AuctionCreateDto
            {
                Title = form.Title,
                Description = form.Description,
                StartingPrice = form.StartingPrice,
                StartDateTime = DateTime.UtcNow,
                EndDateTime = form.EndDateTime,
                MainImageUrl = imgUrl,
                ThumbnailUrl = thumbnailUrl
            };

            var created = await _svc.CreateAuctionAsync(user.Id, dto);

            // point Location header to the *detail* endpoint
            return CreatedAtAction(nameof(GetDetail),
                                   new { id = created.AuctionId },
                                   created);
        }

        /* ───────── PLACE BID ───────── */
        [HttpPost("{id:int}/bid")]
        [Authorize]
        public async Task<IActionResult> Bid(int id, [FromBody] BidDto dto)
        {
            var user = await CurrentUserAsync();
            if (user is null) return Unauthorized();

            var res = await _svc.PlaceBidAsync(user.Id, id, dto);
            return Ok(res);
        }


    }
}
