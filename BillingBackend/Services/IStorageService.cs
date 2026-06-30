using System.IO;
using System.Threading.Tasks;

namespace BillingBackend.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    }
}
