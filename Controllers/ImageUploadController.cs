using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace auctionbay_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]                    
    public class ImageUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        public ImageUploadController(IWebHostEnvironment env) => _env = env;

        // POST api/ImageUpload
        // multipart/form‑data  { file: <blob> }
        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { error = "No file provided." });

            // 1. ensure /wwwroot/images exists
            var images = Path.Combine(_env.WebRootPath, "images");
            if (!Directory.Exists(images))
                Directory.CreateDirectory(images);

            // 2. save with a GUID name to avoid collisions
            var ext = Path.GetExtension(file.FileName);        // keeps .png / .jpg …
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(images, fileName);

            await using (var stream = System.IO.File.Create(path))
                await file.CopyToAsync(stream);

            // 3. return the public URL
            var url = $"/images/{fileName}";
            return Ok(new { url });
        }
    }
}
