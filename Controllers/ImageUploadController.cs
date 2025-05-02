using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImageUploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    public ImageUploadController(IWebHostEnvironment env) => _env = env;

    // ADD the Consumes attribute ⬇︎
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile file)          //  <- no [FromForm] fixed the issue
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });
        


        //size/type checks ---
        const long MAX_BYTES = 10 * 1024 * 1024; // 5 MB
        var allowed = new[] { "image/jpeg", "image/png", "image/gif" };
        if (file.Length > MAX_BYTES)
        return BadRequest(new { error = "File too large. Max 5 MB." });
        if (!allowed.Contains(file.ContentType))
        return BadRequest(new { error = "Invalid file type. Only JPEG, PNG, GIF allowed." });
        // --- end checks ---

                var images = Path.Combine(_env.WebRootPath, "images");
        Directory.CreateDirectory(images);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(images, fileName);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        return Ok(new { url = $"/images/{fileName}" });
    }
}
