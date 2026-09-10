using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using BillingBackend.Services;

namespace BillingBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public class UploadController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IStorageService storageService, ILogger<UploadController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [EnableRateLimiting("api")]
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024, ValueLengthLimit = 2 * 1024 * 1024)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { message = "File too large. Max 10 MB." });

            // Validate image extension (first gate; content is re-validated via decode + magic bytes)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                return BadRequest(new { message = "Invalid image format. Allowed: .jpg, .jpeg, .png, .gif, .webp" });

            // MIME allowlist (client-provided, so only a hint — real check is decode below)
            var allowedMime = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!string.IsNullOrWhiteSpace(file.ContentType) &&
                !allowedMime.Contains(file.ContentType.ToLowerInvariant()))
                return BadRequest(new { message = "Invalid content type." });

            try
            {
                // Generate unique filename (never trust client filename — prevents traversal)
                var uniqueFileName = $"{Guid.NewGuid():N}.webp";
                string fileUrl;

                using (var stream = file.OpenReadStream())
                {
                    // Magic-byte pre-check before ImageSharp decode (cheap reject of non-images)
                    if (!HasImageMagicBytes(stream))
                        return BadRequest(new { message = "File is not a valid image." });

                    using var image = await Image.LoadAsync(stream);
                    // Reject decompression bombs: cap decoded pixels (e.g. 25 MP).
                    if (image.Width <= 0 || image.Height <= 0 || (long)image.Width * image.Height > 25_000_000)
                        return BadRequest(new { message = "Image dimensions are not allowed." });

                    using var memoryStream = new MemoryStream();
                    await image.SaveAsWebpAsync(memoryStream);
                    memoryStream.Position = 0; // Reset stream position for upload
                    fileUrl = await _storageService.UploadFileAsync(memoryStream, uniqueFileName, "image/webp");
                }

                return Ok(new { url = fileUrl });
            }
            catch (UnknownImageFormatException)
            {
                return BadRequest(new { message = "File is not a valid image." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadImage failed.");
                return StatusCode(500, new { message = "Upload failed. Please try again." });
            }
        }

        private static bool HasImageMagicBytes(Stream stream)
        {
            try
            {
                Span<byte> header = stackalloc byte[12];
                stream.Position = 0;
                int read = stream.Read(header);
                stream.Position = 0;
                if (read < 4)
                    return false;
                // JPEG FF D8 FF
                if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                    return true;
                // PNG 89 50 4E 47
                if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                    return true;
                // GIF 47 49 46 38
                if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
                    return true;
                // WEBP RIFF....WEBP
                if (read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                    header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
                    return true;
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
