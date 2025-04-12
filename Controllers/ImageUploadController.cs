// Controllers/ImageUploadController.cs
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace auctionbay_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageUploadController : ControllerBase
    {
        // POST: api/ImageUpload
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file was provided." });
            }

            try
            {
                //Ensure the wwwroot/images folder exists - pol bo to mak questionable 
                var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

                //generate a unique file name
                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(imagesFolder, fileName);

                //save the file locally
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                //construct the file's relative URL (assuming static files are served from wwwroot)
                var fileUrl = $"/images/{fileName}";

                return Ok(new { url = fileUrl, message = "Image uploaded successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
    }
}
