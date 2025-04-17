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

        var images = Path.Combine(_env.WebRootPath, "images");
        Directory.CreateDirectory(images);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(images, fileName);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        return Ok(new { url = $"/images/{fileName}" });
    }
}
