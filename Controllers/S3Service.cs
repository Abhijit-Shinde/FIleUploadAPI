using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using Amazon.S3.Util;
using System.Collections.Generic;
using Amazon.Runtime;

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

            try{
                Console.WriteLine("Uploading....");

                var fileTransferUtility = new TransferUtility(_s3Client);

                var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                {
                    BucketName = bucketName,
                    FilePath = Path,
                    StorageClass = S3StorageClass.Standard,
                    PartSize = 20291456,
                    Key = keyName,
                    CannedACL = S3CannedACL.NoACL
                };

                fileTransferUtilityRequest.Metadata.Add("param1", "Value1");
                fileTransferUtilityRequest.Metadata.Add("param2", "Value2");

                await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);

                Console.WriteLine("Upload Completed");

            }catch (AmazonS3Exception e){
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e){
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
        }

        public static void UploadPartProgressEventCallback(object sender, StreamTransferProgressArgs e)
        {
            // Process event. 
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