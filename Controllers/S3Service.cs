using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using Amazon.S3.Util;
using System.Collections.Generic;
using Amazon.Runtime;
using System.IO;

namespace FileUpload.Controllers
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private const string keyName = "MultipartUpload";
        private const string Path = "D:\\FileUpload\\Sample.csv";

        public S3Service(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        public async Task AddFileAsync(string bucketName)
        {

            List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();

            InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = keyName
            };

            InitiateMultipartUploadResponse initResponse =
                await _s3Client.InitiateMultipartUploadAsync(initiateRequest);

            long contentLength = new FileInfo(Path).Length;
            long partSize = 30 * (long)Math.Pow(2, 20);

            try
            {
                Console.WriteLine("Uploading parts....");
        
                long filePosition = 0;
                for (int i = 1; filePosition < contentLength; i++)
                {
                    UploadPartRequest uploadRequest = new UploadPartRequest
                        {
                            BucketName = bucketName,
                            Key = keyName,
                            UploadId = initResponse.UploadId,
                            PartNumber = i,
                            PartSize = partSize,
                            FilePosition = filePosition,
                            FilePath = Path
                        };

                    uploadRequest.StreamTransferProgress +=
                        new EventHandler<StreamTransferProgressArgs>(UploadPartProgress);

                    uploadResponses.Add(await _s3Client.UploadPartAsync(uploadRequest));

                    filePosition += partSize;
                }

                CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest
                    {
                        BucketName = bucketName,
                        Key = keyName,
                        UploadId = initResponse.UploadId
                     };

                completeRequest.AddPartETags(uploadResponses);

                CompleteMultipartUploadResponse completeUploadResponse =
                    await _s3Client.CompleteMultipartUploadAsync(completeRequest);

                Console.WriteLine("Successfully Uploaded");

            }catch (AmazonS3Exception e){
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e){
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
        }

        public static void UploadPartProgress(object sender, StreamTransferProgressArgs e)
        {
            Console.WriteLine("{0}/{1}", e.TransferredBytes, e.TotalBytes);
        }
        public async Task<S3Response> CreateBucketAsync(string bucketName)
        {
            try{
                if(await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client,bucketName) == false)
                {
                    var putBucketRequest = new PutBucketRequest
                    {
                        BucketName = bucketName,
                        UseClientRegion = true
                    };
                    var response = await _s3Client.PutBucketAsync(putBucketRequest);
                    return new S3Response
                    {
                        Status = response.HttpStatusCode,
                        Message = "Bucket Created"
                    };
                }
            }
            catch(AmazonS3Exception e)
            {
                return new S3Response
                {
                    Status = e.StatusCode,
                    Message = e.Message
                };
            }

            return new S3Response
            {
                Status = HttpStatusCode.InternalServerError,
                Message = "Bucket Already Exists"
            };
        }
    }
}