using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using BillingBackend.Services;

namespace BillingBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly IStorageService _storageService;
        private readonly ILogger<UploadController> _logger;

        public UploadController(IStorageService storageService, ILogger<UploadController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            _logger.LogInformation("UploadImage endpoint triggered.");

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("UploadImage failed: File is null or empty.");
                return BadRequest("No file uploaded.");
            }

            _logger.LogInformation("Received file: Name={FileName}, Length={Length} bytes, ContentType={ContentType}", 
                file.FileName, file.Length, file.ContentType);

            // Validate image extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogWarning("UploadImage failed: Invalid file extension '{Extension}'. Allowed extensions: {Allowed}", 
                    extension, string.Join(", ", allowedExtensions));
                return BadRequest("Invalid image format. Allowed: .jpg, .jpeg, .png, .gif, .webp");
            }

            try
            {
                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}.webp";
                string fileUrl;

                _logger.LogInformation("Starting image transcoding to WebP format in memory...");
                
                using (var stream = file.OpenReadStream())
                using (var image = await Image.LoadAsync(stream))
                using (var memoryStream = new MemoryStream())
                {
                    await image.SaveAsWebpAsync(memoryStream);
                    _logger.LogInformation("Transcoding completed. Original size: {OriginalSize} bytes -> WebP size: {WebpSize} bytes", 
                        file.Length, memoryStream.Length);

                    memoryStream.Position = 0; // Reset stream position for upload
                    
                    _logger.LogInformation("Calling StorageService to upload '{FileName}'...", uniqueFileName);
                    fileUrl = await _storageService.UploadFileAsync(memoryStream, uniqueFileName, "image/webp");
                }

                _logger.LogInformation("Upload successful. Image URL: {Url}", fileUrl);
                return Ok(new { url = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadImage failed with exception. Message: {Message}", ex.Message);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
