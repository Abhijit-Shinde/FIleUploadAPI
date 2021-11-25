using System.Threading.Tasks;

namespace FileUpload.Controllers
{
    public interface IS3Service
    {
        Task<S3Response> CreateBucketAsync(string bucketName);
        Task AddFileAsync(string bucketName);
    }
}