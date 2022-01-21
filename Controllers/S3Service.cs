using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using System.Collections.Generic;
using Amazon.Runtime;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace FileUpload.Controllers
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        public S3Service(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        public async Task<S3Response> AddFileAsync(string bucketName, IFormFile file)
        {
            List<UploadPartResponse> uploadResponses = new List<UploadPartResponse>();

            if(bucketName == null || bucketName.Length == 0)
            {
                return new S3Response
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = "Bucket name is required"
                };
            }

            InitiateMultipartUploadRequest initiateRequest = new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = file.FileName
            };

            InitiateMultipartUploadResponse initResponse =
                await _s3Client.InitiateMultipartUploadAsync(initiateRequest);

            if(file == null || file.Length == 0)
            {
                return new S3Response
                {
                    Status = HttpStatusCode.BadRequest,
                    Message = "File is null or empty"
                };
            }

            var path = Path.Combine(  
                     Directory.GetCurrentDirectory(),file.FileName);

            //var memory = new MemoryStream();  
            using (var stream = new FileStream(path, FileMode.OpenOrCreate))  
            {  
                await file.CopyToAsync(stream);  
            }

            long contentLength = new FileInfo(file.FileName).Length;
            long partSize = 30 * (long)Math.Pow(2, 20);

            try
            {
                long filePosition = 0;
                for (int i = 1; filePosition < contentLength; i++)
                {
                    UploadPartRequest uploadRequest = new UploadPartRequest
                        {
                            BucketName = bucketName,
                            Key = file.FileName,
                            UploadId = initResponse.UploadId,
                            PartNumber = i,
                            PartSize = partSize,
                            FilePosition = filePosition,
                            FilePath = file.FileName
                        };

                    uploadRequest.StreamTransferProgress +=
                        new EventHandler<StreamTransferProgressArgs>(UploadPartProgress);

                    uploadResponses.Add(await _s3Client.UploadPartAsync(uploadRequest));

                    filePosition += partSize;
                }

                CompleteMultipartUploadRequest completeRequest = new CompleteMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = file.FileName,
                    UploadId = initResponse.UploadId
                };

                completeRequest.AddPartETags(uploadResponses);

                CompleteMultipartUploadResponse completeUploadResponse =
                    await _s3Client.CompleteMultipartUploadAsync(completeRequest);

                return new S3Response {
                    Status = HttpStatusCode.OK,
                    Message = "File uploaded to S3",
                    Location = completeUploadResponse.Location,
                    ETag = completeUploadResponse.ETag,
                };    

            }catch (AmazonS3Exception e){
                return new S3Response
                {
                    Status = e.StatusCode,
                    Message = e.Message
                };
            }
            catch (Exception e){
                Console.WriteLine("An AmazonS3Exception was thrown: {0}", e.Message);

                AbortMultipartUploadRequest abortMPURequest = new AbortMultipartUploadRequest
                {
                    BucketName = bucketName,
                    Key = file.FileName,
                    UploadId = initResponse.UploadId
                };
               await _s3Client.AbortMultipartUploadAsync(abortMPURequest);
            }

            return new S3Response {
                Status = HttpStatusCode.InternalServerError,
                Message = "Error. Failed to upload File"
            };

        }

        public static void UploadPartProgress(object sender, StreamTransferProgressArgs e)
        {
            Console.WriteLine("{0}/{1}, {2}% done", e.TransferredBytes, e.TotalBytes, e.PercentDone);
        }

        public async Task<S3Response> CreateBucketAsync(string bucketName)
        {
            try
            {
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
            catch (AmazonS3Exception e)
            {
                return new S3Response
                {
                    Status = e.StatusCode,
                    Message = "Bucket Name with Hyphen, UnderScore or UpperCase letters is not allowed"
                };
            }
            catch(Exception)
            {
                return new S3Response
                {
                    Status = HttpStatusCode.InternalServerError,
                    Message = "Bucket Name is Required"
                };
            }

            return new S3Response
            {
                Status = HttpStatusCode.Conflict,
                Message = "Bucket Already Exists in region"
            };
        }
    }
}
