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
            _logger.LogInformation("LocalStorage Upload Started: FileName={FileName}, ContentType={ContentType}, Size={Size} bytes", 
                fileName, contentType, fileStream.Length);

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
                _logger.LogInformation("Ensuring local directory exists: {Path}", uploadsFolder);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);
                _logger.LogInformation("Saving file to local path: {FilePath}", filePath);
                
                using (var destinationStream = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(destinationStream);
                }

                _logger.LogInformation("File saved to local filesystem successfully.");

                var request = _httpContextAccessor.HttpContext?.Request;
                if (request == null)
                {
                    var fallbackUrl = $"/uploads/{fileName}";
                    _logger.LogWarning("HttpContext request is null. Returning relative fallback URL: {Url}", fallbackUrl);
                    return fallbackUrl;
                }

                var fileUrl = $"{request.Scheme}://{request.Host}/uploads/{fileName}";
                _logger.LogInformation("Generated LocalStorage file URL: {Url}", fileUrl);
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
