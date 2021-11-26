using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileUpload.Controllers
{
    [ApiController]
    [Route("s3")]
    public class UploadFileController : ControllerBase
    {
        private readonly IS3Service _service;
        public UploadFileController(IS3Service service)
        {
            _service = service;
        }

        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> CreateBucket(string bucketName)
        {
            var response = await _service.CreateBucketAsync(bucketName);
            return Ok(response);
        }

        [HttpPost]
        [RequestSizeLimit(10000000000)]
        [Route("AddFile")]
        public async Task<IActionResult> AddFile(string bucketName, IFormFile file)
        {
            var response = await _service.AddFileAsync(bucketName, file);
            return Ok(response);
        }
    }
}