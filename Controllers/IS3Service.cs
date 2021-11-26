using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FileUpload.Controllers
{
    public interface IS3Service
    {
        Task<S3Response> CreateBucketAsync(string bucketName);
        Task<S3Response> AddFileAsync(string bucketName, IFormFile file);
    }
}