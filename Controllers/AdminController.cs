using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using auctionbay_backend.Data;
using auctionbay_backend.Models;
using auctionbay_backend.Services;
using auctionbay_backend.DTOs;
using auctionbay_backend.DTOs.Admin;

namespace auctionbay_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly ApplicationDbContext _db;
        private readonly IAuctionService _auctionsSvc;

        public AdminController(
            UserManager<ApplicationUser> users,
            ApplicationDbContext db,
            IAuctionService auctionsSvc)
        {
            _users = users;
            _db = db;
            _auctionsSvc = auctionsSvc;
        }

        // GET api/Admin/users?search=&page=&pageSize=
        [HttpGet("users")]
        public async Task<IActionResult> SearchUsers(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _users.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(search)
                    || u.FirstName.ToLower().Contains(search)
                    || u.LastName.ToLower().Contains(search));
            }

            var total = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new AdminUserListDto
                {
                    Id = u.Id,
                    Email = u.Email!,
                    FirstName = u.FirstName!,
                    LastName = u.LastName!,
                    ProfilePictureUrl = u.ProfilePictureUrl!
                })
                .ToListAsync();

            return Ok(new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = users
            });
        }

        // GET api/Admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserDetail(string id)
        {
            var u = await _users.FindByIdAsync(id);
            if (u == null) return NotFound();

            var dto = new AdminUserDetailDto
            {
                Id = u.Id,
                Email = u.Email!,
                FirstName = u.FirstName!,
                LastName = u.LastName!,
                ProfilePictureUrl = u.ProfilePictureUrl!
            };

            var auctions = await _auctionsSvc.GetAuctionsByUserAsync(u.Id);
            dto.Auctions = auctions.ToList();

            return Ok(dto);
        }

        // PUT api/Admin/users/{id}
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(
            string id,
            [FromBody] AdminUserUpdateDto dto)
        {
            var u = await _users.FindByIdAsync(id);
            if (u == null) return NotFound();

            // email uniqueness check
            if (!string.Equals(u.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                if (await _users.FindByEmailAsync(dto.Email) != null)
                    return Conflict(new { error = "Email already taken." });
            }

            u.FirstName = dto.FirstName;
            u.LastName = dto.LastName;
            u.Email = dto.Email;
            u.UserName = dto.Email;
            if (dto.ProfilePictureUrl != null)
                u.ProfilePictureUrl = dto.ProfilePictureUrl;

            var res = await _users.UpdateAsync(u);
            if (!res.Succeeded)
                return BadRequest(res.Errors);

            return NoContent();
        }

        // DELETE api/Admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var u = await _users.FindByIdAsync(id);
            if (u == null) return NotFound();

            var res = await _users.DeleteAsync(u);
            if (!res.Succeeded)
                return BadRequest(res.Errors);

            return NoContent();
        }


        // GET api/Admin/users/{id}/auctions
        [HttpGet("users/{id}/auctions")]
        public async Task<IActionResult> GetUserAuctions(string id)
        {
            // reuse your auction service
            var list = await _auctionsSvc.GetAuctionsByUserAsync(id);
            return Ok(list);
        }

        // GET api/Admin/auctions?search=&page=&pageSize=
        [HttpGet("auctions")]
        public async Task<IActionResult> SearchAuctions(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var q = _db.Auctions.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                // simple title/description search; you can join AspNetUsers similarly
                q = q.Where(a =>
                    a.Title.ToLower().Contains(search)
                    || a.Description.ToLower().Contains(search));
            }

            var total = await q.CountAsync();
            var slice = await q
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => a.AuctionId)
                .ToListAsync();

            // map each id via your service (to get DTO)
            var items = new List<AuctionResponseDto>();
            foreach (var id in slice)
            {
                var dto = await _auctionsSvc.GetAuctionAsync(id);
                if (dto != null) items.Add(dto);
            }

            return Ok(new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                Items = items
            });
        }

        // GET api/Admin/auctions/{id}
        [HttpGet("auctions/{id:int}")]
        public async Task<IActionResult> GetAuction(int id)
        {
            var dto = await _auctionsSvc.GetAuctionDetailAsync(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        // PUT api/Admin/auctions/{id}
        [HttpPut("auctions/{id:int}")]
        public async Task<IActionResult> UpdateAuction(
            int id,
            [FromBody] AdminAuctionUpdateDto dto)
        {
            var a = await _db.Auctions.FindAsync(id);
            if (a == null) return NotFound();

            a.Title = dto.Title;
            a.Description = dto.Description;
            a.StartingPrice = dto.StartingPrice;
            a.StartDateTime = dto.StartDateTime;
            a.EndDateTime = dto.EndDateTime;
            a.MainImageUrl = dto.MainImageUrl ?? a.MainImageUrl;
            a.ThumbnailUrl = dto.ThumbnailUrl ?? a.ThumbnailUrl;
            a.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // return the full DTO so front-end can re-render
            var updated = await _auctionsSvc.GetAuctionAsync(id);
            return Ok(updated);
        }

        // DELETE api/Admin/auctions/{id}
        [HttpDelete("auctions/{id:int}")]
        public async Task<IActionResult> DeleteAuction(int id)
        {
            var a = await _db.Auctions.FindAsync(id);
            if (a == null) return NotFound();

            _db.Auctions.Remove(a);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
