using System.Net;
namespace FileUpload.Controllers
{
    public class S3Response
    {
        public HttpStatusCode Status { get; set; }
        public string Message { get; set; }
    }
}