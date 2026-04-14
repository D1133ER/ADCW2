using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WeblogApplication.Controllers.Api
{
    [ApiController]
    [Route("api/upload")]
    [Authorize]
    public class ApiUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public ApiUploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        // POST /api/upload/image
        [HttpPost("image")]
        public async Task<IActionResult> Image(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            // Validate content type
            var allowed = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowed.Contains(file.ContentType.ToLower()))
                return BadRequest(new { message = "Unsupported image type." });

            // Limit to 5 MB
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "File too large (max 5 MB)." });

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            var url = $"/uploads/{fileName}";
            return Ok(new { url });
        }
    }
}
