using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using NFD.Domain.Exceptions;
using NFD.Domain.Interfaces;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace NFD.Infrastructure
{
    public class AwsS3Service : IAwsS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private const string BUCKET_NAME = "crossover-s3";

        public AwsS3Service(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        public async Task<string> UploadFileAsync(Stream fileStream)
        {
            try
            {
                var fileTransferUtility = new TransferUtility(_s3Client);
                var key = Guid.NewGuid().ToString();

                var fileUploadRequest = new TransferUtilityUploadRequest() 
                {
                    CannedACL = S3CannedACL.PublicRead,
                    BucketName = BUCKET_NAME,
                    Key = key,
                    InputStream = fileStream
                };
                await fileTransferUtility.UploadAsync(fileUploadRequest);
                return key;
            }
            catch
            {
                throw new CustomException(HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again.");
            }
        }

        public async Task<bool> RemoveFileAsync(string key)
        {
            try
            {
                var fileTransferUtility = new TransferUtility(_s3Client);
                
                await fileTransferUtility.S3Client.DeleteObjectAsync(new DeleteObjectRequest()
                {
                    BucketName = BUCKET_NAME,
                    Key = key
                });
                
                return true;
            }
            catch
            {
                throw new CustomException(HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again.");
            }
        }
    }
}