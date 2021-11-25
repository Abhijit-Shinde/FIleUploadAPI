using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FileUpload.Controllers
{
    [ApiController]
    [Route("s3Bucket")]
    public class UploadFileController : ControllerBase
    {
        private readonly IS3Service _service;
        public UploadFileController(IS3Service service)
        {
            _service = service;
        }

        [HttpPost]
        [Route("Create/{bucketName}")]
        public async Task<IActionResult> CreateBucket([FromRoute] string bucketName)
        {
            var response = await _service.CreateBucketAsync(bucketName);
            return Ok(response);
        }

        [HttpPost]
        [Route("AddFile/{bucketName}")]
        public async Task<IActionResult> AddFile([FromRoute] string bucketName)
        {
            await _service.AddFileAsync(bucketName);
            return Ok();
        }
    }
}