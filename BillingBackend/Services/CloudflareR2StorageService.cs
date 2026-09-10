using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public class CloudflareR2StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _publicUrl;
        private readonly ILogger<CloudflareR2StorageService> _logger;

        public CloudflareR2StorageService(IConfiguration configuration, ILogger<CloudflareR2StorageService> logger)
        {
            _logger = logger;

            var accountId = configuration["CloudflareR2:AccountId"];
            var accessKey = configuration["CloudflareR2:AccessKey"];
            var secretKey = configuration["CloudflareR2:SecretKey"];
            _bucketName = configuration["CloudflareR2:BucketName"] ?? throw new ArgumentNullException(nameof(configuration), "CloudflareR2:BucketName is missing");
            _publicUrl = configuration["CloudflareR2:PublicUrl"] ?? throw new ArgumentNullException(nameof(configuration), "CloudflareR2:PublicUrl is missing");

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
                throw new InvalidOperationException("CloudflareR2 storage is not configured. Set CloudflareR2__AccountId/AccessKey/SecretKey via environment.");

            // Never log AccountId / keys / full URLs with secrets.
            _logger.LogInformation("Initializing CloudflareR2StorageService. BucketName={BucketName}", _bucketName);

            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true
            };

            var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
            _s3Client = new AmazonS3Client(credentials, config);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            // Strict filename allowlist: GUID.webp only (prevents traversal / content-type smuggling).
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                fileName.Contains('/') || fileName.Contains('\\') || !fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid file name.");

            _logger.LogInformation("Cloudflare R2 Upload Started: Bucket={Bucket}, Size={Size} bytes",
                _bucketName, fileStream.Length);

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileName,
                    InputStream = fileStream,
                    ContentType = contentType,
                    DisablePayloadSigning = true // Crucial for Cloudflare R2 S3-compatibility
                };

                var response = await _s3Client.PutObjectAsync(putRequest);

                _logger.LogInformation("Cloudflare R2 PutObjectAsync completed with status {StatusCode}", response.HttpStatusCode);

                var baseUrl = _publicUrl.TrimEnd('/');
                var fileUrl = $"{baseUrl}/{fileName}";

                _logger.LogInformation("Cloudflare R2 file upload completed.");
                return fileUrl;
            }
            catch (AmazonS3Exception s3Ex)
            {
                _logger.LogError(s3Ex, "AmazonS3Exception during R2 upload. ErrorCode={ErrorCode}, StatusCode={StatusCode}", 
                    s3Ex.ErrorCode, s3Ex.StatusCode);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during R2 upload: {Message}", ex.Message);
                throw;
            }
        }
    }
}
