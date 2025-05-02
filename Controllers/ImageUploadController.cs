using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ImageUploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    private bool IsImageByMagicBytes(IFormFile file)
    {
        // Read the first 8 bytes (max needed for PNG header)
        Span<byte> header = stackalloc byte[8];
        using var stream = file.OpenReadStream();
        if (stream.Length < header.Length) return false;
        stream.Read(header);

        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E &&
            header[3] == 0x47 && header[4] == 0x0D && header[5] == 0x0A &&
            header[6] == 0x1A && header[7] == 0x0A)
            return true;

        // GIF87a or GIF89a
        if (header[0] == (byte)'G' && header[1] == (byte)'I' &&
            header[2] == (byte)'F' &&
            (header[3] == (byte)'8' && (header[4] == (byte)'7' || header[4] == (byte)'9') && header[5] == (byte)'a'))
            return true;

        return false;
    }



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
        if (!IsImageByMagicBytes(file))
        return BadRequest(new { error = "File contents do not match a valid image." });



        var images = Path.Combine(_env.WebRootPath, "images");
        Directory.CreateDirectory(images);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(images, fileName);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        return Ok(new { url = $"/images/{fileName}" });
    }
}
