using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class LocalStorageService : IStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LocalStorageService> _logger;

        public LocalStorageService(IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor, ILogger<LocalStorageService> logger)
        {
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            // Strict filename allowlist (GUID.webp only) — prevents path traversal even if caller changes.
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                fileName.Contains('/') || fileName.Contains('\\') || !fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid file name.");

            _logger.LogInformation("LocalStorage Upload Started: Size={Size} bytes", fileStream.Length);

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, Path.GetFileName(fileName));
                // Ensure resolved path stays inside uploads folder (traversal guard).
                var fullUploads = Path.GetFullPath(uploadsFolder);
                var fullPath = Path.GetFullPath(filePath);
                if (!fullPath.StartsWith(fullUploads, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Invalid file path.");

                using (var destinationStream = new FileStream(fullPath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(destinationStream);
                }

                _logger.LogInformation("File saved to local filesystem successfully.");

                var request = _httpContextAccessor.HttpContext?.Request;
                if (request == null)
                {
                    return $"/uploads/{fileName}";
                }

                var fileUrl = $"{request.Scheme}://{request.Host}/uploads/{fileName}";
                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during LocalStorage upload: {Message}", ex.Message);
                throw;
            }
        }
    }
}
